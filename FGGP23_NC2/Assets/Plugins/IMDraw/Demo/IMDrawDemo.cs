using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class IMDrawDemo : MonoBehaviour
{
    public enum EDrawType
    {
        LINE,
        TORUS
    }

    [SerializeField] private EDrawType DrawType = EDrawType.LINE;
    [SerializeField] private bool isInvertForward = false;
    [SerializeField][Range(0, 1)] private float n1 = 1.0f;
    [SerializeField][Range(0, 1)] private float n2 = 1.0f;
    [SerializeField][Range(0, 10)] private float radius = 1.0f;
    [SerializeField][Range(0, 10)] private float minorRadius = 0.1f;
    [SerializeField][Range(0, 10)] private float length = 1.0f;
    
    [SerializeField] private MeshRenderer discRenderer;    

    void Start()
    {
        IMDraw.PrimitiveScope.Initialize();
    }
    void Update()
    {
        UnityEngine.Vector3 forward = (isInvertForward) ? -UnityEngine.Vector3.forward : UnityEngine.Vector3.forward;
        UnityEngine.Vector3 u0 = (n1 >= 0.5f) ? UnityEngine.Vector3.Lerp(UnityEngine.Vector3.right, forward, Mathf.InverseLerp(1, 0.5f, n1)) : UnityEngine.Vector3.Lerp(-UnityEngine.Vector3.right, forward, Mathf.InverseLerp(0, 0.5f, n1));
        UnityEngine.Vector3 u1 = (n2 >= 0.5f) ? UnityEngine.Vector3.Lerp(UnityEngine.Vector3.up, u0.normalized, Mathf.InverseLerp(1, 0.5f, n2)).normalized : UnityEngine.Vector3.Lerp(-UnityEngine.Vector3.up, u0.normalized, Mathf.InverseLerp(0, 0.5f, n2)).normalized;
        
        switch (DrawType)
        {
            case EDrawType.LINE:
            IMDraw.PrimitiveScope.BeginScope();
            IMDraw.Primitive.LineSDF(transform.position, transform.position + u1.normalized * length, radius);
            IMDraw.PrimitiveScope.EndScope();
            break;
            case EDrawType.TORUS:
            Vector3 u = u1;
            
            IMDraw.PrimitiveScope.BeginScope();
            IMDraw.Primitive.DiscSDF(transform.position, u, radius, minorRadius);
            IMDraw.PrimitiveScope.EndScope();
            break;
        }                        
        // IMDraw.Primitive.Line(transform.position, transform.position + u1.normalized * length);
    }    

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (DrawType == EDrawType.LINE)
        {
            float offset = 1.0f;
            UnityEngine.Vector3 forward = (isInvertForward) ? -UnityEngine.Vector3.forward : UnityEngine.Vector3.forward;
            UnityEngine.Vector3 u0 = (n1 >= 0.5f) ? UnityEngine.Vector3.Lerp(UnityEngine.Vector3.right, forward, Mathf.InverseLerp(1, 0.5f, n1)) : UnityEngine.Vector3.Lerp(-UnityEngine.Vector3.right, forward, Mathf.InverseLerp(0, 0.5f, n1));
            UnityEngine.Vector3 u1 = (n2 >= 0.5f) ? UnityEngine.Vector3.Lerp(UnityEngine.Vector3.up, u0.normalized, Mathf.InverseLerp(1, 0.5f, n2)).normalized : UnityEngine.Vector3.Lerp(-UnityEngine.Vector3.up, u0.normalized, Mathf.InverseLerp(0, 0.5f, n2)).normalized;

            UnityEngine.Vector3 start = transform.position;
            UnityEngine.Vector3 end = transform.position + u1 * length;

            UnityEngine.Vector3 l = end - start;
            UnityEngine.Vector3 L = UnityEngine.Vector3.ProjectOnPlane(l, UnityEngine.Vector3.up);
            float lDotRNormalized = UnityEngine.Vector3.Dot(L.normalized, UnityEngine.Vector3.right);
            float lDotFNormalized = UnityEngine.Vector3.Dot(L.normalized, UnityEngine.Vector3.forward);
            float lDotUNormalized = UnityEngine.Vector3.Dot(l.normalized, UnityEngine.Vector3.up);

            UnityEngine.Vector3 Lr = UnityEngine.Vector3.Dot(L, UnityEngine.Vector3.right) * UnityEngine.Vector3.right;
            UnityEngine.Vector3 Lf = UnityEngine.Vector3.Dot(L, UnityEngine.Vector3.forward) * UnityEngine.Vector3.forward;
            UnityEngine.Vector3 Lh = l - L;

            UnityEngine.Vector3 A = (lDotRNormalized > 0) ? start : start + Lr;
            UnityEngine.Vector3 B = (lDotRNormalized > 0) ? start + Lr : start;
            UnityEngine.Vector3 C = (lDotRNormalized > 0) ? start + Lr + Lh : start + Lh;
            UnityEngine.Vector3 D = (lDotRNormalized > 0) ? start + Lh : start + Lr + Lh;
            UnityEngine.Vector3 E = (lDotRNormalized > 0) ? start + Lf + Lr + Lh : start + Lf + Lh;
            UnityEngine.Vector3 F = (lDotRNormalized > 0) ? start + Lf + Lh : start + Lf + Lr + Lh;
            UnityEngine.Vector3 G = (lDotRNormalized > 0) ? start + Lf : start + Lf + Lr;
            UnityEngine.Vector3 H = (lDotRNormalized > 0) ? start + Lf + Lr : start + Lf;

            A = (lDotFNormalized > 0) ? A : A + Lf;
            B = (lDotFNormalized > 0) ? B : B + Lf;
            C = (lDotFNormalized > 0) ? C : C + Lf;
            D = (lDotFNormalized > 0) ? D : D + Lf;

            E = (lDotFNormalized > 0) ? E : E - Lf;
            F = (lDotFNormalized > 0) ? F : F - Lf;
            G = (lDotFNormalized > 0) ? G : G - Lf;
            H = (lDotFNormalized > 0) ? H : H - Lf;

            A = (lDotUNormalized > 0) ? A : A + Lh;
            B = (lDotUNormalized > 0) ? B : B + Lh;
            C = (lDotUNormalized > 0) ? C : C - Lh;       
            D = (lDotUNormalized > 0) ? D : D - Lh;       
            
            E = (lDotUNormalized > 0) ? E : E - Lh;
            F = (lDotUNormalized > 0) ? F : F - Lh;
            G = (lDotUNormalized > 0) ? G : G + Lh;       
            H = (lDotUNormalized > 0) ? H : H + Lh;

            UnityEngine.Vector3 center = start + Lr/2 + Lf/2 + Lh/2;
            UnityEngine.Vector3 A_direction = (A - center).normalized;
            UnityEngine.Vector3 B_direction = (B - center).normalized;
            UnityEngine.Vector3 C_direction = (C - center).normalized;
            UnityEngine.Vector3 D_direction = (D - center).normalized;
            UnityEngine.Vector3 E_direction = (E - center).normalized;
            UnityEngine.Vector3 F_direction = (F - center).normalized;
            UnityEngine.Vector3 G_direction = (G - center).normalized;
            UnityEngine.Vector3 H_direction = (H - center).normalized;

            A += A_direction * radius* offset;                
            B += B_direction * radius* offset;                
            C += C_direction * radius* offset;                
            D += D_direction * radius* offset;                
            E += E_direction * radius* offset;                
            F += F_direction * radius* offset;                
            G += G_direction * radius* offset;                
            H += H_direction * radius* offset;

            float rIV = (lDotRNormalized > 0) ? Vector3Extension.InverseLerp(UnityEngine.Vector3.right * length, UnityEngine.Vector3.zero, Lr) : Vector3Extension.InverseLerp(-UnityEngine.Vector3.right * length, UnityEngine.Vector3.zero, Lr);
            float fIV = (lDotFNormalized > 0) ? Vector3Extension.InverseLerp(UnityEngine.Vector3.forward * length, UnityEngine.Vector3.zero, Lf) : Vector3Extension.InverseLerp(-UnityEngine.Vector3.forward * length, UnityEngine.Vector3.zero, Lf);
            float uIV = (lDotUNormalized > 0) ? Vector3Extension.InverseLerp(UnityEngine.Vector3.up * length, UnityEngine.Vector3.zero, Lh) : Vector3Extension.InverseLerp(-UnityEngine.Vector3.up * length, UnityEngine.Vector3.zero, Lh);

            UnityEngine.Vector3 Ao = A - fIV * UnityEngine.Vector3.forward * radius - Mathf.Clamp(rIV, 0, 1) * UnityEngine.Vector3.right * radius - uIV * UnityEngine.Vector3.up * radius;
            UnityEngine.Vector3 Bo = B - fIV * UnityEngine.Vector3.forward * radius + Mathf.Clamp(rIV, 0, 1) * UnityEngine.Vector3.right * radius - uIV * UnityEngine.Vector3.up * radius;
            UnityEngine.Vector3 Co = C - fIV * UnityEngine.Vector3.forward * radius + Mathf.Clamp(rIV, 0, 1) * UnityEngine.Vector3.right * radius + uIV * UnityEngine.Vector3.up * radius;
            UnityEngine.Vector3 Do = D - fIV * UnityEngine.Vector3.forward * radius - Mathf.Clamp(rIV, 0, 1) * UnityEngine.Vector3.right * radius + uIV * UnityEngine.Vector3.up * radius;
            UnityEngine.Vector3 Eo = E + fIV * UnityEngine.Vector3.forward * radius + Mathf.Clamp(rIV, 0, 1) * UnityEngine.Vector3.right * radius + uIV * UnityEngine.Vector3.up * radius;
            UnityEngine.Vector3 Fo = F + fIV * UnityEngine.Vector3.forward * radius - Mathf.Clamp(rIV, 0, 1) * UnityEngine.Vector3.right * radius + uIV * UnityEngine.Vector3.up * radius;
            UnityEngine.Vector3 Go = G + fIV * UnityEngine.Vector3.forward * radius - Mathf.Clamp(rIV, 0, 1) * UnityEngine.Vector3.right * radius - uIV * UnityEngine.Vector3.up * radius;
            UnityEngine.Vector3 Ho = H + fIV * UnityEngine.Vector3.forward * radius + Mathf.Clamp(rIV, 0, 1) * UnityEngine.Vector3.right * radius - uIV * UnityEngine.Vector3.up * radius;

            // Vector3 A = start + L + Vector3.Cross(l, L).normalized * radius;
            // Vector3 B = start + L + Vector3.Cross(L, l).normalized * radius;
            // Vector3 C = start + L + Lh + Vector3.Cross(L, l).normalized * radius;
            // Vector3 D = start + L + Lh + Vector3.Cross(l, L).normalized * radius;
            // Vector3 E = start + Lh + Vector3.Cross(L, l).normalized * radius;
            // Vector3 F = start + Lh + Vector3.Cross(l, L).normalized * radius;
            // Vector3 G = start + Vector3.Cross(l, L).normalized * radius;
            // Vector3 H = start + Vector3.Cross(L, l).normalized * radius;
            
            Gizmos.DrawSphere(center, 0.2f);
            Gizmos.DrawSphere(Ao, 0.05f);           
            Gizmos.DrawSphere(Bo, 0.05f);           
            Gizmos.DrawSphere(Co, 0.05f);           
            Gizmos.DrawSphere(Do, 0.05f);           
            Gizmos.DrawSphere(Eo, 0.05f);           
            Gizmos.DrawSphere(Fo, 0.05f);           
            Gizmos.DrawSphere(Go, 0.05f);           
            Gizmos.DrawSphere(Ho, 0.05f);
            Handles.DrawWireDisc(start, u1, radius);            
            Handles.DrawWireDisc(end, u1, radius);

            Handles.Label(Ao + UnityEngine.Vector3.up * 0.15f, "A");
            Handles.Label(Bo + UnityEngine.Vector3.up * 0.15f, "B");
            Handles.Label(Co + UnityEngine.Vector3.up * 0.15f, "C");
            Handles.Label(Do + UnityEngine.Vector3.up * 0.15f, "D");
            Handles.Label(Eo + UnityEngine.Vector3.up * 0.15f, "E");
            Handles.Label(Fo + UnityEngine.Vector3.up * 0.15f, "F");
            Handles.Label(Go + UnityEngine.Vector3.up * 0.15f, "G");
            Handles.Label(Ho + UnityEngine.Vector3.up * 0.15f, "H");

            Gizmos.DrawLine(start, end);
            Gizmos.DrawLine(start, L);
            Gizmos.DrawLine(start, Lr);
            Gizmos.DrawLine(start, Lf);
            Gizmos.DrawLine(start, Lh);
        }
        else if (DrawType == EDrawType.TORUS)
        {
            UnityEngine.Vector3 forward = (isInvertForward) ? -UnityEngine.Vector3.forward : UnityEngine.Vector3.forward;
            UnityEngine.Vector3 u0 = (n1 >= 0.5f) ? UnityEngine.Vector3.Lerp(UnityEngine.Vector3.right, forward, Mathf.InverseLerp(1, 0.5f, n1)) : UnityEngine.Vector3.Lerp(-UnityEngine.Vector3.right, forward, Mathf.InverseLerp(0, 0.5f, n1));
            UnityEngine.Vector3 u1 = (n2 >= 0.5f) ? UnityEngine.Vector3.Lerp(UnityEngine.Vector3.up, u0.normalized, Mathf.InverseLerp(1, 0.5f, n2)).normalized : UnityEngine.Vector3.Lerp(-UnityEngine.Vector3.up, u0.normalized, Mathf.InverseLerp(0, 0.5f, n2)).normalized;

            Vector3 un = u1;
            Vector3 fn = Vector3.Cross(Vector3.right.normalized, un.normalized).normalized;
            Vector3 rn = Vector3.Cross(un.normalized, fn.normalized).normalized;                       
                    
            Vector3 start = transform.position;
            float offset = (radius + minorRadius) * 1.0f;

            Vector3 A = start + Vector3.right * offset + Vector3.up * offset + Vector3.forward * offset;
            Vector3 B = start + Vector3.right * offset + Vector3.up * offset - Vector3.forward * offset;
            Vector3 C = start + Vector3.right * offset - Vector3.up * offset - Vector3.forward * offset;
            Vector3 D = start + Vector3.right * offset - Vector3.up * offset + Vector3.forward * offset;
            
            Vector3 E = start - Vector3.right * offset - Vector3.up * offset - Vector3.forward * offset;
            Vector3 F = start - Vector3.right * offset - Vector3.up * offset + Vector3.forward * offset;
            Vector3 G = start - Vector3.right * offset + Vector3.up * offset + Vector3.forward * offset;
            Vector3 H = start - Vector3.right * offset + Vector3.up * offset - Vector3.forward * offset;

            Gizmos.DrawSphere(A, 0.05f);
            Gizmos.DrawSphere(B, 0.05f);
            Gizmos.DrawSphere(C, 0.05f);
            Gizmos.DrawSphere(D, 0.05f);

            Gizmos.DrawSphere(E, 0.05f);
            Gizmos.DrawSphere(F, 0.05f);
            Gizmos.DrawSphere(G, 0.05f);
            Gizmos.DrawSphere(H, 0.05f);

            Handles.Label(A + UnityEngine.Vector3.up * 0.15f, "A");
            Handles.Label(B + UnityEngine.Vector3.up * 0.15f, "B");
            Handles.Label(C + UnityEngine.Vector3.up * 0.15f, "C");
            Handles.Label(D + UnityEngine.Vector3.up * 0.15f, "D");
            
            Handles.Label(E + UnityEngine.Vector3.up * 0.15f, "E");
            Handles.Label(F + UnityEngine.Vector3.up * 0.15f, "F");
            Handles.Label(G + UnityEngine.Vector3.up * 0.15f, "G");
            Handles.Label(H + UnityEngine.Vector3.up * 0.15f, "H");
                                                    
            Gizmos.DrawLine(start, start + un.normalized * (radius + minorRadius));                                        
            Gizmos.DrawLine(start, start + fn.normalized * (radius + minorRadius));                                        
            Gizmos.DrawLine(start, start + rn.normalized * (radius + minorRadius));                                                                            

            Handles.DrawWireDisc(start, un.normalized, radius);                                                                    
            Handles.DrawWireDisc(start, un.normalized, radius + minorRadius);                                                                    
            // Gizmos.DrawLine(start, start + Lh);                            
        }        
                              
    }
    #endif
}
