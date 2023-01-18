using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    //Objects & Components:
    private GameObject networkPlayerInstance; //Instance of this player in the photon network

    //Settings:
    [Header("General Settings:")]
    [SerializeField, Min(0.5f), Tooltip("Time to wait on connection failure to retry")]   private float joinRetryDelay;

    //Runtime Variables:
    private bool connectedToServer = false; //Whether or not this player is currently connected to the server
    private bool connectedToRoom = false;   //Whether or not this player is currently in the demo room

    //EVENTS & COROUTINES:
    IEnumerator RetryServerConnect()
    {
        yield return new WaitForSeconds(joinRetryDelay); //Wait for designated number of seconds
        ConnectToServer();                               //Re-attempt server connection
    }
    IEnumerator RetryRoomConnect()
    {
        yield return new WaitForSeconds(joinRetryDelay); //Wait for designated number of seconds
        ConnectToRoom();                                 //Re-attempt room connection
    }

    //REALTIME METHODS:
    private void Start()
    {
        ConnectToServer(); //DEMO: Connect to server immediately when game starts
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Attempt to connect instance to Photon server.
    /// </summary>
    private void ConnectToServer()
    {
        if (PhotonNetwork.ConnectUsingSettings()) //Attempt server connection
        {
            connectedToServer = true; //Indicate that player is now connected to server
        }
        else //Server connection failed for some reason
        {
            Debug.LogWarning("Failed to connect to server, retrying in " + joinRetryDelay + " seconds."); //Indicate failure to connect
            StartCoroutine(RetryServerConnect());                                                         //Wait then retry connection
        }
    }
    /// <summary>
    /// Attempt to connect player to default demo room.
    /// </summary>
    private void ConnectToRoom()
    {
        RoomOptions roomOptions = new RoomOptions(); //Instantiate new room settings object
        roomOptions.MaxPlayers = 4;                  //Set max player count (arbitrarily)
        roomOptions.IsVisible = true;                //Make sure room is listed in lobby
        roomOptions.IsOpen = true;                   //Make sure room is joinable
        if (PhotonNetwork.JoinOrCreateRoom("DemoRoom", roomOptions, TypedLobby.Default)) //Join or create room
        {
            connectedToRoom = true; //Indicate that player is now connected to room
        }
        else //Room connection failed for some reason
        {
            Debug.LogWarning("Failed to join/create room, retrying in " + joinRetryDelay + " seconds."); //Indicate failure to connect
            StartCoroutine(RetryRoomConnect());                                                          //Wait then retry connection
        }
    }

    //CALLBACK METHODS:
    public override void OnConnectedToMaster()
    {
        //Initialization:
        base.OnConnectedToMaster();                     //Call base method
        Debug.Log("Successfully connected to server."); //Indicate successful connection
        ConnectToRoom();                                //Attempt to join demo room immediately
    }
    public override void OnJoinedRoom()
    {
        //Initialization:
        base.OnJoinedRoom();       //Call base method
        Debug.Log("Joined room."); //Indicate that room has been successfully joined

        //Instantiate network player:
        Transform playerTransform = PlayerController.main.XROrigin.transform;                                                   //Get transform for player body
        networkPlayerInstance = PhotonNetwork.Instantiate("NetworkPlayer", playerTransform.position, playerTransform.rotation); //Instantiate this player in the Photon network at actual position of player (and store a reference to spawned object)
    }
    public override void OnLeftRoom()
    {
        //Initialization:
        base.OnLeftRoom();       //Call base method
        Debug.Log("Left room."); //Indicate that player has left room

        //Cleanup:
        PhotonNetwork.Destroy(networkPlayerInstance); //Destroy instance of player in network
    }
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        //Initialization:
        base.OnPlayerEnteredRoom(newPlayer);            //Call base method
        Debug.Log("A new player has joined the room."); //Indicate that a new player has joined
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        //Initialization:
        base.OnPlayerLeftRoom(otherPlayer);      //Call base method
        Debug.Log("A player has left the room"); //Indicate that a player has left the room
    }
}
