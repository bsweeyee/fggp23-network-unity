using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using FGNetworkProgramming;
using System.Linq;

public interface INetworkInitialize
{
    void OnNetworkInitialize(NetworkGame game);
}

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
                        }
                    }
                    break;
                }
            });
        }        
    }        
    
    public override void OnNetworkSpawn()
    {
        if (IsLocalPlayer)
        {            
            // we call all interfaces that should only run when network is spawned
            var networkInitializers = FindObjectsOfType<MonoBehaviour>(true).OfType<INetworkInitialize>();
            foreach(var ni in networkInitializers)
            {
                ni.OnNetworkInitialize(this);
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void SpawnUnitRPC(ulong id)
    {        
        Vector3 position = Vector3.zero;        
        if (NetworkManager.Singleton.LocalClientId == id)
        {
            position = LocalGame.Instance.GameData.UnitSpawnPosition[0];
        }
        else
        {
            position = LocalGame.Instance.GameData.UnitSpawnPosition[1];
        }

        NetworkUnit ob = Instantiate(LocalGame.Instance.GameData.NetworkUnit, position, Quaternion.identity);
        ob.GetComponent<NetworkObject>().Spawn();
        networkUnitInstances.Add(ob); // TODO: remember to remove this from list when a networkUnit is destroyed                 
    }    
}
