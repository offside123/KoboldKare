using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;
using Photon;
using Photon.Pun;
using Photon.Realtime;
using System.IO;

[RequireComponent(typeof(AudioSource))]
public class Pachinko : GenericUsable {
#region Definitions
    [System.Serializable]
    public class Prize {
        public Transform location;
        public UnityEvent prizeGet;
        public ScriptablePurchasable prizeSpawn;
        public VisualEffect spawnVFX;
        public void Spawn() {
            GameObject gobj = GameObject.Instantiate(prizeSpawn.display, location);
            ScriptablePurchasable.DisableAllButGraphics(gobj);
            spawnVFX = Instantiate(spawnVFX, location);
        }
        public void Claim() {
            if ( prizeSpawn.spawnPrefab != null) {
                GameObject award = PhotonNetwork.Instantiate(prizeSpawn.spawnPrefab.photonName, location.position, location.rotation);
                //TODO: Play particle system to mask/explain instantaneous spawning
                if(award.GetComponent<Rigidbody>() != null) {
                    award.GetComponent<Rigidbody>().AddRelativeForce(Vector3.up * 10f, ForceMode.VelocityChange); //Shoot the prize out from its spawn spot
                }
            }
            prizeGet.Invoke();
            spawnVFX.Play();
        }
    }
    [Header("Pachinko!")]
    [SerializeField]
    private MoneyFloater floater;
    [SerializeField]
    private Sprite displaySprite;
    public ScriptableFloat money;
    public float playCost = 50f;
    public PhotonGameObjectReference pachinkoBallPrefab;
    public Transform ballSpawnPoint;
    GameObject activeBall;
    
    AudioSource audioSrc;
    [Header("Audio Setup"),Space(10)]
    public AudioClip wonPrize;
    public AudioClip ballReset, hitPin, gameStart;
    [Space(10)]
    [Header("Prize Setup"),Space(10)]
    public List<Prize> prizes = new List<Prize>();
#endregion

#region Top Level Code
    public override Sprite GetSprite(Kobold k) {
        return displaySprite;
    }
    void Start(){
        floater.SetBounds(GetComponent<Renderer>().bounds);
        floater.SetText(playCost.ToString());
        audioSrc = GetComponent<AudioSource>();
        foreach(var prize in prizes) {
            prize.Spawn();
        }
    }

    public override bool CanUse(Kobold k) {
        return money.has(playCost) && activeBall == null;
    }

    public override void Use(Kobold k) {
        if (MoneySyncHack.view.IsMine && CanUse(k)) {
            money.charge(playCost);
            StartGame();
        }
    }

    public void StartGame(){
        if (!photonView.IsMine) {
            return;
        }
        SpawnBall();
        audioSrc.clip = gameStart;
        audioSrc.Play();
    }

    public void ResetGame(){
        if(photonView.IsMine && activeBall != null){
            PhotonNetwork.Destroy(activeBall); //One ball per customer!
        }
    }
    #endregion

#region Worker Methods
    void DestroyBall(){
        if (!photonView.IsMine) {
            return;
        }
        PhotonNetwork.Destroy(activeBall);
    }

    void SpawnBall(){
        if (!photonView.IsMine) {
            return;
        }
        //Debug.Log("Ball spawned");
        activeBall = PhotonNetwork.Instantiate(pachinkoBallPrefab.photonName,ballSpawnPoint.position,Quaternion.identity, 0, new object[]{photonView.ViewID});
        // We set the machine via instantiation data, since other clients would just have the ball "appear".
        //activeBall.GetComponent<BallCheat>().SetMachine(this);
        activeBall.GetComponent<Rigidbody>().velocity = ballSpawnPoint.transform.parent.GetComponent<Rigidbody>().velocity;
    }

    void DistributePrize(int listIdx){
        if (!photonView.IsMine) {
            return;
        }
        audioSrc.clip = wonPrize;
        audioSrc.Play();
        prizes[listIdx].Claim();
    }

    public void ReachedBottom(PachinkoBallZone src){
        //Debug.Log("Ball reached bottom!");
        DestroyBall();
        DistributePrize(src.zoneID);
    }
#endregion

#region Utilities
    public void BallStuck(){
        audioSrc.PlayOneShot(ballReset);
        ResetGame();
    }

    public void HitPin(){
        audioSrc.PlayOneShot(hitPin);
    }
    #endregion
    void OnValidate() {
        pachinkoBallPrefab.OnValidate();
    }
    public override void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) { }
    public override void Save(BinaryWriter writer, string version) { }
    public override void Load(BinaryReader reader, string version) { }
}