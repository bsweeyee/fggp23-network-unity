using System.Collections;
using System.Collections.Generic;
using FGNetworkProgramming;
using UnityEngine;
using UnityEngine.Rendering;

public class LocalGameCamera : MonoBehaviour, IOnGameStatePlay, IOnGameStateStart, IOnGameStateWaiting
{
    private Camera gameCamera;

    public Camera GameCamera
    {
        get { return gameCamera; }
    }

    static Material lineMaterial;

    public void Initialize(LocalGame lg)
    {
        gameCamera = GetComponent<Camera>();
    }

    public void OnGameStatePlay(NetworkGame game, int connectionIndex)
    {
        transform.position = LocalGame.Instance.GameData.CameraSpawnPosition[connectionIndex];
        transform.rotation = LocalGame.Instance.GameData.CameraRotation[connectionIndex];
    }

    public void OnGameStateStart(LocalGame game)
    {
        transform.position = LocalGame.Instance.GameData.CameraNonNetworkSpawnPosition;
        transform.rotation = LocalGame.Instance.GameData.CameraNonNetworkRotation;
    }

    public void OnGameStateWaiting(NetworkGame myNetworkGame, LocalGame game)
    {
        transform.position = LocalGame.Instance.GameData.CameraNonNetworkSpawnPosition;
        transform.rotation = LocalGame.Instance.GameData.CameraNonNetworkRotation;
    }            
}
