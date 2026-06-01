using CubeLand.Core;
using DG.Tweening;
using UnityEngine;

namespace CubeLand.Gameplay
{
    public class Cell : MonoBehaviour
    {
        public SlotType slotType; // Shooter, Key, Lock...
        public SlotObject slotObject;
        private int colIndex;
        public bool isInteractable;
        private GunTurret attachedTurret; // Nếu slotType là Shooter thì giữ tham chiếu này
        private Board board;

        [Header("Prefab Ụ Súng")]
        [SerializeField] private GameObject turretPrefab; // Prefab thực thể ụ súng chiến đấu
        [SerializeField] private Transform turretAnchor;   // Điểm neo (vị trí đặt súng nằm trên đỉnh của ô)

        public void Setup(GridSlotNode node, int col, Board board)
        {
            slotType = node.slotType;
            colIndex = col;
            this.board = board;

            slotObject = SimplePool.Instance.Spawn<SlotObject>(slotType.ToString(), turretAnchor.position, turretAnchor.rotation);
            slotObject.Setup(this, board, slotType, colIndex);
            slotObject.transform.SetParent(turretAnchor);

            // Nếu là Shooter thì sinh ụ súng đặt lên trên Cell này
            if (slotType == SlotType.Shooter)
            {
                // Sinh ụ súng đặt lên trên Cell này
                attachedTurret = slotObject as GunTurret;
                attachedTurret.Init(node.shooter);
            }
        }

        public void SetInteractable(bool state)
        {
            // Thay đổi Visual hoặc bật/tắt Collider của nút bấm tùy bạn cấu hình
            this.isInteractable = state;
            if (state)
            {
                slotObject.OnReachTop();
            }
        }

        public void OnCellClicked()
        {
            if (!isInteractable) return; // Không phải hàng đầu thì bấm vô dụng

            slotObject.OnSlotClicked();
        }

        public void MoveToPosition(Vector3 newPos)
        {
            transform.DOMove(newPos, 0.4f).SetEase(Ease.InOutQuad);
        }

        private void OnMouseDown()
        {
            OnCellClicked();
        }
    }
}