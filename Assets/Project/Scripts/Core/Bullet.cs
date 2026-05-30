using UnityEngine;

namespace CubeLand.Gameplay
{
    public class Bullet : MonoBehaviour
    {
        [SerializeField] private float speed = 25f; // Tăng tốc độ bay cho mượt
        [SerializeField] private MeshRenderer bulletRenderer;
        [SerializeField] private TrailRenderer trailRenderer;
        private VoxelControl targetVoxel;
        private bool isLaunched = false;


        private void Start()
        {
            speed = GameManager.Instance.gunTurretConfig.bulletSpeed;
        }

        public void Launch(VoxelControl target, Color color)
        {
            this.targetVoxel = target;
            this.isLaunched = true;


            trailRenderer.emitting = true;
        }

        private void Update()
        {
            if (!isLaunched || targetVoxel == null) return;

            // 1. Di chuyển hướng về phía mục tiêu
            Vector3 targetPos = targetVoxel.transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

            // 2. Kiểm tra khoảng cách: Nếu đã đến vị trí của Voxel (sai số nhỏ)
            if (Vector3.Distance(transform.position, targetPos) < 0.1f)
            {
                // Gọi phá hủy Voxel
                targetVoxel.OnHitByBullet();

                // Thu hồi đạn về Pool an toàn
                isLaunched = false;

                trailRenderer.emitting = false;
                trailRenderer.Clear();
                SimplePool.Instance.Despawn(gameObject);
            }
        }
    }
}