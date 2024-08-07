using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FGNetworkProgramming;
using Unity.Netcode;

public class GameView : MonoBehaviour
{       
    private Canvas canvas;
    private Button spawnUnit;
    public void Initialize(LocalGame localGame)
    {
        canvas = GetComponent<Canvas>();
        canvas.worldCamera = localGame.MainCamera;        
    }

    public void NetworkInitialize(NetworkGame networkGame)
    {
        spawnUnit.onClick.AddListener(() => {
            // TODO: Figure out a way to abstract the need to reference networkGame from View
            int id = (int) NetworkManager.Singleton.LocalClientId; // TODO: Local Client Id will likely change when a player leaves and a new player joins. Check and change implementation if that's the case
            networkGame.SpawnUnitRPC(LocalGame.Instance.GameData.UnitSpawnPosition[id]);                        
        });
    }    
}
