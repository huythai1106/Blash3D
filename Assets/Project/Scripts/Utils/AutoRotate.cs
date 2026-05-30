using UnityEngine;

public class AutoRotate : MonoBehaviour
{
    [Tooltip("Tốc độ xoay (độ/giây)")]
    [SerializeField] private float speed = 100f;

    [Tooltip("Trục muốn xoay, mặc định là trục Y")]
    [SerializeField] private Vector3 axis = Vector3.up;

    // Tối ưu: Dùng Update cho chuyển động liên tục
    private void Update()
    {
        // Xoay object dựa trên thời gian thực (Time.deltaTime) 
        // để đảm bảo tốc độ xoay không phụ thuộc vào khung hình (FPS)
        transform.Rotate(axis * speed * Time.deltaTime);
    }
}