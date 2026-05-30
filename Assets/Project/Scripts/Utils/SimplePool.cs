using System;
using System.Collections.Generic;
using UnityEngine;


public class SimplePool : MonoBehaviour
{
    public static SimplePool Instance { get; private set; }

    // Lưu trữ hàng đợi các object rảnh rỗi, phân loại theo Prefab gốc
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        // Nếu chưa có hàng đợi cho Prefab này, tạo mới
        if (!poolDictionary.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            poolDictionary[prefab] = pool;
        }

        GameObject obj;
        // Nếu trong kho có sẵn đồ cũ -> Tái sử dụng
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
            obj.transform.SetPositionAndRotation(position, rotation); // Gọi 1 hàm gộp để tối ưu API của Unity
            obj.SetActive(true);
        }
        else
        {
            // Hết hàng thì mới Instantiate và gắn chip theo dõi (PoolMember)
            obj = Instantiate(prefab, position, rotation);
            PoolMember member = obj.AddComponent<PoolMember>();
            member.prefabSource = prefab;
        }

        return obj;
    }

    public void Despawn(GameObject obj, Action onDespawned = null)
    {
        if (!obj.activeSelf) return;

        // Đọc chip theo dõi để biết phải trả về hàng đợi nào
        PoolMember member = obj.GetComponent<PoolMember>();
        if (member != null && poolDictionary.TryGetValue(member.prefabSource, out Queue<GameObject> pool))
        {
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
        else
        {
            // Trượt mất chip (hiếm khi xảy ra nếu code chuẩn) -> Hủy luôn
            Destroy(obj);
        }
        onDespawned?.Invoke();
    }
}

// Component nhỏ gắn kèm object để lưu vết Prefab gốc
public class PoolMember : MonoBehaviour
{
    [HideInInspector] public GameObject prefabSource;
}
