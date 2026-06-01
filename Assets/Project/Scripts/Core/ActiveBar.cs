using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace CubeLand.Gameplay
{
    public class ActiveBar : MonoBehaviour
    {
        public static ActiveBar Instance { get; private set; }

        [SerializeField] private List<Transform> activeSlotPoints; // Kéo thả 5 Transform vị trí của 5 ô trên Inspector
        private GunTurret[] occupiedSlots = new GunTurret[5]; // Mảng cố định 5 phần tử để quản lý runtime
        private bool[] reservedSlots = new bool[5]; // Mảng để đánh dấu ô đã được đặt chỗ nhưng chưa đến nơi (đang bay)

        private void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Kiểm tra xem còn ô chiến đấu nào trống không
        /// </summary>
        public bool HasEmptySlot()
        {
            for (int i = 0; i < occupiedSlots.Length; i++)
            {
                if (occupiedSlots[i] == null) return true;
            }
            return false;
        }

        /// <summary>
        /// Giải phóng ô khi súng hết đạn
        /// </summary>
        public void ReleaseSlot(GunTurret turret)
        {
            for (int i = 0; i < occupiedSlots.Length; i++)
            {
                if (occupiedSlots[i] == turret)
                {
                    occupiedSlots[i] = null;
                    break;
                }
            }
        }

        /// <summary>
        /// Tìm và trả về Transform của ô trống tiếp theo, đồng thời đặt chỗ trước
        /// </summary>
        public Transform GetNextAvailableSlot()
        {
            for (int i = 0; i < occupiedSlots.Length; i++)
            {
                if (occupiedSlots[i] == null && !reservedSlots[i])
                {
                    reservedSlots[i] = true; // Khóa tạm thời, súng đang trên đường bay đến
                    return activeSlotPoints[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Được gọi khi súng hoàn tất cú DOJump và chạm đất an toàn tại Active Slot
        /// </summary>
        public void RegisterTurret(GunTurret turret, Transform slotTransform)
        {
            // Tìm index của slot dựa vào Transform nhận được
            int slotIndex = activeSlotPoints.IndexOf(slotTransform);

            if (slotIndex != -1)
            {
                occupiedSlots[slotIndex] = turret;
                reservedSlots[slotIndex] = false; // Giải phóng trạng thái đặt chỗ, súng đã đến nơi

                // Đóng đinh súng vào vị trí ô chiến đấu
                turret.transform.SetParent(slotTransform);
                turret.transform.localPosition = Vector3.zero;
                turret.transform.localRotation = Quaternion.identity;
            }
            else
            {
                Debug.LogError("Không tìm thấy slot tương ứng với Transform này trên ActiveBar!");
            }
        }
    }
}