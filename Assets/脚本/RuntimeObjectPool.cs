using System.Collections.Generic;
using UnityEngine;

public interface IPooledObject
{
    void OnSpawnedFromPool();
    void OnReturnedToPool();
}

public class RuntimePooledInstance : MonoBehaviour
{
    public GameObject SourcePrefab { get; set; }
    public int SourcePrefabId { get; set; }
    public bool IsInPool { get; set; }
}

public static class RuntimeObjectPool
{
    private static readonly Dictionary<int, Stack<GameObject>> pools = new Dictionary<int, Stack<GameObject>>();
    private static readonly Dictionary<int, Transform> poolParents = new Dictionary<int, Transform>();
    private static Transform root;

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
            return null;

        int prefabId = prefab.GetInstanceID();
        Stack<GameObject> pool = GetPool(prefabId);
        GameObject instance = null;

        while (pool.Count > 0 && instance == null)
        {
            instance = pool.Pop();
        }

        RuntimePooledInstance marker;
        if (instance == null)
        {
            instance = Object.Instantiate(prefab);
            marker = instance.GetComponent<RuntimePooledInstance>();
            if (marker == null)
            {
                marker = instance.AddComponent<RuntimePooledInstance>();
            }

            marker.SourcePrefab = prefab;
            marker.SourcePrefabId = prefabId;
        }
        else
        {
            marker = instance.GetComponent<RuntimePooledInstance>();
            if (marker == null)
            {
                marker = instance.AddComponent<RuntimePooledInstance>();
                marker.SourcePrefab = prefab;
                marker.SourcePrefabId = prefabId;
            }
        }

        marker.IsInPool = false;
        instance.transform.SetParent(null, false);
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);
        NotifySpawned(instance);
        return instance;
    }

    public static void Release(GameObject instance)
    {
        if (instance == null)
            return;

        RuntimePooledInstance marker = instance.GetComponent<RuntimePooledInstance>();
        if (marker == null || marker.SourcePrefabId == 0)
        {
            Object.Destroy(instance);
            return;
        }

        if (marker.IsInPool)
            return;

        NotifyReturned(instance);
        marker.IsInPool = true;
        instance.SetActive(false);
        instance.transform.SetParent(GetPoolParent(marker.SourcePrefab, marker.SourcePrefabId), false);
        GetPool(marker.SourcePrefabId).Push(instance);
    }

    public static void ClearAll()
    {
        foreach (Stack<GameObject> pool in pools.Values)
        {
            while (pool.Count > 0)
            {
                GameObject instance = pool.Pop();
                if (instance != null)
                {
                    Object.Destroy(instance);
                }
            }
        }

        pools.Clear();
        poolParents.Clear();

        if (root != null)
        {
            Object.Destroy(root.gameObject);
            root = null;
        }
    }

    private static Stack<GameObject> GetPool(int prefabId)
    {
        if (!pools.TryGetValue(prefabId, out Stack<GameObject> pool))
        {
            pool = new Stack<GameObject>();
            pools[prefabId] = pool;
        }

        return pool;
    }

    private static Transform GetPoolParent(GameObject prefab, int prefabId)
    {
        if (poolParents.TryGetValue(prefabId, out Transform parent) && parent != null)
            return parent;

        EnsureRoot();
        GameObject parentObject = new GameObject($"Pool_{(prefab != null ? prefab.name : prefabId.ToString())}");
        parentObject.transform.SetParent(root, false);
        poolParents[prefabId] = parentObject.transform;
        return parentObject.transform;
    }

    private static void EnsureRoot()
    {
        if (root != null)
            return;

        GameObject rootObject = new GameObject("Runtime Object Pool");
        Object.DontDestroyOnLoad(rootObject);
        root = rootObject.transform;
    }

    private static void NotifySpawned(GameObject instance)
    {
        IPooledObject[] pooledObjects = instance.GetComponentsInChildren<IPooledObject>(true);
        foreach (IPooledObject pooledObject in pooledObjects)
        {
            if (pooledObject is Behaviour behaviour && !behaviour.enabled)
                continue;

            pooledObject.OnSpawnedFromPool();
        }
    }

    private static void NotifyReturned(GameObject instance)
    {
        IPooledObject[] pooledObjects = instance.GetComponentsInChildren<IPooledObject>(true);
        foreach (IPooledObject pooledObject in pooledObjects)
        {
            if (pooledObject is Behaviour behaviour && !behaviour.enabled)
                continue;

            pooledObject.OnReturnedToPool();
        }
    }
}
