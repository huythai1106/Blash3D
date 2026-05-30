using UnityEngine;

public class ParticleAutoPool : MonoBehaviour
{
    // Hàm này được Unity tự động gọi khi Particle kết thúc (nếu chọn Stop Action: Callback)
    private void OnParticleSystemStopped()
    {
        // Trả về pool thay vì hủy (Destroy)
        SimplePool.Instance.Despawn(this.gameObject);
    }
}