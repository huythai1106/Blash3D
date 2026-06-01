using CubeLand.Gameplay;
using UnityEngine;

namespace CubeLand.Gameplay
{
    public class UnifiedRotationControl : MonoBehaviour
    {
        [Header("Swipe Settings")]
        [Tooltip("Tốc độ phản hồi ngón tay")]
        [SerializeField] private float swipeSpeed = 0.2f;
        [Tooltip("Độ mượt khi xoay (càng thấp càng trượt dài)")]
        [SerializeField] private float smoothSpeed = 10f;
        [Tooltip("Giới hạn góc ngẩng/cúi (Trục X)")]
        [SerializeField] private Vector2 pitchLimit = new Vector2(-80f, 80f);

        [Header("Invert Direction (Đảo hướng vuốt)")]
        [SerializeField] private bool invertX = false; // Bật nếu muốn đảo hướng Trái/Phải
        [SerializeField] private bool invertY = false; // Bật nếu muốn đảo hướng Lên/Xuống

        [Header("Auto Rotate Settings")]
        [Tooltip("Bật/Tắt tính năng tự động xoay khi rảnh")]
        [SerializeField] private bool enableAutoRotate = true;
        [Tooltip("Tốc độ tự động xoay (độ/giây) quanh trục Y")]
        [SerializeField] private float autoRotateSpeed = 30f;

        private float yawAngle;
        private float pitchAngle;
        private Quaternion targetRotation;

        private int activeFingerId = -1;
        private Vector2 lastMousePosition;
        private bool isMouseDragging = false;
        private bool hasCrossedDragThreshold = false;

        private void Start()
        {
            Vector3 currentEuler = transform.eulerAngles;
            yawAngle = currentEuler.y;
            pitchAngle = currentEuler.x;

            if (pitchAngle > 180f) pitchAngle -= 360f;

            targetRotation = transform.rotation;
        }

        private void Update()
        {
            Vector2 inputDelta = GetInputDelta();

            // Đang giữ tay trên màn hình (hoặc giữ chuột)
            bool isInteracting = (activeFingerId != -1) || isMouseDragging;

            if (isInteracting)
            {
                // 1. Kiểm tra nếu có sự di chuyển vượt ngưỡng -> Đánh dấu là đã vuốt
                if (inputDelta.sqrMagnitude > 0.01f)
                {
                    hasCrossedDragThreshold = true; // Cờ này sẽ giữ nguyên True cho đến khi thả tay

                    float deltaX = invertX ? inputDelta.x : -inputDelta.x;
                    float deltaY = invertY ? inputDelta.y : -inputDelta.y;

                    yawAngle -= deltaX * swipeSpeed;
                    pitchAngle += deltaY * swipeSpeed;

                    if (pitchAngle > 360f) pitchAngle -= 360f;
                    if (pitchAngle < -360f) pitchAngle += 360f;
                }

                // 2. Chỉ chuyển InputState sang Dragging nếu tay ĐÃ từng di chuyển
                // Nếu người chơi chỉ Tap (chạm) rồi giữ nguyên, State sẽ không bị biến thành Dragging
                if (hasCrossedDragThreshold)
                {
                    GameManager.Instance.CurrentInputState = GameInputState.Dragging;
                }

                // Dù ngón tay đang dừng hay đang vuốt, vẫn cập nhật target để nội suy Slerp chạy tới đích
                targetRotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
            }
            else
            {
                // 3. THẢ TAY RA: Reset mọi thứ về trạng thái ban đầu
                hasCrossedDragThreshold = false;
                GameManager.Instance.CurrentInputState = GameInputState.None;

                if (enableAutoRotate)
                {
                    yawAngle += autoRotateSpeed * Time.deltaTime;

                    if (yawAngle > 360f) yawAngle -= 360f;
                    if (yawAngle < -360f) yawAngle += 360f;

                    targetRotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
                }
            }

            // 4. Nội suy Slerp mượt mà
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * smoothSpeed
            );
        }

        private Vector2 GetInputDelta()
        {
            if (Input.touchCount > 0)
            {
                foreach (Touch touch in Input.touches)
                {
                    if (touch.phase == TouchPhase.Began && activeFingerId == -1)
                    {
                        // KIỂM TRA: Chỉ khóa ngón tay này nếu nó chạm ở NỬA TRÊN màn hình
                        if (touch.position.y > Screen.height * 0.5f)
                        {
                            activeFingerId = touch.fingerId;
                        }
                    }

                    if (touch.fingerId == activeFingerId)
                    {
                        if (touch.phase == TouchPhase.Moved)
                        {
                            return touch.deltaPosition;
                        }
                        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        {
                            activeFingerId = -1;
                        }
                    }
                }
                return Vector2.zero;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (Input.mousePosition.y > Screen.height * 0.5f)
                {
                    isMouseDragging = true;
                    lastMousePosition = Input.mousePosition;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isMouseDragging = false;
            }

            if (isMouseDragging)
            {
                Vector2 currentMousePos = Input.mousePosition;
                Vector2 delta = currentMousePos - lastMousePosition;
                lastMousePosition = currentMousePos;
                return delta;
            }

            return Vector2.zero;
        }
    }
}