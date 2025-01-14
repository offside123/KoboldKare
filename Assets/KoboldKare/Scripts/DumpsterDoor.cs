using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

[RequireComponent(typeof(Animator)), RequireComponent(typeof(AudioSource)), RequireComponent(typeof(Photon.Pun.PhotonView))]
public class DumpsterDoor : GenericDoor {
    public ScriptableFloat money;
    public GameObject dumpsterTrigger;
    protected override void Open(){
        base.Open();
        dumpsterTrigger.SetActive(true);
    }
    protected override void Close(){
        base.Close();
        dumpsterTrigger.SetActive(false);
    }
}
