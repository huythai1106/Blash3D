using System.Collections;
using UnityEngine;

public class CoroutineManager : Singleton<CoroutineManager>
{
    // Đảm bảo CoroutineManager được khởi tạo ngay cả khi chưa ai gọi nó
    // (Hữu ích khi game vừa bật lên cần chạy ngầm cái gì đó ngay)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        var init = Instance;
    }

    /// <summary>
    /// Chạy một Coroutine từ bất kỳ đâu (ScriptableObject, C# Class...)
    /// </summary>
    public Coroutine RunCoroutine(IEnumerator routine)
    {
        if (routine == null) return null;
        return StartCoroutine(routine);
    }

    /// <summary>
    /// Dừng một Coroutine cụ thể
    /// </summary>
    public void HaltCoroutine(Coroutine routine)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
        }
    }

    /// <summary>
    /// Dừng toàn bộ các Coroutine đang chạy ngầm trong Manager này
    /// </summary>
    public void HaltAllCoroutines()
    {
        StopAllCoroutines();
    }
}