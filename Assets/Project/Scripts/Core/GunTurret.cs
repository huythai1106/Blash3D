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
        public Animator anim;
        private int currentAmmo;
        private int colorIndex = 0;

        public bool CanFire => currentAmmo > 0;

        public float speedRate;
        private float fireCooldown;
        private float fireTimer = 0f;
        private LevelCreator cachedLevelCreator;

        private bool isAutoFiring = false; // Mặc định tắt khi nằm ở hàng đợi dưới
        private float rotationSpeed = 360f; // Tốc độ xoay tháp súng (độ/giây)

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

        public TextMeshPro ammoText;

        public void Init(ShooterConfig shooterConfig)
        {
            this.config = shooterConfig;
            SetAmmoText(config.ammoCount);
            speedRate = GameManager.Instance.gunTurretConfig.speedRate;

            this.cachedLevelCreator = FindObjectOfType<LevelCreator>();

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
            if (currentTarget != null && GameManager.Instance.CurrentInputState == GameInputState.None)
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
            gunTurretState.SetState(TurretState.Shooting);
            SetAmmoText(currentAmmo);
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
            if (cell.isInteractable)
            {
                gunTurretState.SetState(TurretState.ReadyIdle);
            }
            else
            {
                gunTurretState.SetState(TurretState.NotReadyIdle);
            }
        }

        public override void OnSlotClicked()
        {
            switch (config.type)
            {
                case ShooterType.Normal:
                case ShooterType.Hidden:
                    if (config.linkGroupId != 0)
                    {
                        TryMoveToActiveFollowLinkGroupID(config.linkGroupId);
                    }
                    else
                    {
                        var result = TryMoveToActiveBar();
                        if (result)
                        {
                            EventDispatcher.PostEvent(Constant.OnTurretMoveToActiveBarEvent);
                        }
                    }
                    break;
                case ShooterType.Frozen:
                    // Logic bắn đóng băng (ví dụ: bắn ra đạn đóng băng làm chậm hoặc đóng băng tạm thời các voxel mục tiêu...)
                    break;
                default:
                    TryMoveToActiveBar();
                    break;
            }
        }

        public void TryMoveToActiveFollowLinkGroupID(int linkGroupId)
        {
            // B1: tìm tất cả các Cell khác trong cùng hàng có linkGroupId giống nhau
            // B2: kiểm tra xem tất cả các Cell ở hàng đầu chưa
            // B3: check xem ActiveBar có đủ chỗ cho tất cả các Cell này không
            // B4: nếu đủ chỗ thì tất cả các Cell này cùng bay lên ActiveBar (giả sử có 3 turrnet, và còn 3 slot trống lần lượt có vị trí là _+_+_ (_ là vị trí trống, + là vị trí có súng), thì sẽ di chuyển vị trí có súng về đầu để có thể có 3 slot liên tiếp), nếu không đủ thì không bay con nào cả
        }

        public bool TryMoveToActiveBar()
        {
            if (cell.isInteractable && ActiveBar.Instance.HasEmptySlot())
            {
                // 1. Tạm thời vô hiệu hóa tương tác để tránh bấm spam khi súng đang bay
                cell.isInteractable = false;

                // 2. Lấy vị trí đích từ ActiveBar (giả sử ActiveBar cung cấp điểm nhảy đến)
                Transform targetSlot = ActiveBar.Instance.GetNextAvailableSlot();

                // 3. HIỆU ỨNG SÚNG NHẢY LÊN (DOJump)
                transform.SetParent(null); // Tách súng ra khỏi Cell để nó bay tự do
                // gunTurretState.SetState(TurretState.FirstShot);
                transform.DOJump(targetSlot.position, 1.5f, 1, 0.3f)
                    .OnComplete(() =>
                    {
                        // Khi súng đã nhảy đến nơi:
                        ActiveBar.Instance.RegisterTurret(this, targetSlot);
                        this.ActivateAutoFire();
                    });

                // 4. Báo Board tịnh tiến các Cell phía sau lên (DOMove)
                board.OnCellDisplaced(cell, colIndex);

                // 5. Hủy vỏ Cell (cái bệ chứa súng)
                // Bạn có thể cho cái bệ nó scale nhỏ dần hoặc biến mất sau khi súng nhảy đi
                cell.transform.DOScale(0, 0.2f).OnComplete(() => SimplePool.Instance.Despawn(cell.gameObject));
                cell = null; // Ngắt tham chiếu đến Cell đã bị hủy

                return true;
            }
            else
            {
                gunTurretState.SetState(TurretState.NotReadyClick, () =>
                {
                    gunTurretState.SetState(TurretState.ReadyIdle);
                });
                return false;
            }
        }

        public override void OnBoardUpdate(int colParams)
        {
            if (cell == null) return; // Đã bay lên Active Slot rồi thì thôi, không cần update nữa

            if (colParams == colIndex)
            {
                if (gunTurretState.currentState == TurretState.ReadyIdle)
                {
                    gunTurretState.SetState(TurretState.FirstShot);
                }
                else if (!cell.isInteractable)
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