using System;
using System.Collections;
using UnityEngine;

public static class AnimationExtensions
{
    /// <summary>
    /// Play animation (Legacy) và gọi callback khi kết thúc thực tế.
    /// Tự động hủy nếu bị đè bởi animation khác hoặc bị dừng đột ngột.
    /// </summary>
    public static void Play(this Animation animation, string clipName, Action callback)
    {
        if (animation == null)
        {
            Debug.LogError("Animation component bị null!");
            return;
        }

        AnimationState state = animation[clipName];
        if (state == null)
        {
            Debug.LogError($"Animation State '{clipName}' không tồn tại trên component!");
            return;
        }

        // Thực hiện chơi animation
        animation.Play(clipName);

        // Chạy Coroutine kiểm tra runtime state theo frame thay vì chờ thời gian cứng
        CoroutineManager.Instance.RunCoroutine(WaitForLegacyAnimation(animation, state, callback));
    }

    private static IEnumerator WaitForLegacyAnimation(Animation animation, AnimationState state, Action callback)
    {
        while (true)
        {
            yield return null; // Chờ frame tiếp theo (Zero Allocation)

            if (animation == null) yield break;

            if (!state.enabled || !animation.IsPlaying(state.name))
            {
                yield break;
            }

            // TRƯỜNG HỢP 3: Animation chạy hết thời gian thực tế của nó (Đã xong)
            if (state.normalizedTime >= 0.99f && state.wrapMode != WrapMode.Loop)
            {
                break; // Thoát vòng lặp để gọi callback
            }
        }

        callback?.Invoke();
    }
}