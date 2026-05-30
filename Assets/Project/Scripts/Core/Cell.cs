using DG.Tweening;
using UnityEngine;

namespace CubeLand.Gameplay
{
    public class Cell : MonoBehaviour
    {
        public SlotType slotType; // Shooter, Key, Lock...
        private int colIndex;
        public bool isInteractable;
        private GunTurret attachedTurret; // Nếu slotType là Shooter thì giữ tham chiếu này
        private Board board;

        private void Start()
        {
            EventDispatcher.AddListener(Constant.OnTurretMoveToActiveBarEvent, OnTurretMove);
        }

        private void OnDestroy()
        {
            EventDispatcher.RemoveListener(Constant.OnTurretMoveToActiveBarEvent, OnTurretMove);
        }

        public void Setup(GridSlotNode node, int col, Board board)
        {
            slotType = node.slotType;
            colIndex = col;
            this.board = board;

            if (slotType == SlotType.Shooter)
            {
                // Sinh ụ súng đặt lên trên Cell này
                attachedTurret = SpawnTurret(node.shooter);
                attachedTurret.colIndex = colIndex;
            }
        }

        public void SetInteractable(bool state)
        {
            this.isInteractable = state;
            // Thay đổi Visual hoặc bật/tắt Collider của nút bấm tùy bạn cấu hình
        }

        private void OnMouseDown()
        {
            OnCellClicked();
        }

        public void OnCellClicked()
        {
            if (!isInteractable) return; // Không phải hàng đầu thì bấm vô dụng

            if (slotType == SlotType.Shooter)
            {
                // Kiểm tra xem hàng ActiveBar trên cùng còn chỗ trống không
                ShooterConfig config = attachedTurret.config;
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
            else if (slotType == SlotType.Key)
            {
                // Logic xử lý Chìa khóa (Ví dụ: bay vào túi trữ chìa khóa, mở ô Lock tương ứng...)
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
            if (isInteractable && attachedTurret != null && ActiveBar.Instance.HasEmptySlot())
            {
                // 1. Tạm thời vô hiệu hóa tương tác để tránh bấm spam khi súng đang bay
                isInteractable = false;

                // 2. Lấy vị trí đích từ ActiveBar (giả sử ActiveBar cung cấp điểm nhảy đến)
                Transform targetSlot = ActiveBar.Instance.GetNextAvailableSlot();

                // 3. HIỆU ỨNG SÚNG NHẢY LÊN (DOJump)
                attachedTurret.transform.SetParent(null); // Tách súng ra khỏi Cell để nó bay tự do
                // attachedTurret.gunTurretState.SetState(TurretState.FirstShot);
                attachedTurret.transform.DOJump(targetSlot.position, 5f, 1, 0.5f)
                    .OnComplete(() =>
                    {
                        // Khi súng đã nhảy đến nơi:
                        ActiveBar.Instance.RegisterTurret(attachedTurret, targetSlot);
                        attachedTurret.ActivateAutoFire();
                        attachedTurret = null;
                    });

                // 4. Báo Board tịnh tiến các Cell phía sau lên (DOMove)
                board.OnCellDisplaced(this, colIndex);

                // 5. Hủy vỏ Cell (cái bệ chứa súng)
                // Bạn có thể cho cái bệ nó scale nhỏ dần hoặc biến mất sau khi súng nhảy đi
                transform.DOScale(0, 0.2f).OnComplete(() => SimplePool.Instance.Despawn(this.gameObject));

                return true;
            }
            else
            {
                attachedTurret.gunTurretState.SetState(TurretState.NotReadyClick, () =>
                {
                    attachedTurret.gunTurretState.SetState(TurretState.ReadyIdle);
                });
                return false;
            }
        }

        public void MoveToPosition(Vector3 newPos)
        {
            // Dùng DOTween di chuyển mượt mà lên vị trí mới khi hàng trước bị trống
            transform.DOMove(newPos, 0.4f).SetEase(Ease.InOutQuad);
            // transform.position = newPos; // Tạm thời nhảy cóc, sau này đổi thành di chuyển mượt
        }


        [Header("Prefab Ụ Súng")]
        [SerializeField] private GameObject turretPrefab; // Prefab thực thể ụ súng chiến đấu
        [SerializeField] private Transform turretAnchor;   // Điểm neo (vị trí đặt súng nằm trên đỉnh của ô)

        public GunTurret SpawnTurret(ShooterConfig shooterConfig)
        {
            if (shooterConfig == null) return null;

            // 1. Sinh ụ súng ngay tại điểm neo của ô
            GameObject turretGo = SimplePool.Instance.Spawn(turretPrefab, turretAnchor.position, turretAnchor.rotation);
            turretGo.transform.SetParent(turretAnchor); // Gắn chặt súng vào bệ Cell dưới hàng đợi

            // 2. Lấy Component súng và truyền toàn bộ dữ liệu (Số đạn, Danh sách dải màu...)
            GunTurret turretScript = turretGo.GetComponent<GunTurret>();

            // Khởi tạo súng (Lúc này súng nhận data nhưng isAutoFiring vẫn là false, nằm im chờ đợi)
            turretScript.Init(shooterConfig, this);

            return turretScript;
        }

        private void OnTurretMove()
        {
            // Event được gọi khi có 1 súng nào đó trong hàng này được bấm và đang bay lên ActiveBar, tất cả các Cell phía sau nó sẽ nhận được sự kiện này
            if (slotType == SlotType.Key)
            {
                // tìm nếu ở hàng đầu có ổ khóa thì sẽ bay đến ổ khóa đó, cột có ổ khóa và khóa sẽ biến mất, mở đường cho các Cell phía sau tiến lên
            }
        }
    }
}