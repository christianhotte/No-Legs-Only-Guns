using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Photon.Pun;

public class NetworkPlayer : MonoBehaviour
{
    //Objects & Components:
    private PhotonView photonView; //PhotonView component on this object

    //Settings:
    [Header("Components:")]
    [SerializeField, Tooltip("Object following player's head")]       private Transform head;
    [SerializeField, Tooltip("Object following player's left hand")]  private Transform leftHand;
    [SerializeField, Tooltip("Object following player's right hand")] private Transform rightHand;

    //Runtime Variables:

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        photonView = GetComponent<PhotonView>(); //Get photon view component from this object

        //Hide self:
        if (photonView.IsMine) //This network player belongs to real player in this scene
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false; //Disable all renderers in own network player
        }
    }
    private void Update()
    {
        if (photonView.IsMine) //Only perform updates for self
        {
            //Map rig positions:
            MapPosition(PlayerController.main.head, head);           //Map head transform
            MapPosition(PlayerController.main.leftHand, leftHand);   //Map left hand transform
            MapPosition(PlayerController.main.rightHand, rightHand); //Map right hand transform
        }
    }

    //FUNCTIONALITY METHODS:
    private void MapPosition(Transform target, Transform rigTransform)
    {
        target.position = rigTransform.position; //Match target position to that of rig
        target.rotation = rigTransform.rotation; //Match target rotation to that of rig
    }
}
