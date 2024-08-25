using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Vector3Extension
{
    public static float InverseLerp(UnityEngine.Vector3 a, UnityEngine.Vector3 b, UnityEngine.Vector3 value)
    {
        Vector3 AB = b - a;
        Vector3 AV = value - a;
        return Vector3.Dot(AV, AB) / Vector3.Dot(AB, AB);
    }
}
