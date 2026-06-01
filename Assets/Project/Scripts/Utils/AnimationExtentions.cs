using System;
using System.Collections;
using UnityEngine;

public static class AnimatorExtensions
{
    /// <summary>
    /// Play một State trong Animator và gọi callback khi chạy xong.
    /// </summary>
    public static void PlayWithCallback(this Animator animator, string stateName, Action onComplete)
    {
        if (animator == null) return;

        // 1. Gọi Play như bình thường
        AnimationClip animationClip = GetAnimationClip(animator, stateName);

        animator.Play(stateName, 0);

        // 2. Chạy Coroutine kiểm tra tiến trình
        CoroutineManager.Instance.RunCoroutine(WaitForAnimatorComplete(animator, stateName, onComplete));
    }

    public static AnimationClip GetAnimationClip(this Animator animator, string clipName)
    {
        if (!animator.runtimeAnimatorController)
        {
            Debug.LogError(animator.name + " không có AnimatorController", animator);
            return null;
        }

        RuntimeAnimatorController runtimeAnimatorController = animator.runtimeAnimatorController;
        foreach (AnimationClip clip in runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
            {
                return clip;
            }
        }

        return null;
    }

    private static IEnumerator WaitForAnimatorComplete(Animator animator, string stateName, Action callback)
    {
        // BẮT BUỘC: Chờ 1 frame để Animator cập nhật sang State mới.
        // Nếu không có dòng này, GetCurrentAnimatorStateInfo sẽ lấy nhầm thông tin của State TRƯỚC ĐÓ.
        yield return null;

        if (animator == null) yield break;

        while (true)
        {
            yield return null; // Chờ frame tiếp theo (Zero Alloc)

            if (animator == null) yield break;

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            // TRƯỜNG HỢP 1: State hiện tại không còn trùng tên (Bị đè bởi anim khác hoặc chuyển trạng thái)
            if (!stateInfo.IsName(stateName))
            {
                yield break; // Tự hủy, không gọi callback
            }

            // TRƯỜNG HỢP 2: Đã chạy đến cuối (> 99%) và KHÔNG trong quá trình Transition sang state khác
            if (stateInfo.normalizedTime >= 0.99f && !animator.IsInTransition(0))
            {
                break; // Xong! Thoát vòng lặp để gọi callback
            }
        }

        callback?.Invoke();
    }
}