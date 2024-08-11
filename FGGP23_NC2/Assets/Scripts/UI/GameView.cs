using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FGNetworkProgramming;
using Unity.Netcode;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class GameView : MonoBehaviour, IOnGameStatePlay, IOnGameStateStart, IOnGameStateWaiting, IOnGameStateWin, IOnGameStateLose
{       
    [SerializeField] private Button spawnUnit;

    private Canvas canvas;
    private EventSystem eventSystem;
    public void Initialize(LocalGame localGame, Camera worldCamera)
    {
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = worldCamera;

        var es = new GameObject("EventSystem");
        
        eventSystem = es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();                
    }

    public void OnGameStatePlay(NetworkGame networkGame, int clientID)
    {
        gameObject.SetActive(true);
        spawnUnit.onClick.RemoveAllListeners();
        spawnUnit.onClick.AddListener(() => {
            // TODO: Figure out a way to abstract the need to reference networkGame from View
            networkGame.SpawnUnitRpc(LocalGame.Instance.LocalConnectionIndex);
        });
    }

    public void OnGameStateStart(LocalGame game)
    {
        gameObject.SetActive(false);
    }

    public void OnGameStateWaiting(NetworkGame myNetworkGame, LocalGame game)
    {
        gameObject.SetActive(false);
    }

    public void OnGameStateWin(NetworkGame myNetworkGame, LocalGame myLocalGame)
    {
        gameObject.SetActive(false);
    }

    public void OnGameStateLose(NetworkGame myNetworkGame, LocalGame myLocalGame)
    {
        gameObject.SetActive(false);
    }
}
