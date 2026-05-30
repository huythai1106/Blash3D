using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CubeLand.Gameplay
{
    public class GunTurret : SlotObject
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject bulletPrefab; // Đổi sang Bullet Pool trong thực tế
        [SerializeField] private Renderer barrelRenderer;

        public ShooterConfig config;
        public Animation anim;
        private int currentAmmo;
        private int colorIndex = 0;

        public bool CanFire => currentAmmo > 0;

        public float speedRate;
        private float fireCooldown;
        private float fireTimer = 0f;
        private LevelCreator cachedLevelCreator;

        private bool isAutoFiring = false; // Mặc định tắt khi nằm ở hàng đợi dưới
        private float rotationSpeed = 270f; // Tốc độ xoay tháp súng (độ/giây)

        // --- CÁC BIẾN MỚI THÊM ĐỂ TRACKING MỤC TIÊU ---
        private VoxelControl currentTarget;
        private Color currentTargetColor;

        // for Frozen Shooter
        private int remainingIceCounter = 0;
        public GunTurretState gunTurretState;

        public void ActivateAutoFire()
        {
            this.isAutoFiring = true;
        }
        private Cell attachedSlot; // Tham chiếu đến Cell đang gắn tháp súng này (nếu cần)

        public TextMeshPro ammoText;

        public void Init(ShooterConfig shooterConfig, Cell slot)
        {
            this.config = shooterConfig;
            SetAmmoText(config.ammoCount);
            speedRate = GameManager.Instance.gunTurretConfig.speedRate;

            this.cachedLevelCreator = FindObjectOfType<LevelCreator>();
            this.attachedSlot = slot;

            this.fireCooldown = 1f / Mathf.Max(0.1f, speedRate);
            this.fireTimer = fireCooldown;

            string hex = config.colorsHex[0];
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                barrelRenderer.SetColorOptimized(color);
            }

            gunTurretState = new GunTurretState(anim);
            UpdateTurretVisual();
        }

        private void Update()
        {
            if (GameManager.Instance.CurrentState != GameState.Playing) return;
            if (!isAutoFiring) return; // CHỈ BẮN KHI ĐÃ NHẢY LÊN ACTIVE SLOT
            if (!CanFire) return;
            if (currentAmmo <= 0) return;

            // Thời gian hồi chiêu vẫn đếm kể cả khi đang xoay nòng
            fireTimer += Time.deltaTime;

            // 1. TÌM MỤC TIÊU: Nếu chưa có mục tiêu, xin quản lý map 1 cái
            if (currentTarget == null)
            {
                string currentHex = config.colorsHex[colorIndex % config.colorsHex.Count];
                ColorUtility.TryParseHtmlString(currentHex, out currentTargetColor);

                currentTarget = cachedLevelCreator.FindAvailableTarget(currentTargetColor);
            }

            // 2. NGẮM & BẮN: Nếu đã khóa được mục tiêu
            if (currentTarget != null)
            {
                // Optional: Nếu Voxel bị ụ súng khác bắn nổ trước khi mình kịp bắn, cần reset
                if (!currentTarget.gameObject.activeInHierarchy)
                {
                    currentTarget = null;
                    return;
                }

                Vector3 targetPos = currentTarget.transform.position;
                Vector3 directionToTarget = targetPos - transform.position;
                directionToTarget.y = 0; // Khóa trục Y

                // Xoay nòng súng mượt mà MỖI FRAME
                RotateTurret(directionToTarget);

                // Tính toán xem nòng súng đã chỉ thẳng vào mục tiêu chưa
                if (directionToTarget.sqrMagnitude > 0.001f)
                {
                    float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

                    // 3. KHAI HỎA: Đã ngắm chuẩn (sai số < 5 độ) VÀ đã nạp đạn xong
                    if (angleToTarget <= 5f && fireTimer >= fireCooldown)
                    {
                        ExecuteShoot(currentTarget, currentTargetColor);

                        fireTimer = 0f; // Reset cooldown
                        currentTarget = null; // Xóa mục tiêu hiện tại để tìm mục tiêu mới cho viên đạn sau
                    }
                }
            }
            else
            {
                // khi không có mục tiêu, xoay về hướng mặc định (có thể là hướng thẳng đứng hoặc hướng đã định sẵn)
                RotateTurret(Vector3.forward); // Ví dụ: hướng mặc định là về phía trước
                gunTurretState.SetState(TurretState.NoTarget);
            }
        }

        /// <summary>
        /// Thực thi logic bắn (đã được gọi sau khi ngắm chuẩn)
        /// </summary>
        private void ExecuteShoot(VoxelControl target, Color bulletColor)
        {
            SpawnBullet(target, bulletColor);

            currentAmmo--;
            SetAmmoText(currentAmmo);
            gunTurretState.SetState(TurretState.Shooting);
            gunTurretState.UpdateAnimation();
            UpdateTurretVisual();
        }

        private void RotateTurret(Vector3 directionToTarget)
        {
            if (directionToTarget.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        private void SpawnBullet(VoxelControl target, Color color)
        {
            GameObject bGo = SimplePool.Instance.Spawn(bulletPrefab, firePoint.position, firePoint.rotation);
            Bullet bullet = bGo.GetComponent<Bullet>();

            bullet.Launch(target, color);
        }

        private void UpdateTurretVisual()
        {
            if (currentAmmo <= 0)
            {
                colorIndex++;
                if (colorIndex >= config.colorsHex.Count)
                {
                    gunTurretState.SetState(TurretState.EndShot);
                    DOVirtual.DelayedCall(0.5f, () =>
                    {
                        gameObject.SetActive(false); // Ẩn tháp súng khi đã hết đạn
                        ActiveBar.Instance.ReleaseSlot(this); // Trả lại ô trống cho Active Slot
                    });
                    return;
                }
                else
                {
                    SetAmmoText(config.ammoCount);
                    string nextHex = config.colorsHex[colorIndex % config.colorsHex.Count];
                    if (ColorUtility.TryParseHtmlString(nextHex, out Color nextColor))
                    {
                        // Đảm bảo bạn đã định nghĩa biến ColorPropId ở đâu đó (như RendererExtensions)
                        barrelRenderer.SetColorOptimized(nextColor, RendererExtensions.ColorPropId);
                    }
                }
            }
        }

        private void SetAmmoText(int ammo)
        {
            currentAmmo = ammo;
            if (ammoText != null)
            {
                ammoText.text = ammo.ToString();
            }
        }

        public override void OnBoardInit()
        {
            if (attachedSlot.isInteractable)
            {
                gunTurretState.SetState(TurretState.ReadyIdle);
            }
            else
            {
                gunTurretState.SetState(TurretState.NotReadyIdle);
            }
        }

        public override void OnBoardUpdate(int colParams)
        {
            if (colParams == colIndex)
            {
                if (gunTurretState.currentState == TurretState.ReadyIdle)
                {
                    gunTurretState.SetState(TurretState.FirstShot);
                }
                else if (!attachedSlot.isInteractable)
                {
                    gunTurretState.SetState(TurretState.NotReadyToNotReady, () =>
                    {
                        gunTurretState.SetState(TurretState.NotReadyIdle);
                    });
                }
                else
                {
                    gunTurretState.SetState(TurretState.NotReadyToReady, () =>
                    {
                        gunTurretState.SetState(TurretState.ReadyIdle);
                    });
                }
            }
        }
    }
}