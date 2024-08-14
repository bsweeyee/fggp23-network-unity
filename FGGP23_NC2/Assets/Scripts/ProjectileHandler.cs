using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileHandler : MonoBehaviour
{
    [SerializeField] private Vector3 initialProjectileVelocity;
    [SerializeField] private Vector3 gravity;
    private Vector3 targetPosition;

    float CalculateProjectileTime(Vector3 v, Vector3 g)
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

    Vector3 CalculateProjectileVelocity(Vector3 u, Vector3 a, float t, bool isZ = true)
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


    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + initialProjectileVelocity * 10.0f);
        
        var tempPos = transform.position;
        var tempTime = 0.0f;

        var currentVelocity = initialProjectileVelocity;
        var t = CalculateProjectileTime(currentVelocity, gravity);
        var dt = t;

        while (t > 0)
        {
            var intermediateVelocity = CalculateProjectileVelocity(currentVelocity, gravity, tempTime);

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(tempPos, tempPos + intermediateVelocity);

            tempPos += intermediateVelocity;
            tempTime += 0.01f;
            t -= 0.01f;                         
        }
    }
    #endif
}
