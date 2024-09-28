using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IMDraw;
using Unity.Netcode;

public class RVOAgent : MonoBehaviour
{
    [SerializeField] private float radius = 0.5f;
    [SerializeField] private float speed = 0.1f;
    [SerializeField] private float collisionSpeedUpFactor = 1f;
    
    private Vector3 p1;
    private Vector3 p2;
    private Vector3 v1;
    private Vector3 v2;

    public Vector3 Velocity;
    public Vector3 iVelocity;
    public Vector3 iPosition;
    public Vector3 Goal;

    public float Radius { get { return radius; } }

    void Update()
    {
        Collider[] results = new Collider[99];
        int numOfHits = Physics.OverlapSphereNonAlloc(transform.position, 99, results);    
        
        Vector3 averageVelocity = Vector3.zero;
        int total = 0;
        // for(int i=0; i<numOfHits; i++)
        // {
            // var other = results[i].GetComponent<RVOAgent>();
            // if (other != this)
            // {
            // }
        //     averageVelocity += other.Velocity;
        //     total++;
        // }
        // averageVelocity /= total;

        bool isDrawiVelocity = false;

        for(int i=0; i<numOfHits; i++)
        {
            var other = results[i].GetComponent<RVOAgent>();
            if (other != this)
            {                
                // var otherVelocity = other.Velocity;
                var otherVelocity = (other.Velocity + Velocity) * 0.5f;              
                var ownVelocity = Velocity;

                var dist = Vector3.Distance(other.transform.position, transform.position);
                var rSum = (other.Radius + radius);

                Vector3 relativePosition = other.transform.position - transform.position;
                // Debug.Log( $"{name}: {dist}, {rSum}");

                if (dist > rSum)
                {                    
                    // calculate avoidance velocity
                    
                    // TODO recalculate v1 and v2 to the proper tangential velocity
                    var angle = Mathf.Asin(rSum / dist);
                    Vector3 pDir = Vector3.Cross(relativePosition.normalized, -Vector3.forward);
                    var n = angle / (Mathf.PI / 2);
                    
                    v1 = Vector3.Lerp(relativePosition.normalized, pDir, n);
                    v2 = Vector3.Lerp(relativePosition.normalized, -pDir, n);
                    
                    p1 = transform.position + v1 * Vector3.Distance(other.transform.position, transform.position) * Mathf.Cos(angle);
                    p2 = transform.position + v2 * Vector3.Distance(other.transform.position, transform.position) * Mathf.Cos(angle);
                }

                // p1 = other.transform.position + pDir * other.Radius;
                // p2 = other.transform.position + -pDir * other.Radius;

                // v1 = (p1 - transform.position).normalized;
                // v2 = (p2 - transform.position).normalized;

                // check if other velocity and own velocity will intersect with Minkowski sum of other on self
                var relativeVel = ownVelocity - otherVelocity;                
                Vector3 newPosition = transform.position + (relativeVel * speed * Time.deltaTime);
                
                using (new IMDraw.PrimitiveScope())
                {                    
                    IMDraw.Primitive.Disc(other.transform.position, -Vector3.forward, other.Radius + radius, Color.cyan);
                }                

                // Choose v1 or v2 by taking the dot product that is larger?
                float dot1 = Vector3.Dot(relativePosition.normalized, v1.normalized);
                float dot2 = Vector3.Dot(relativePosition.normalized, v2.normalized);
                if (dot1 < dot2) {
                    iVelocity = v1.normalized * speed * collisionSpeedUpFactor;
                    iPosition = p1;
                }
                else { 
                    iVelocity = v2.normalized * speed * collisionSpeedUpFactor;
                    iPosition = p2;
                }
                
                if (dist < rSum)
                {   
                    Velocity = iVelocity; 
                    isDrawiVelocity = true;                   
                }
                else
                {
                    // We continue towards goal
                    Velocity = (Goal - transform.position).normalized * speed;
                }
            }
            else
            {
                // We continue towards goal
                Velocity = (Goal - transform.position).normalized * speed;
            }

            transform.position += Velocity * Time.deltaTime;
        }

        using (new IMDraw.PrimitiveScope())
        {
            IMDraw.Primitive.Disc(transform.position, -Vector3.forward, radius, Color.black);
            IMDraw.Primitive.Disc(Goal, -Vector3.forward, 0.2f, Color.green);
            // IMDraw.Primitive.Disc(p1, -Vector3.forward, 0.1f, Color.red);
            // IMDraw.Primitive.Disc(p2, -Vector3.forward, 0.1f, Color.red);
            
            // IMDraw.Primitive.Line(transform.position, p1 , Color.red);
            // IMDraw.Primitive.Line(transform.position, p2, Color.red);
            if (isDrawiVelocity)
            {
                IMDraw.Primitive.Disc(iPosition, -Vector3.forward, 0.1f, Color.red);
                IMDraw.Primitive.Line(iPosition, iVelocity, Color.white);
            }
        }
    }
}
