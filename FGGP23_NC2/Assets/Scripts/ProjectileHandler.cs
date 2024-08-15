using System.Collections;
using System.Collections.Generic;
using FGNetworkProgramming;
using Unity.Networking.Transport.Utilities;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class ProjectileHandler : MonoBehaviour 
{
    [Range(0, 1)][SerializeField] private float projectileForwardStrength = 0.2f;
    [Range(0, 1)][SerializeField] private float normalizedForwardDirection = 0.5f;
    [SerializeField] private GameObject mesh;
    [SerializeField] private GameObject projectilePoint;
    [SerializeField] private GameObject projectileTarget;

    private int ownerConnectionIndex;
    private float timeIntervalResolution = 0.01f;
    private LinkedList<Projectile> projectilesList;
    private List<GameObject> projectileCurve;

    public int OwnerConnectionIndex { get { return ownerConnectionIndex; } }    

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
        this.ownerConnectionIndex = ownerConnectionId;
        if (this.ownerConnectionIndex == LocalGame.Instance.MyNetworkGameInstance.ConnectionIndex.Value)
        {
            // handle input here
            FGNetworkProgramming.Input.Instance.OnHandleKeyboardInput.AddListener(OnHandleKeyboardInput);
            projectileTarget.gameObject.SetActive(true);
        }
        else
        {
            projectileTarget.gameObject.SetActive(false);        
        }
        projectilePoint.gameObject.SetActive(false);

        projectileCurve = new List<GameObject>();               
        for(int i=0; i<20; i++)
        {
            var pp = Instantiate(projectilePoint, transform.position, Quaternion.identity, transform);            
            projectileCurve.Add(pp);
            pp.gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        FGNetworkProgramming.Input.Instance.OnHandleKeyboardInput.RemoveListener(OnHandleKeyboardInput);
        var e = projectilesList.First;
        while (e != null)
        {
            var next = e.Next;
            Destroy(e.Value);
            e = next;          
        }
        projectilesList.Clear();
    }
    
    void Update()
    {
        if (ownerConnectionIndex != LocalGame.Instance.MyNetworkGameInstance.ConnectionIndex.Value) return;

        var forwardDirection = -Mathf.Sign(Vector3.Dot(transform.forward, Vector3.forward)) * Vector3.Slerp(transform.right, -transform.right, normalizedForwardDirection);        
        var currentVelocity = forwardDirection * projectileForwardStrength + new Vector3(0, LocalGame.Instance.GameData.ProjectileUpStrength, 0);
        
        var tempPos = transform.position;
        var tempTime = 0.0f;
        var t = CalculateProjectileTime(currentVelocity, LocalGame.Instance.GameData.ProjectileGravity);
        var finalPos = transform.position;
        var targetPos = transform.position;
        
        int index=0;
        int a = 0;
        int n = Mathf.FloorToInt(t / timeIntervalResolution);
        while (t > 0)
        {
            var intermediateVelocity = CalculateProjectileVelocity(currentVelocity, LocalGame.Instance.GameData.ProjectileGravity, tempTime);
            finalPos += intermediateVelocity;            
            if (a < n) targetPos += intermediateVelocity;

            var newPos = tempPos + intermediateVelocity;
            
            tempPos += intermediateVelocity;
            tempTime += timeIntervalResolution;
            t -= timeIntervalResolution;                         
            
            if (a%3 == 0)
            {
                projectileCurve[index].transform.position = newPos;
                projectileCurve[index].gameObject.SetActive(true);
                index++;
            }
            a++;
        }

        projectileTarget.transform.position = targetPos;        
        for(int i=index; i<projectileCurve.Count; i++)
        {
            projectileCurve[index].gameObject.SetActive(false);
        }         
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
            var dotSign = Mathf.Sign(Vector3.Dot(transform.forward, Vector3.forward));
            switch(gameInput)
            {
                case EGameInput.W:
                projectileForwardStrength = Mathf.Clamp(projectileForwardStrength + (LocalGame.Instance.GameData.ForwardStrengthAdjustmentSpeed  * Time.deltaTime), LocalGame.Instance.GameData.MinForwardStrength, LocalGame.Instance.GameData.MaxForwardStrength);
                break;
                case EGameInput.A:                
                normalizedForwardDirection = Mathf.Clamp(normalizedForwardDirection - dotSign*(LocalGame.Instance.GameData.ForwardDirectionAdjustmentSpeed * Time.deltaTime), LocalGame.Instance.GameData.MinNormalizedDirection, LocalGame.Instance.GameData.MaxNormalizedDirection);
                mesh.transform.forward = -dotSign * Vector3.Slerp(transform.right, -transform.right, normalizedForwardDirection);
                break;
                case EGameInput.S:
                projectileForwardStrength = Mathf.Clamp(projectileForwardStrength - (LocalGame.Instance.GameData.ForwardStrengthAdjustmentSpeed * Time.deltaTime), LocalGame.Instance.GameData.MinForwardStrength, LocalGame.Instance.GameData.MaxForwardStrength);;
                break;
                case EGameInput.D:
                normalizedForwardDirection = Mathf.Clamp(normalizedForwardDirection + dotSign*(LocalGame.Instance.GameData.ForwardDirectionAdjustmentSpeed * Time.deltaTime), LocalGame.Instance.GameData.MinNormalizedDirection, LocalGame.Instance.GameData.MaxNormalizedDirection);
                mesh.transform.forward = -dotSign * Vector3.Slerp(transform.right, -transform.right, normalizedForwardDirection);
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
        Gizmos.DrawWireSphere(transform.position, 0.25f);
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
