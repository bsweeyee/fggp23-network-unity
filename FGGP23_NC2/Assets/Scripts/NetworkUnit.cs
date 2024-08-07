using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(NetworkTransform))]
public class NetworkUnit : NetworkBehaviour
{
    NetworkVariable<Vector3> moveInput = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Server);    

    MeshRenderer renderer;        

    private void OnEnable()
    {
        renderer = GetComponentInChildren<MeshRenderer>();
        if (OwnerClientId == NetworkManager.Singleton.LocalClientId) // TODO: check which client this network unit belongs to
        {
            renderer.SetMaterials(new List<Material>{ FGNetworkProgramming.LocalGame.Instance.GameData.GameMaterials[0] } );
        }
        else
        {
            renderer.SetMaterials(new List<Material>{ FGNetworkProgramming.LocalGame.Instance.GameData.GameMaterials[1] } );
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
