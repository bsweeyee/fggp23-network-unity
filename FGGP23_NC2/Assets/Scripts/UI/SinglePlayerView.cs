using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using FGNetworkProgramming;

public class SinglePlayerView : MonoBehaviour, IOnGameStatePlay, IOnGameStateStart, IOnGameStateWaiting, IOnGameStateLose, IOnGameStateWin
{
    [SerializeField] private Button hostButton;        
    [SerializeField] private Button joinButton;        
    [SerializeField] private Button quitButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button leaveButton;    
    [SerializeField] private Button waitingLeaveButton;    

    [SerializeField] private GameObject singlePlayerButtons;
    [SerializeField] private GameObject resetGameButtons;
    [SerializeField] private GameObject multiplayerWait;
    [SerializeField] private GameObject gameWin;
    [SerializeField] private GameObject gameLose;

    public void Initialize()
    {
        hostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
        });
        joinButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
        });
        quitButton.onClick.AddListener(() => {
            Application.Quit();
        });
        restartButton.onClick.AddListener(() => {
            LocalGame.Instance.MyNetworkGameInstance.RestartRpc();
        });
        leaveButton.onClick.AddListener(() => {
            NetworkManager.Singleton.Shutdown();
        });
        waitingLeaveButton.onClick.AddListener(() => {
            NetworkManager.Singleton.Shutdown();
        });        
    }

    public void OnGameStateLose(NetworkGame myNetworkGame, LocalGame myLocalGame)
    {
        gameObject.SetActive(true);
        multiplayerWait.SetActive(false);
        singlePlayerButtons.SetActive(false);
        gameWin.SetActive(false);
        gameLose.SetActive(true);
        resetGameButtons.SetActive(true);
    }

    public void OnGameStatePlay(NetworkGame myNetworkGame, int localConnectionIndex)
    {
        gameObject.SetActive(false);
    }

    public void OnGameStateStart(LocalGame myLocalGame)
    {
        gameObject.SetActive(true);
        singlePlayerButtons.SetActive(true);
        multiplayerWait.SetActive(false);        
        gameWin.SetActive(false);        
        gameLose.SetActive(false);
        resetGameButtons.SetActive(false);        
    }

    public void OnGameStateWaiting(NetworkGame myNetworkGame, LocalGame myLocalGame)
    {
        gameObject.SetActive(true);
        multiplayerWait.SetActive(true);
        singlePlayerButtons.SetActive(false);
        gameWin.SetActive(false);
        gameLose.SetActive(false);
        resetGameButtons.SetActive(false);
    }

    public void OnGameStateWin(NetworkGame myNetworkGame, LocalGame myLocalGame)
    {
        gameObject.SetActive(true);
        multiplayerWait.SetActive(false);
        singlePlayerButtons.SetActive(false);
        gameWin.SetActive(true);
        gameLose.SetActive(false);
        resetGameButtons.SetActive(true);
    }
}
