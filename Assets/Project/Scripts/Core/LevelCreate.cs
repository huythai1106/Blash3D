using System.Collections.Generic;
using UnityEngine;

namespace CubeLand.Gameplay
{
    public class LevelCreator : MonoBehaviour
    {
        [SerializeField] private GameObject voxelPrefab;
        public Transform modelRoot;

        private Camera mainCamera;
        private Plane[] frustumPlanes = new Plane[6];

        // Spatial Hashing: Map tọa độ Vector3Int chuẩn xác với khối Voxel tại đó
        public Dictionary<Vector3Int, VoxelControl> voxelGrid = new Dictionary<Vector3Int, VoxelControl>(5000);

        // Cache lại mảng 6 hướng để tránh cấp phát mảng mới trong vòng lặp (Zero-Allocation)
        public static readonly Vector3Int[] neighborOffsets = new Vector3Int[]
        {
            Vector3Int.up, Vector3Int.down,
            Vector3Int.left, Vector3Int.right,
            Vector3Int.forward, Vector3Int.back
        };

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        public int ActiveVoxelCount => voxelGrid.Count;

        public void GenerateVoxelModel(VoxelData voxelData)
        {
            voxelGrid.Clear();
            if (voxelData == null || voxelData.voxels == null || voxelData.voxels.Count == 0) return;

            // Tính toán Center Offset
            Vector3 centerOffset = new Vector3(
                (voxelData.size.x - 1) / 2f,
                (voxelData.size.y - 1) / 2f,
                (voxelData.size.z - 1) / 2f
            );

            foreach (var voxelInfo in voxelData.voxels)
            {
                Vector3Int gridPos = new Vector3Int(
                    Mathf.RoundToInt(voxelInfo.x),
                    Mathf.RoundToInt(voxelInfo.y),
                    Mathf.RoundToInt(voxelInfo.z)
                );

                // 1. Tọa độ này giờ sẽ là Local Position (so với modelRoot)
                Vector3 localPhysicalPos = (Vector3)gridPos - centerOffset;

                // 2. CHỈ truyền parent vào Instantiate để nó sinh ra làm con của modelRoot trước
                GameObject go = Instantiate(voxelPrefab, modelRoot);

                // 3. Gán trực tiếp vào localPosition và localRotation
                go.transform.localPosition = localPhysicalPos;
                go.transform.localRotation = Quaternion.identity;

                // (Tùy chọn) Đảm bảo scale của từng viên Voxel không bị biến dạng
                go.transform.localScale = Vector3.one;

                VoxelControl voxelCtrl = go.GetComponent<VoxelControl>();

                ColorUtility.TryParseHtmlString(voxelInfo.color, out Color voxelColor);
                voxelCtrl.Init(voxelColor, gridPos);

                voxelGrid[gridPos] = voxelCtrl;
            }

            OptimizeVoxelLayers();
        }


        /// Thuật toán quét và chỉ đánh dấu các Voxel nằm ở lớp vỏ ngoài cùng (Outer Layer)
        /// </summary>
        private void OptimizeVoxelLayers()
        {
            foreach (var kvp in voxelGrid)
            {
                Vector3Int pos = kvp.Key;
                VoxelControl voxel = kvp.Value;
                bool isOuter = false;

                // Kiểm tra 6 mặt của Voxel này
                for (int i = 0; i < 6; i++)
                {
                    Vector3Int neighborPos = pos + neighborOffsets[i];

                    // Nếu có ít nhất 1 hướng bị trống (không có voxel hàng xóm) -> Đây là lớp vỏ bọc ngoài
                    if (!voxelGrid.ContainsKey(neighborPos))
                    {
                        isOuter = true;
                        break;
                    }
                }

                // CẬP NHẬT: Không dùng Collider nữa, chỉ set cờ trạng thái logic
                voxel.SetOuterLayerState(isOuter);
            }
        }

        /// <summary>
        /// Được gọi khi đạn bắn vỡ một khối Voxel
        /// </summary>
        public void RemoveVoxel(VoxelControl voxel)
        {
            Vector3Int pos = voxel.GridPosition;

            // Xóa voxel khỏi lưới quản lý
            if (voxelGrid.Remove(pos))
            {
                // BƯỚC QUAN TRỌNG: Đánh thức (Awaken) các Voxel bên trong vừa bị lộ ra ngoài
                for (int i = 0; i < 6; i++)
                {
                    Vector3Int neighborPos = pos + neighborOffsets[i];

                    // Lấy ra Voxel hàng xóm siêu nhanh bằng O(1)
                    if (voxelGrid.TryGetValue(neighborPos, out VoxelControl neighbor))
                    {
                        // CẬP NHẬT: Đánh dấu Voxel bên trong này trở thành lớp vỏ ngoài để súng có thể nhắm bắn
                        neighbor.SetOuterLayerState(true);
                    }
                }

                Destroy(voxel.gameObject); // Vẫn Destroy (Hoặc đổi thành Pool nếu bạn làm Pool cho Voxel)
                LevelManager.Instance.CheckWinCondition();
            }
        }

        /// <summary>
        /// Tìm mục tiêu thỏa mãn: Lớp ngoài + Cùng màu + Chưa bị nhắm + Nằm trong Camera
        /// </summary>
        public VoxelControl FindAvailableTarget(Color targetColor)
        {
            if (mainCamera == null) return null;

            // Tính toán 6 mặt phẳng giới hạn của Camera (Ghi đè vào mảng có sẵn -> Zero Allocation)
            GeometryUtility.CalculateFrustumPlanes(mainCamera, frustumPlanes);

            foreach (var kvp in voxelGrid)
            {
                VoxelControl voxel = kvp.Value;

                // BƯỚC 1: Lọc điều kiện cơ bản (O(1) - Cực nhanh)
                if (voxel.MyColor != targetColor || voxel.IsTargeted)
                {
                    continue;
                }

                // BƯỚC 2: Đẩy Frustum Culling lên trước bằng toán học AABB
                // Chỉ những viên thực sự nằm trong tầm nhìn của Camera mới đi tiếp xuống bước 3
                if (!GeometryUtility.TestPlanesAABB(frustumPlanes, voxel.VoxelBounds))
                {
                    continue;
                }

                // BƯỚC 3: Kiểm tra hướng bề mặt + Raycast vật cản
                // Nhờ bước 2 lọc trước, số lượng Voxel phải chạy Raycast ở đây giảm đi từ 70% - 80%
                if (!voxel.IsExposedAndFacingCamera(mainCamera, GameManager.Instance.layerVoxel))
                {
                    continue;
                }

                // Thỏa mãn tất cả điều kiện chặt chẽ
                voxel.LockTarget();
                return voxel;
            }

            return null; // Không còn mục tiêu nào thỏa mãn trên màn hình
        }
    }
}