using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using FGNetworkProgramming;

public class NetworkGame : NetworkBehaviour
{
    public List<NetworkObject> networkObjects= new List<NetworkObject>();

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
                            SpawnRPC(0, hit.point);
                        }
                    }
                    break;
                }
            });
        }        
    }        

    [Rpc(SendTo.Server)]
    private void SpawnRPC(int idx, Vector3 position)
    {
        NetworkObject ob = Instantiate(networkObjects[idx], position, Quaternion.identity);
        ob.SpawnWithOwnership(NetworkManager.Singleton.LocalClientId);        

        // Alternative spawn method
        // NetworkManager.Instantiate(bullet);        
    }

    void OnGUI()
    {

    }    
}
