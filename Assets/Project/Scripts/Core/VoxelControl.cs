using System;
using System.Collections;
using UnityEngine;

namespace CubeLand.Gameplay
{
    public class VoxelControl : MonoBehaviour
    {
        [SerializeField] private MeshRenderer voxelRenderer;

        public Color MyColor { get; private set; }
        public Vector3Int GridPosition { get; private set; }
        public Bounds VoxelBounds => voxelRenderer.bounds;
        public BoxCollider boxCollider;

        // --- CỜ ĐÁNH DẤU TRẠNG THÁI ---
        public bool IsOuterLayer { get; private set; } // Có nằm ở lớp vỏ ngoài cùng không?
        public bool IsTargeted { get; private set; }   // Đã bị viên đạn nào nhắm mục tiêu chưa?

        private LevelCreator cachedLevelCreator;
        [SerializeField] private ParticleSystem voxelHitEffect;

        public void Init(Color color, Vector3Int gridPos)
        {
            this.cachedLevelCreator = FindObjectOfType<LevelCreator>();
            this.MyColor = color;
            this.GridPosition = gridPos;
            this.IsTargeted = false; // Mặc định chưa bị nhắm

            voxelRenderer.SetColorOptimized(color, RendererExtensions.ColorPropId);
        }

        // Bật / tắt hiển thị hoặc logic thay cho Collider vật lý
        public void SetOuterLayerState(bool isOuter)
        {
            this.IsOuterLayer = isOuter;
            gameObject.SetActive(isOuter); // Chỉ kích hoạt GameObject nếu là lớp vỏ ngoài cùng (để tiết kiệm hiệu năng)
            boxCollider.enabled = isOuter; // Chỉ bật Collider nếu là lớp vỏ ngoài cùng
        }

        // Đạn gọi hàm này để khóa mục tiêu
        public void LockTarget()
        {
            IsTargeted = true;
        }

        public bool CheckMatchColor(Color bulletColor)
        {
            return Vector4.Distance(MyColor, bulletColor) < 0.1f;
        }

        public void OnHitByBullet()
        {
            PlayAnimationOnHit(() =>
            {
                cachedLevelCreator.RemoveVoxel(this);
            });

        }

        private void PlayAnimationOnHit(Action onComplete)
        {
            // Bắt đầu Coroutine để chạy animation mà không làm dừng logic game
            StartCoroutine(HitSequence(onComplete));
        }

        private IEnumerator HitSequence(Action onComplete)
        {
            // 1. Phát hiệu ứng hạt từ Pool
            if (voxelHitEffect != null)
            {
                // Giả sử SimplePool hỗ trợ lấy prefab particle và tự động trả về sau thời gian định sẵn
                GameObject effect = SimplePool.Instance.Spawn(voxelHitEffect.gameObject, transform.position, Quaternion.identity);

                // Nếu bạn muốn đổi màu hạt theo màu Voxel
                var ps = effect.GetComponent<ParticleSystem>();
                ps.Play();
                ps.GetComponent<ParticleSystemRenderer>().SetColorOptimized(MyColor, RendererExtensions.ColorPropId);
            }

            // 2. Animation "Vỡ vụn" (Scale lên rồi thu nhỏ về 0)
            float duration = 0.2f; // Thời gian animation
            Vector3 startScale = transform.localScale;
            Vector3 endScale = Vector3.zero;

            // Phóng to nhẹ trước khi biến mất (cảm giác "nổ" nhẹ)
            transform.localScale = startScale * 1.1f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Thu nhỏ dần về 0
                transform.localScale = Vector3.Lerp(startScale * 2f, endScale, t);

                // Fade out alpha nhẹ nếu cần (tùy vào Shader của bạn)
                yield return null;
            }

            // 3. Kết thúc
            onComplete?.Invoke();
        }

        public bool IsExposedAndFacingCamera(Camera cam, LayerMask voxelLayer)
        {
            // 1. Chặn lõi
            if (!this.IsOuterLayer) return false;

            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null) return false;

            Vector3 camForward = cam.transform.forward;
            bool hasFacingFace = false;

            // 2. Lọc Backface Culling (Gạt bỏ các khối hoàn toàn quay lưng)
            for (int i = 0; i < 6; i++)
            {
                Vector3Int dir = LevelCreator.neighborOffsets[i];
                Vector3Int neighborPos = this.GridPosition + dir;

                if (!cachedLevelCreator.voxelGrid.ContainsKey(neighborPos))
                {
                    Vector3 worldNormal = cachedLevelCreator.modelRoot.TransformDirection((Vector3)dir);
                    if (Vector3.Dot(worldNormal, camForward) < 0f)
                    {
                        hasFacingFace = true;
                        break;
                    }
                }
            }

            if (!hasFacingFace) return false;

            // =================================================================
            // 3. MULTI-POINT RAYCAST (Chỉ hở 1 chút là bắn)
            // =================================================================
            Bounds bounds = col.bounds;
            Vector3 c = bounds.center;

            // Thu nhỏ vùng quét lại 20% (nhân 0.8f) để tránh các tia ở góc bị 
            // bắn sượt qua mép (floating-point error) và chạm nhầm vào tường láng giềng.
            Vector3 e = bounds.extents * 0.8f;
            float rayLength = 200f;

            // Ưu tiên check TÂM trước (Hầu hết các khối lộ thiên sẽ pass ngay tại dòng này)
            if (CheckVisibilityRay(c, camForward, rayLength, voxelLayer)) return true;

            // Nếu tâm bị che, ta tiếp tục "soi" 8 góc của khối Voxel
            if (CheckVisibilityRay(c + new Vector3(e.x, e.y, e.z), camForward, rayLength, voxelLayer)) return true;
            if (CheckVisibilityRay(c + new Vector3(e.x, e.y, -e.z), camForward, rayLength, voxelLayer)) return true;
            if (CheckVisibilityRay(c + new Vector3(e.x, -e.y, e.z), camForward, rayLength, voxelLayer)) return true;
            if (CheckVisibilityRay(c + new Vector3(e.x, -e.y, -e.z), camForward, rayLength, voxelLayer)) return true;
            if (CheckVisibilityRay(c + new Vector3(-e.x, e.y, e.z), camForward, rayLength, voxelLayer)) return true;
            if (CheckVisibilityRay(c + new Vector3(-e.x, e.y, -e.z), camForward, rayLength, voxelLayer)) return true;
            if (CheckVisibilityRay(c + new Vector3(-e.x, -e.y, e.z), camForward, rayLength, voxelLayer)) return true;
            if (CheckVisibilityRay(c + new Vector3(-e.x, -e.y, -e.z), camForward, rayLength, voxelLayer)) return true;

            // Đã soi hết các góc mà vẫn bị che -> Mục tiêu thực sự đang nấp hoàn toàn
            return false;
        }

        // Hàm hỗ trợ bắn Raycast gọn gàng (Không sinh rác bộ nhớ)
        private bool CheckVisibilityRay(Vector3 targetPoint, Vector3 camForward, float rayLength, LayerMask voxelLayer)
        {
            // Bắn 1 tia từ ngoài màn hình thẳng vào cái điểm targetPoint
            Vector3 rayOrigin = targetPoint - camForward * rayLength;
            if (Physics.Raycast(rayOrigin, camForward, out RaycastHit hit, rayLength + 1f, voxelLayer))
            {
                return hit.collider.gameObject == this.gameObject;
            }
            return false;
        }
    }
}