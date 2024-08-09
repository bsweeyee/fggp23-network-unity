using System.Collections;
using System.Collections.Generic;
using FGNetworkProgramming;
using UnityEngine;

public class LocalGameCamera : MonoBehaviour, INetworkGameSpawned
{
    private Camera gameCamera;

    public Camera GameCamera
    {
        get { return gameCamera; }
    }

    public void Initialize(LocalGame lg)
    {
        gameCamera = GetComponent<Camera>();
    }

    public void OnNetworkGameSpawned(NetworkGame game, ulong clientID)
    {
        int idx = (int)clientID;
        transform.position = LocalGame.Instance.GameData.CameraSpawnPosition[idx];
        transform.rotation = LocalGame.Instance.GameData.CameraRotation[idx];
    }
}
