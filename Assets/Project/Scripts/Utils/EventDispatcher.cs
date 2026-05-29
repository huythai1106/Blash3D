using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static EventDispatcher - Hệ thống quản lý sự kiện toàn cục không cần Mono.
/// Hỗ trợ đăng ký, hủy, kích hoạt sự kiện bằng string với cơ chế an toàn.
/// </summary>
public static class EventDispatcher
{
    private static readonly Dictionary<string, Action<object[]>> _eventTable = new Dictionary<string, Action<object[]>>();

    #region Basic Methods

    /// <summary>
    /// Đăng ký lắng nghe sự kiện
    /// </summary>
    public static void AddListener(string eventName, Action<object[]> listener)
    {
        if (!_eventTable.ContainsKey(eventName))
            _eventTable[eventName] = null;

        _eventTable[eventName] += listener;
    }

    /// <summary>
    /// Hủy đăng ký sự kiện (Luôn gọi khi đối tượng bị hủy)
    /// </summary>
    public static void RemoveListener(string eventName, Action<object[]> listener)
    {
        if (_eventTable.ContainsKey(eventName))
        {
            _eventTable[eventName] -= listener;

            if (_eventTable[eventName] == null)
            {
                _eventTable.Remove(eventName);
            }
        }
    }

    /// <summary>
    /// Kích hoạt sự kiện
    /// </summary>
    /// <param name="debug">Nếu true, sẽ in log khi sự kiện được bắn</param>
    public static void PostEvent(string eventName, bool debug = false, params object[] parameters)
    {
        if (debug) Debug.Log($"[EventDispatcher] Post: {eventName}");

        if (_eventTable.TryGetValue(eventName, out var action) && action != null)
        {
            action(parameters);
        }
        else if (debug)
        {
            Debug.LogWarning($"[EventDispatcher] Không có listener nào cho sự kiện: {eventName}");
        }
    }

    #endregion

    #region Advanced Methods

    /// <summary>
    /// Đăng ký lắng nghe sự kiện chỉ 1 lần duy nhất, sau đó tự hủy
    /// </summary>
    public static void AddOnceListener(string eventName, Action<object[]> listener)
    {
        Action<object[]> wrapper = null;
        wrapper = (parameters) =>
        {
            RemoveListener(eventName, wrapper);
            listener(parameters);
        };
        AddListener(eventName, wrapper);
    }

    /// <summary>
    /// Gọi sự kiện an toàn (bọc trong try-catch để tránh crash khi listener lỗi)
    /// </summary>
    public static void SafePostEvent(string eventName, params object[] parameters)
    {
        try
        {
            if (_eventTable.TryGetValue(eventName, out var action) && action != null)
            {
                action(parameters);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[EventDispatcher] Lỗi khi xử lý sự kiện {eventName}: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// Kiểm tra xem sự kiện đã có ai đăng ký chưa
    /// </summary>
    public static bool HasListener(string eventName)
    {
        return _eventTable.ContainsKey(eventName) && _eventTable[eventName] != null;
    }

    /// <summary>
    /// Lấy số lượng listener đang đăng ký cho sự kiện này
    /// </summary>
    public static int GetListenerCount(string eventName)
    {
        if (_eventTable.TryGetValue(eventName, out var action) && action != null)
        {
            return action.GetInvocationList().Length;
        }
        return 0;
    }

    /// <summary>
    /// Xóa toàn bộ danh sách sự kiện (Nên gọi khi chuyển Scene hoặc reset game)
    /// </summary>
    public static void ClearAllEvents()
    {
        _eventTable.Clear();
        Debug.Log("[EventDispatcher] Đã xóa toàn bộ sự kiện!");
    }

    #endregion
}