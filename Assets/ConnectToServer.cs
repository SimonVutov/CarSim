using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class ConnectToServer : MonoBehaviourPunCallbacks
{
    bool canJoin;
    
    public void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        canJoin = true;
    }

    public void WantToJoinMultiplayer()
    {
        if (canJoin) {
            SceneManager.LoadScene("Lobby");
        }
    }
}
