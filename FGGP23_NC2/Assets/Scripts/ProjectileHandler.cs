using System.Collections;
using System.Collections.Generic;
using FGNetworkProgramming;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class ProjectileHandler : MonoBehaviour
{
    [Range(0, 1)][SerializeField] private float projectileForwardStrength = 0.2f;
    [Range(0, 1)][SerializeField] private float normalizedForwardDirection = 0.5f;
    
    private int ownerConnectionId;
    private float timeIntervalResolution = 0.01f;
    private LinkedList<Projectile> projectilesList;    

    public float CalculateProjectileTime(Vector3 v, Vector3 g)
	{
		var h = -Mathf.Pow(v.y, 2) / (2 * g.y);
		var a = g.y;
		var b = 2 * v.y;
		var c = -2 * h;
		var b2m4ac = (Mathf.Pow(b, 2) - 4 * a * c);
		b2m4ac = b2m4ac < 0 ? 0 : b2m4ac;

		var t1 = (-b + Mathf.Sqrt(b2m4ac)) / (2 * a);
		var t2 = (-b - Mathf.Sqrt(b2m4ac)) / (2 * a);

		var t = (t1 > 0) ? t1 : (t2 > 0) ? t2 : 0;
		t *= 2;

		return t;
	}

    public Vector3 CalculateProjectileVelocity(Vector3 u, Vector3 a, float t, bool isZ = true)
	{
		var ux = isZ ? u.z : u.x;
		var ax = isZ ? a.z : a.x;

		var uz = isZ ? u.x : u.z;
		var az = isZ ? a.x : a.z;

		var vx = ux + ax * t;
		var vy = u.y + a.y * t;
		var vz = uz + az * t;

		return isZ ? new Vector3(vz, vy, vx) : new Vector3(vx, vy, vz);
	}

    public Vector3 CalculateDeltaPosition(Vector3 u, float t, Vector3 g)
    {
        return u*t + 0.5f*g*Mathf.Pow(t, 2);
    }
        
    public void Initialize(int ownerConnectionId)
    {
        projectilesList = new LinkedList<Projectile>();
        this.ownerConnectionId = ownerConnectionId;
        if (this.ownerConnectionId == LocalGame.Instance.MyNetworkGameInstance.ConnectionIndex.Value)
        {
            // handle input here
            FGNetworkProgramming.Input.Instance.OnHandleKeyboardInput.AddListener(OnHandleKeyboardInput);
        }                
    }

    void OnDestroy()
    {
        FGNetworkProgramming.Input.Instance.OnHandleKeyboardInput.RemoveListener(OnHandleKeyboardInput);
    }
    
    void FixedUpdate()
    {
        var e = projectilesList.First;
        while (e != null)
        {
            var next = e.Next;
            e.Value.LocalFixedUpdate(Time.fixedDeltaTime);                            
            e = next;            
        }
    }

    public void Spawn(int ownerConnectionId)
    {
        var forwardDirection = -Mathf.Sign(Vector3.Dot(transform.forward, Vector3.forward)) * Vector3.Slerp(transform.right, -transform.right, normalizedForwardDirection);
        var inputVelocity = forwardDirection * projectileForwardStrength + new Vector3(0, LocalGame.Instance.GameData.ProjectileUpStrength, 0);
        LocalGame.Instance.MyNetworkGameInstance.SpawnProjectileServerRpc(ownerConnectionId, transform.position, transform.rotation, inputVelocity);        
    }

    public void CreateProjectile(Vector3 position, Quaternion rotation, Vector3 inputVelocity)
    {
        var projectile = Instantiate(LocalGame.Instance.GameData.ProjectilePrefab, position, rotation);
        projectile.Initialize(this, timeIntervalResolution, LocalGame.Instance.GameData.ProjectileGravity, inputVelocity);
        projectilesList.AddLast(projectile);
    }

    public void Destroy(Projectile p)
    {
        projectilesList.Remove(p);
        Destroy(p.gameObject);
    }
    
    void OnHandleKeyboardInput(EGameInput gameInput, EInputState inputState)
    {        
        if (inputState == EInputState.HOLD)
        {
            switch(gameInput)
            {
                case EGameInput.W:
                projectileForwardStrength = Mathf.Clamp(projectileForwardStrength + (LocalGame.Instance.GameData.ForwardStrengthAdjustmentSpeed  * Time.deltaTime), LocalGame.Instance.GameData.MinForwardStrength, LocalGame.Instance.GameData.MaxForwardStrength);
                break;
                case EGameInput.A:                
                normalizedForwardDirection = Mathf.Clamp(normalizedForwardDirection - (LocalGame.Instance.GameData.ForwardDirectionAdjustmentSpeed * Time.deltaTime), LocalGame.Instance.GameData.MinNormalizedDirection, LocalGame.Instance.GameData.MaxNormalizedDirection);
                break;
                case EGameInput.S:
                projectileForwardStrength = Mathf.Clamp(projectileForwardStrength - (LocalGame.Instance.GameData.ForwardStrengthAdjustmentSpeed * Time.deltaTime), LocalGame.Instance.GameData.MinForwardStrength, LocalGame.Instance.GameData.MaxForwardStrength);;
                break;
                case EGameInput.D:
                normalizedForwardDirection = Mathf.Clamp(normalizedForwardDirection + (LocalGame.Instance.GameData.ForwardDirectionAdjustmentSpeed * Time.deltaTime), LocalGame.Instance.GameData.MinNormalizedDirection, LocalGame.Instance.GameData.MaxNormalizedDirection);
                break;
            }
        }
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {        
        var forwardDirection = -Mathf.Sign(Vector3.Dot(transform.forward, Vector3.forward)) * Vector3.Slerp(transform.right, -transform.right, normalizedForwardDirection);        
        var currentVelocity = forwardDirection * projectileForwardStrength + new Vector3(0, LocalGame.Instance.GameData.ProjectileUpStrength, 0);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + currentVelocity * 10);
        Gizmos.DrawLine(transform.position, transform.position + (forwardDirection * projectileForwardStrength));
        
        var tempPos = transform.position;
        var tempTime = 0.0f;
        
        var t = CalculateProjectileTime(currentVelocity, LocalGame.Instance.GameData.ProjectileGravity);
        var finalPos = transform.position; 

        while (t > 0)
        {
            var intermediateVelocity = CalculateProjectileVelocity(currentVelocity, LocalGame.Instance.GameData.ProjectileGravity, tempTime);
            finalPos += intermediateVelocity;

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(tempPos, tempPos + intermediateVelocity);

            tempPos += intermediateVelocity;
            tempTime += timeIntervalResolution;
            t -= timeIntervalResolution;                         
        }
        Handles.color = Color.yellow;
        Handles.DrawSolidDisc(finalPos, Vector3.up, LocalGame.Instance.GameData.ProjectileRadius);
    }
#endif
}
