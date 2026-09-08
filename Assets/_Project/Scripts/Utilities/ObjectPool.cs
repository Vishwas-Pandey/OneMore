using UnityEngine;
using System.Collections.Generic;

public class ObjectPool : MonoBehaviour
{
    private readonly Dictionary<GameObject, List<GameObject>> pool = new Dictionary<GameObject, List<GameObject>>();
    private readonly Dictionary<GameObject, Transform> parentMap = new Dictionary<GameObject, Transform>();

    public void Initialize(GameObject[] prefabs, int initialCount = 5)
    {
        foreach (GameObject prefab in prefabs)
        {
            EnsurePoolExists(prefab);

            for (int i = 0; i < initialCount; i++)
            {
                CreateInstance(prefab);
            }
        }
    }

    private void EnsurePoolExists(GameObject prefab)
    {
        if (pool.ContainsKey(prefab)) return;

        pool[prefab] = new List<GameObject>();
        GameObject parent = new GameObject($"{prefab.name}_Pool");
        parent.transform.SetParent(transform);
        parentMap[prefab] = parent.transform;
    }

    private GameObject CreateInstance(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, parentMap[prefab]);
        obj.SetActive(false);

        Poolable poolable = obj.GetComponent<Poolable>();
        if (poolable == null) poolable = obj.AddComponent<Poolable>();
        poolable.SourcePrefab = prefab;

        Obstacle obstacle = obj.GetComponent<Obstacle>();
        if (obstacle != null) obstacle.SetOwnerPool(this);

        pool[prefab].Add(obj);
        return obj;
    }

    public GameObject GetObject(GameObject prefab)
    {
        EnsurePoolExists(prefab);

        foreach (GameObject obj in pool[prefab])
        {
            if (!obj.activeInHierarchy)
            {
                obj.SetActive(true);
                return obj;
            }
        }

        GameObject newObj = CreateInstance(prefab);
        newObj.SetActive(true);
        return newObj;
    }

    public void ReturnObject(GameObject obj)
    {
        Poolable poolable = obj.GetComponent<Poolable>();
        obj.SetActive(false);

        if (poolable != null && poolable.SourcePrefab != null && parentMap.TryGetValue(poolable.SourcePrefab, out Transform parent))
        {
            obj.transform.SetParent(parent);
        }

        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
    }

    public void ClearPool()
    {
        foreach (var list in pool.Values)
        {
            foreach (GameObject obj in list)
            {
                if (obj != null) Destroy(obj);
            }
            list.Clear();
        }
        pool.Clear();
        parentMap.Clear();
    }
}
