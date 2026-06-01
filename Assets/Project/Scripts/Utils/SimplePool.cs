using System;
using System.Collections.Generic;
using UnityEngine;

public class SimplePool : MonoBehaviour
{
    public static SimplePool Instance { get; private set; }

    // --- BỔ SUNG: Cache lưu trữ Prefab đã nạp từ Resources ---
    private Dictionary<string, GameObject> resourceCache = new Dictionary<string, GameObject>();

    // --- CODE CŨ: Lưu trữ hàng đợi các object rảnh rỗi ---
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    #region Overloads cho Resources

    /// <summary>
    /// Nạp Prefab từ Resources (chỉ load 1 lần) và chuyển vào luồng Spawn gốc
    /// </summary>
    public GameObject Spawn(string resourcePath, Vector3 position, Quaternion rotation)
    {
        // 1. Kiểm tra cache xem đã nạp prefab này vào RAM chưa
        if (!resourceCache.TryGetValue(resourcePath, out GameObject prefab))
        {
            prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogError($"[SimplePool] Không tìm thấy Prefab tại Resources/{resourcePath}");
                return null;
            }
            // 2. Lưu vào cache để các lần sau không phải đọc I/O ổ cứng nữa
            resourceCache[resourcePath] = prefab;
        }

        // 3. Đẩy Prefab gốc vào hàm Spawn nguyên bản
        return Spawn(prefab, position, rotation);
    }

    /// <summary>
    /// Helper lấy nhanh Component khi nạp từ Resources
    /// </summary>
    public T Spawn<T>(string resourcePath, Vector3 position, Quaternion rotation) where T : Component
    {
        GameObject obj = Spawn(resourcePath, position, rotation);
        return obj != null ? obj.GetComponent<T>() : null;
    }

    #endregion

    #region Lõi Code Nguyên Bản (Giữ nguyên)

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
            obj.transform.SetPositionAndRotation(position, rotation);
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

    #endregion
}

// Component nhỏ gắn kèm object để lưu vết Prefab gốc
public class PoolMember : MonoBehaviour
{
    [HideInInspector] public GameObject prefabSource;
}