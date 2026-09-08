using UnityEngine;

/// <summary>
/// Tags a spawned instance with the prefab it was created from, so
/// ObjectPool.ReturnObject() knows which pool list/parent to return it to.
/// </summary>
public class Poolable : MonoBehaviour
{
    public GameObject SourcePrefab { get; set; }
}
