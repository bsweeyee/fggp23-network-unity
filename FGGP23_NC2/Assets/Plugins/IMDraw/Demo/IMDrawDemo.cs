using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class IMDrawDemo : MonoBehaviour
{
    [SerializeField][Range(0, 1)] private float n1 = 1.0f;
    [SerializeField][Range(0, 1)] private float n2 = 1.0f;
    [SerializeField][Range(0, 10)] private float radius = 1.0f;
    [SerializeField][Range(0, 10)] private float offset = 1.0f;

    void Update()
    {
        Vector3 u0 = Vector3.Lerp(Vector3.right, Vector3.forward, n1);
        Vector3 u1 = Vector3.Lerp(u0.normalized, Vector3.up, n2);
        Vector3 u2 = (u0.normalized + u1.normalized).normalized;
        using (new IMDraw.PrimitiveScope())
        {                
            IMDraw.PrimitiveScope.Offset = offset;
            // IMDraw.Primitive.Disc(transform.position, u, 5);
            IMDraw.Primitive.Line(transform.position, transform.position + u2.normalized * 4, radius);
        }
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Vector3 u0 = Vector3.Lerp(Vector3.right, Vector3.forward, n1);
        Vector3 u1 = Vector3.Lerp(u0.normalized, Vector3.up, n2);
        Vector3 u2 = (u0.normalized + u1.normalized).normalized;
        Vector3 start = transform.position;
        Vector3 end =  transform.position + u2.normalized * 4;

        Vector3 l = end - start;

        // Matrix4x4 r = new Matrix4x4();
        // r.SetColumn(0, new Vector4(1, 0, 0, 0));
        // r.SetColumn(1, new Vector4(0, 0, 1, 0));
        // r.SetColumn(2, new Vector4(0, -1, 0, 0));
        // r.SetColumn(3, new Vector4(0, 0, 0, 1));
        // Vector3 lr = r * l;

        Vector3 L = Vector3.ProjectOnPlane(l, Vector3.up);
        Vector3 h = l - L;

        Vector3 A = start + L + Vector3.Cross(l, L).normalized * radius;
        Vector3 B = start + L + Vector3.Cross(L, l).normalized * radius;
        Vector3 C = start + L + h + Vector3.Cross(L, l).normalized * radius + l.normalized * radius;
        Vector3 D = start + L + h + Vector3.Cross(l, L).normalized * radius + l.normalized * radius;
        Vector3 E = start + h + Vector3.Cross(L, l).normalized * radius;
        Vector3 F = start + h + Vector3.Cross(l, L).normalized * radius;
        Vector3 G = start + Vector3.Cross(l, L).normalized * radius - l.normalized * radius;
        Vector3 H = start + Vector3.Cross(L, l).normalized * radius - l.normalized * radius;

        Gizmos.DrawSphere(A, 0.05f);           
        Gizmos.DrawSphere(B, 0.05f);           
        Gizmos.DrawSphere(C, 0.05f);           
        Gizmos.DrawSphere(D, 0.05f);           
        Gizmos.DrawSphere(E, 0.05f);           
        Gizmos.DrawSphere(F, 0.05f);           
        Gizmos.DrawSphere(G, 0.05f);           
        Gizmos.DrawSphere(H, 0.05f);
        Handles.DrawWireDisc(start, u2, radius);            
        Handles.DrawWireDisc(end, u2, radius);            

        Handles.Label(A + Vector3.up * 0.15f, "A");
        Handles.Label(B + Vector3.up * 0.15f, "B");
        Handles.Label(C + Vector3.up * 0.15f, "C");
        Handles.Label(D + Vector3.up * 0.15f, "D");
        Handles.Label(E + Vector3.up * 0.15f, "E");
        Handles.Label(F + Vector3.up * 0.15f, "F");
        Handles.Label(G + Vector3.up * 0.15f, "G");
        Handles.Label(H + Vector3.up * 0.15f, "H");

        Gizmos.DrawLine(start, end);
        // Gizmos.DrawLine(start, start + lr);                      
    }
    #endif
}
