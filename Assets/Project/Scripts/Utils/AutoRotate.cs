using CubeLand.Gameplay;
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
        if (GameManager.Instance.CurrentInputState == GameInputState.None)
            transform.Rotate(speed * Time.deltaTime * axis);
    }
}