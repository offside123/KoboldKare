using System.Collections;
using System.Collections.Generic;
using System.IO;
using KoboldKare;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

public class GenericPurchasable : GenericUsable {

    [SerializeField]
    private ScriptableFloat money;
    [SerializeField]
    private Sprite displaySprite;

    [SerializeField]
    private ScriptablePurchasable purchasable;
    [SerializeField]
    private AudioPack purchaseSoundPack;
    private bool inStock {
        get {
            return display.activeInHierarchy;
        }
    }
    [SerializeField]
    private GameEventGeneric restockEvent;
    private GameObject display;
    private AudioSource source;
    [SerializeField]
    private UnityEvent purchased;
    [SerializeField]
    private MoneyFloater floater;

    public ScriptablePurchasable GetPurchasable() => purchasable;
    public delegate void PurchasableChangedAction(ScriptablePurchasable newPurchasable);
    public PurchasableChangedAction purchasableChanged;
    public virtual void Start() {
        source = new AudioSource();
        source = gameObject.AddComponent<AudioSource>();
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Custom;
        source.minDistance = 0f;
        source.maxDistance = 25f;
        source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, GameManager.instance.volumeCurve);
        source.outputAudioMixerGroup = GameManager.instance.soundEffectGroup;
        if (restockEvent != null) {
            restockEvent.AddListener(OnRestock);
        }
        SwapTo(purchasable, true);
    }
    public override Sprite GetSprite(Kobold k) {
        return displaySprite;
    }
    protected void SwapTo(ScriptablePurchasable newPurchasable, bool forceRefresh = false) {
        if (purchasable == newPurchasable && !forceRefresh) {
            return;
        }
        if (display != null) {
            Destroy(display);
        }
        purchasable = newPurchasable;
        display = GameObject.Instantiate(purchasable.display, transform);
        Bounds centerBounds = ScriptablePurchasable.DisableAllButGraphics(display);
        floater.SetBounds(centerBounds);
        display.SetActive(inStock);
        floater.SetText(purchasable.cost.ToString());
        purchasableChanged?.Invoke(purchasable);
    }
    public virtual void OnDestroy() {
        if (restockEvent != null) {
            restockEvent.RemoveListener(OnRestock);
        }
    }
    public virtual void OnRestock(object nothing) {
        if (!display.activeInHierarchy) {
            display.SetActive(true);
            floater.gameObject.SetActive(true);
        }
    }
    public override bool CanUse(Kobold k) {
        return display.activeInHierarchy && money.has(purchasable.cost);
    }
    public override void Use(Kobold k) {
        base.Use(k);
        source.PlayOneShot(purchaseSoundPack.GetRandomClip(), purchaseSoundPack.volume);
        if (MoneySyncHack.view.IsMine && CanUse(k)) {
            money.charge(purchasable.cost);
            PhotonNetwork.Instantiate(purchasable.spawnPrefab.photonName, transform.position, Quaternion.identity);
        }
        floater.gameObject.SetActive(false);
        purchased.Invoke();
        display.SetActive(false);
    }
    public override void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        base.OnPhotonSerializeView(stream, info);
        if (stream.IsWriting) {
            stream.SendNext(inStock);
            stream.SendNext(PurchasableDatabase.GetID(purchasable));
        } else {
            display.SetActive((bool)stream.ReceiveNext());
            short currentPurchasable = (short)stream.ReceiveNext();
            SwapTo(PurchasableDatabase.GetPurchasable(currentPurchasable));
        }
    }
    public override void Save(BinaryWriter writer, string version) {
        base.Save(writer, version);
        writer.Write(inStock);
        writer.Write(PurchasableDatabase.GetID(purchasable));
    }

    public override void Load(BinaryReader reader, string version) {
        base.Load(reader, version);
        display.SetActive(reader.ReadBoolean());
        short currentPurchasable = (short)reader.ReadInt16();
        SwapTo(PurchasableDatabase.GetPurchasable(currentPurchasable));
    }
}
