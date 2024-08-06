using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class Player : NetworkBehaviour
{
    [SerializeField] private NetworkObject bullet;

    NetworkVariable<Vector3> moveInput = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Server);
    void Start()
    {
        // TODO: register input events
        if (IsLocalPlayer)
        {

        }        
    }

    private void Update()
    {
        if (IsServer)
        {            
            transform.position += moveInput.Value;
        }
    }
    
    private void OnMove(Vector3 input)
    {
        MoveRPC(input);
    }

    [Rpc(SendTo.Server)]
    private void SpawnRPC()
    {
        NetworkObject ob = Instantiate(bullet);
        ob.Spawn();

        // Alternative spawn method
        // NetworkManager.Instantiate(bullet);        
    }

    [Rpc(SendTo.Server)]
    private void MoveRPC(Vector3 data)
    {
        moveInput.Value = data;
    }

    // TODO: check on how to send strings through RPC
    [Rpc(SendTo.Server)]
    public void SubmitStringRPC(FixedString128Bytes s)
    {
        UpdateStringRpc(s);
    }

    [Rpc(SendTo.Everyone)]
    public void UpdateStringRpc(FixedString128Bytes s)
    {
        Debug.Log("[" + OwnerClientId + "] Client received: " + s);
    }
}
