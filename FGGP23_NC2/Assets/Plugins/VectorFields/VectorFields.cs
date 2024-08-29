using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class VectorFields : MonoBehaviour
{
    [SerializeField] private AnimationCurve fx;
    [SerializeField] private AnimationCurve fy;
    [SerializeField] private Vector2Int size = new Vector2Int(20, 20);
    [SerializeField] private float gridInterval = 1.0f;
    [SerializeField] private float speed = 1.0f;

    private List<Vector3> particlePositions = new List<Vector3>();

    private Vector2 min;
    private Vector2 max;

    void Start()
    {
        min = new Vector2(transform.position.x - (gridInterval * size.x/2), transform.position.z - (gridInterval * size.y/2));
        max = new Vector2(transform.position.x + (gridInterval * size.x/2), transform.position.z + (gridInterval * size.y/2));        

        Vector3 startPosition = new Vector3(min.x, transform.position.y, min.y);
        for (int i = 0; i<size.x; i++)
        {
            for (int j=0; j<size.y; j++)
            {
                Vector3 pos = new Vector3(startPosition.x + i*gridInterval, 0, startPosition.z + j*gridInterval);
                particlePositions.Add(pos);
            }
        }
        IMDraw.PrimitiveScope.Initialize();
    }
    
    void Update()
    {
        for (int i = 0; i<particlePositions.Count; i++)
        {
            IMDraw.PrimitiveScope.BeginScope();
            IMDraw.Primitive.SphereSDF(particlePositions[i], 0.4f);
            IMDraw.PrimitiveScope.EndScope();

            Vector3 nextPos = particlePositions[i] + Get2DVectorDirection(particlePositions[i]) * Time.deltaTime * speed;
            nextPos.x = Mathf.Clamp(nextPos.x, min.x, max.x);
            nextPos.z = Mathf.Clamp(nextPos.z, min.y, max.y);
            particlePositions[i] = nextPos;
            // if ((nextPos - particlePositions[i]).magnitude < 0.01f)
            // {
            //     particlePositions[i] = new Vector3(min.x + i/size.x *gridInterval, 0, min.y + (i/size.y + i%size.y)*gridInterval);
            // }
            // else
            // {
            //     particlePositions[i] = nextPos;
            // }
        }       
    }

    Vector3 Get2DVectorDirection(Vector3 position)
    {
        Vector3 d = Vector2.zero;
        float x = fx.Evaluate(Mathf.InverseLerp(-size.x/2, size.x/2, position.x));
        float y = fy.Evaluate(Mathf.InverseLerp(-size.y/2, size.y/2, position.z));

        d = new Vector3(x, 0, y).normalized;
        return d;
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {        
        Vector3 startPosition = new Vector3(transform.position.x - gridInterval * size.x/2, transform.position.y, transform.position.z - gridInterval * size.y/2);

        for (int i = 0; i < size.x; i++)
        {
            for (int j =0; j<size.y; j++)
            {
                Vector3 pos = new Vector3(startPosition.x + i*gridInterval, 0, startPosition.z + j*gridInterval);
                float x = fx.Evaluate(Mathf.InverseLerp(-size.x/2, size.x/2, pos.x));
                float y = fy.Evaluate(Mathf.InverseLerp(-size.y/2, size.y/2, pos.z));

                Vector3 direction = new Vector3(x, 0, y).normalized;

                Gizmos.DrawLine(pos, pos + direction);
            }
        }
    }
    #endif
}
