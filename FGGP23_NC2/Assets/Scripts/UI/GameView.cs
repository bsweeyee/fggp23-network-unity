using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FGNetworkProgramming;
using Unity.Netcode;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class GameView : MonoBehaviour, INetworkGameSpawned
{       
    [SerializeField] private Button spawnUnit;

    private Canvas canvas;
    private EventSystem eventSystem;
    public void Initialize(LocalGame localGame)
    {
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = localGame.MainCamera.GameCamera;

        var es = new GameObject("EventSystem");
        
        eventSystem = es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        gameObject.SetActive(false);        
    }

    public void OnNetworkGameSpawned(NetworkGame networkGame, ulong clientID)
    {
        gameObject.SetActive(true);
        spawnUnit.onClick.AddListener(() => {
            // TODO: Figure out a way to abstract the need to reference networkGame from View
            networkGame.SpawnUnitRpc(NetworkManager.Singleton.LocalClientId);
        });
    }    
}
