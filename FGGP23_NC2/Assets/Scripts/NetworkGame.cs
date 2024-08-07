using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using FGNetworkProgramming;
using Unity.VisualScripting;

/// <summary>
/// Network Game tracks GameStates and manages spawned network objects
/// </summary>
public class NetworkGame : NetworkBehaviour
{
    List<NetworkUnit> networkUnitInstances = new List<NetworkUnit>();    
    void Start()
    {
        // TODO: register input events
        if (IsLocalPlayer)
        {
            FGNetworkProgramming.Input.Instance.OnHandleMouseInput.AddListener((Vector2 mousePos, EGameInput input, EInputState state) => {
                switch(input)
                {
                    case EGameInput.LEFT_MOUSE_BUTTON:
                    if (state == EInputState.PRESSED)
                    {
                        Ray ray = FGNetworkProgramming.LocalGame.Instance.MainCamera.ScreenPointToRay(mousePos);                        
                        RaycastHit hit;                    
                        bool isHit = Physics.Raycast(ray, out hit);
                        if (isHit)
                        {
                            Debug.Log("hit point: " + hit.point);
                            SpawnUnitRPC(hit.point);
                        }
                    }
                    break;
                }
            });
        }        
    }        
    
    [Rpc(SendTo.Server)]
    public void SpawnUnitRPC(Vector3 position)
    {
        NetworkUnit ob = Instantiate(LocalGame.Instance.GameData.NetworkUnit, position, Quaternion.identity);
        ob.GetComponent<NetworkObject>().SpawnWithOwnership(NetworkManager.Singleton.LocalClientId);
        networkUnitInstances.Add(ob); // TODO: remember to remove this from list when a networkUnit is destroyed                 
    }    
}
