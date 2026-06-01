using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CubeLand.Gameplay
{
    public enum TurretState
    {
        Shooting, EndShot, WaitingShot, FirstShot, ReadyIdle,
        NotReadyIdle, NotReadyToReady, NotReadyToNotReady, NotReadyClick, NoTarget
    }

    public class GunTurretState
    {
        private static readonly int ShootingHash = Animator.StringToHash("Shooting");
        public Animator anim;
        public TurretState currentState;

        public GunTurretState(Animator anim)
        {
            this.anim = anim;
        }

        public void UpdateAnimation(Action callback = null)
        {
            switch (currentState)
            {
                case TurretState.Shooting:
                    anim.PlayWithCallback("Shooting", null);
                    break;
                case TurretState.EndShot:
                    anim.PlayWithCallback("EndShot", callback);
                    break;
                case TurretState.WaitingShot:
                    anim.PlayWithCallback("WaitingShot", callback);
                    break;
                case TurretState.FirstShot:
                    anim.PlayWithCallback("FirstShot", callback);
                    break;
                case TurretState.ReadyIdle:
                    anim.PlayWithCallback("ReadyIdle", callback);
                    break;
                case TurretState.NotReadyIdle:
                    anim.PlayWithCallback("NotReadyIdle", callback);
                    break;
                case TurretState.NotReadyToReady:
                    anim.PlayWithCallback("NotReadyToReady", callback);
                    break;
                case TurretState.NotReadyToNotReady:
                    anim.PlayWithCallback("NotReadyToNotReady", callback);
                    break;
                case TurretState.NotReadyClick:
                    anim.PlayWithCallback("NotReadyClick", callback);
                    break;
                case TurretState.NoTarget:
                    anim.PlayWithCallback("NoTaget", callback);
                    break;
                default:
                    Debug.LogWarning($"Unknown turret state: {currentState}");
                    break;
            }
        }

        public void SetState(TurretState newState, Action onCompleteAnim = null, Action onStageChanged = null)
        {
            if (currentState == newState) return; // Tránh gọi lại animation nếu trạng thái không đổi

            currentState = newState;
            onStageChanged?.Invoke();
            UpdateAnimation(onCompleteAnim);
        }
    }
}
