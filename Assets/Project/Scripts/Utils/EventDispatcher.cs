using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static EventDispatcher - Hệ thống quản lý sự kiện toàn cục tối ưu (Zero GC Alloc).
/// Sử dụng Generic Delegate để loại bỏ hoàn toàn Boxing/Unboxing.
/// </summary>
public static class EventDispatcher
{
    // Dùng chung một bảng băm duy nhất lưu trữ mọi loại Delegate
    private static readonly Dictionary<string, Delegate> _eventTable = new Dictionary<string, Delegate>();

    #region Add Listener

    /// <summary>
    /// Đăng ký sự kiện (KHÔNG CÓ tham số)
    /// </summary>
    public static void AddListener(string eventName, Action listener)
    {
        OnListenerAdding(eventName, listener);
    }

    /// <summary>
    /// Đăng ký sự kiện (CÓ 1 tham số)
    /// </summary>
    public static void AddListener<T>(string eventName, Action<T> listener)
    {
        OnListenerAdding(eventName, listener);
    }

    /// <summary>
    /// Đăng ký sự kiện (CÓ 2 tham số)
    /// </summary>
    public static void AddListener<T1, T2>(string eventName, Action<T1, T2> listener)
    {
        OnListenerAdding(eventName, listener);
    }

    private static void OnListenerAdding(string eventName, Delegate listener)
    {
        if (!_eventTable.TryGetValue(eventName, out var existingDelegate))
        {
            _eventTable[eventName] = listener;
        }
        else
        {
            // Kết hợp delegate mới vào danh sách hiện tại
            _eventTable[eventName] = Delegate.Combine(existingDelegate, listener);
        }
    }

    #endregion

    #region Remove Listener

    public static void RemoveListener(string eventName, Action listener)
    {
        OnListenerRemoving(eventName, listener);
    }

    public static void RemoveListener<T>(string eventName, Action<T> listener)
    {
        OnListenerRemoving(eventName, listener);
    }

    public static void RemoveListener<T1, T2>(string eventName, Action<T1, T2> listener)
    {
        OnListenerRemoving(eventName, listener);
    }

    private static void OnListenerRemoving(string eventName, Delegate listener)
    {
        if (_eventTable.TryGetValue(eventName, out var existingDelegate))
        {
            var newDelegate = Delegate.Remove(existingDelegate, listener);
            if (newDelegate == null)
            {
                _eventTable.Remove(eventName);
            }
            else
            {
                _eventTable[eventName] = newDelegate;
            }
        }
    }

    #endregion

    #region Post Event

    /// <summary>
    /// Phát sự kiện (KHÔNG CÓ tham số)
    /// </summary>
    public static void PostEvent(string eventName)
    {
        if (_eventTable.TryGetValue(eventName, out var d))
        {
            if (d is Action action)
            {
                action.Invoke();
            }
            else
            {
                Debug.LogError($"[EventDispatcher] Lỗi kiểu dữ liệu! Sự kiện '{eventName}' yêu cầu tham số nhưng lại được gọi không có tham số.");
            }
        }
    }

    /// <summary>
    /// Phát sự kiện (CÓ 1 tham số)
    /// </summary>
    public static void PostEvent<T>(string eventName, T arg)
    {
        if (_eventTable.TryGetValue(eventName, out var d))
        {
            if (d is Action<T> action)
            {
                action.Invoke(arg);
            }
            else
            {
                Debug.LogError($"[EventDispatcher] Lỗi kiểu dữ liệu! Sự kiện '{eventName}' được đăng ký với kiểu khác so với lúc phát ({typeof(T)}).");
            }
        }
    }

    /// <summary>
    /// Phát sự kiện (CÓ 2 tham số)
    /// </summary>
    public static void PostEvent<T1, T2>(string eventName, T1 arg1, T2 arg2)
    {
        if (_eventTable.TryGetValue(eventName, out var d))
        {
            if (d is Action<T1, T2> action)
            {
                action.Invoke(arg1, arg2);
            }
        }
    }

    #endregion

    #region Utilities

    public static void ClearAllEvents()
    {
        _eventTable.Clear();
        Debug.Log("[EventDispatcher] Đã xóa toàn bộ sự kiện!");
    }

    public static void ClearEvent(string eventName)
    {
        if (_eventTable.Remove(eventName))
        {
            Debug.Log($"[EventDispatcher] Đã xóa sự kiện: {eventName}");
        }
    }

    #endregion
}