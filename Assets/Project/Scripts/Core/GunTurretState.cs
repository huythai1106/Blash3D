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

        public Animation anim;
        public TurretState currentState;

        public GunTurretState(Animation anim)
        {
            this.anim = anim;
        }

        public void UpdateAnimation(Action callback = null)
        {
            switch (currentState)
            {
                case TurretState.Shooting:
                    anim.Play("Shooting", callback);
                    break;
                case TurretState.EndShot:
                    anim.Play("EndShot", callback);
                    break;
                case TurretState.WaitingShot:
                    anim.Play("WaitingShot", callback);
                    break;
                case TurretState.FirstShot:
                    anim.Play("FirstShot", callback);
                    break;
                case TurretState.ReadyIdle:
                    anim.Play("ReadyIdle", callback);
                    break;
                case TurretState.NotReadyIdle:
                    anim.Play("NotReadyIdle", callback);
                    break;
                case TurretState.NotReadyToReady:
                    anim.Play("NotReadyToReady", callback);
                    break;
                case TurretState.NotReadyToNotReady:
                    anim.Play("NotReadyToNotReady", callback);
                    break;
                case TurretState.NotReadyClick:
                    anim.Play("NotReadyClick", callback);
                    break;
                case TurretState.NoTarget:
                    anim.Play("NoTaget", callback);
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
