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

        public bool IsExposedAndFacingCamera(Camera cam)
        {
            // 1. CHẶN ĐỨNG NGAY LẬP TỨC nếu Voxel đang nằm sâu trong lõi (O(1))
            if (!this.IsOuterLayer) return false;

            Vector3 camForward = cam.transform.forward;

            // 2. Chỉ những viên lớp vỏ ngoài cùng mới phải chạy logic Backface Culling
            for (int i = 0; i < 6; i++) // Dùng hằng số 6 thay vì .Length để trình biên dịch tối ưu (Unroll)
            {
                Vector3Int dir = LevelCreator.neighborOffsets[i];
                Vector3Int neighborPos = this.GridPosition + dir;

                // Nếu hướng này bị khuyết láng giềng -> Mặt này lộ thiên
                if (!cachedLevelCreator.voxelGrid.ContainsKey(neighborPos))
                {
                    // Tính vector pháp tuyến (Normal) của mặt lộ thiên trong không gian thế giới
                    Vector3 worldNormal = cachedLevelCreator.modelRoot.TransformDirection((Vector3)dir);

                    // Kiểm tra xem mặt lộ thiên đó có quay về hướng Camera không
                    if (Vector3.Dot(worldNormal, camForward) < 0f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}