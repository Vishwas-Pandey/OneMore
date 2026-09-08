using UnityEngine;

public static class MathExtensions
{
    public static float Remap(this float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    public static Vector3 WithY(this Vector3 vector, float y) => new Vector3(vector.x, y, vector.z);
    public static Vector3 WithX(this Vector3 vector, float x) => new Vector3(x, vector.y, vector.z);
}
