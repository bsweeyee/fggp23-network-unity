using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class NetworkUnit : NetworkBehaviour
{
    NetworkVariable<Vector3> moveInput = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Server);    

    MeshRenderer renderer;        

    private void OnEnable()
    {
        renderer = GetComponentInChildren<MeshRenderer>();
        if (OwnerClientId == NetworkManager.Singleton.LocalClientId)
        {
            renderer.SetMaterials(new List<Material>{ FGNetworkProgramming.LocalGame.Instance.GameMaterials[0] } );
        }
        else
        {
            renderer.SetMaterials(new List<Material>{ FGNetworkProgramming.LocalGame.Instance.GameMaterials[1] } );
        }
    }

    private void Update()
    {
        // if (IsServer)
        // {            
        //     transform.position += moveInput.Value;
        // }
    }

    private void OnMove(Vector3 input)
    {
        MoveRPC(input);
    }

    [Rpc(SendTo.Server)]
    private void MoveRPC(Vector3 data)
    {
        moveInput.Value = data;
    }    
}
