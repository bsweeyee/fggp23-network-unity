using System.Collections;
using System.Collections.Generic;
using FGNetworkProgramming;
using UnityEngine;

public class Unit : MonoBehaviour
{
    UnitData data;
    MeshRenderer renderer;

    public UnitData Data
    {
        get { return data;}
    }

    public void Initialize(UnitData ud, ulong clientID)
    {
        data = ud;
        renderer = GetComponentInChildren<MeshRenderer>();
        if (ud.NetworkOwnerID == clientID) // TODO: check which client this network unit belongs to
        {
            renderer.SetMaterials(new List<Material>{ FGNetworkProgramming.LocalGame.Instance.GameData.GameMaterials[0] } );
            transform.position = LocalGame.Instance.GameData.UnitSpawnPosition[0];
        }
        else
        {
            renderer.SetMaterials(new List<Material>{ FGNetworkProgramming.LocalGame.Instance.GameData.GameMaterials[1] } );
            transform.position = LocalGame.Instance.GameData.UnitSpawnPosition[1];
        }
    }
}
