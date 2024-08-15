using System.Collections;
using System.Collections.Generic;
using FGNetworkProgramming;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

public class Projectile : MonoBehaviour
{
    private float accumulatedTime;

    private float timeIntervalResolution;
    private Vector3 inputVelocity;
    private Vector3 gravity;
    private float finalTime;    

    private float tempTime = 0.0f;

    private ProjectileHandler projectileHandler; 
       
    public void Initialize(ProjectileHandler ph, float timeIntervalResolution, Vector3 gravity, Vector3 inputVelocity)
    {
        this.projectileHandler = ph;
        this.gravity = gravity;
        this.inputVelocity = inputVelocity;
        this.finalTime = ph.CalculateProjectileTime(inputVelocity, gravity);
        this.timeIntervalResolution = timeIntervalResolution;        
    }

    public void LocalFixedUpdate(float dt)
    {
        accumulatedTime += dt;
        if (accumulatedTime >= timeIntervalResolution)
        {
            var intermediateVelocity = projectileHandler.CalculateProjectileVelocity(inputVelocity, gravity, tempTime);
            transform.position += intermediateVelocity;

            tempTime += timeIntervalResolution;
            
            accumulatedTime -= timeIntervalResolution;
        }
        if (tempTime >= finalTime)
        {
            // TODO: we calculate radial aoe to see who it hits. For client, we call any effects we want to display. For server, we inflict damage to objects
            Collider[] hs = Physics.OverlapSphere(transform.position, LocalGame.Instance.GameData.ProjectileRadius, LocalGame.Instance.GameData.UnitAttackableLayer);
            Collider[] notOwnerHS = hs.Where( x=> (x.GetComponent<NetworkUnit>().OwnerConnectionIndexPlusOne.Value - 1) != LocalGame.Instance.MyNetworkGameInstance.ConnectionIndex.Value).ToArray();

            foreach(var c in notOwnerHS)
            {
                if (NetworkManager.Singleton.IsServer) // we only trigger damage on server side
                {
                    var unitID = c.GetComponent<NetworkUnit>().UnitID.Value;
                    c.GetComponent<NetworkUnit>().ExecuteProjectileAttackRpc(unitID);
                }                
            }
            projectileHandler.Destroy(this);
        }
    }
}
