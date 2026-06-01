// File: InputManager.cs
using UnityEngine;
using UnityEngine.EventSystems;

namespace CubeLand.Core
{
    public class InputManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera mainCamera;

        [Header("Raycast Settings")]
        [Tooltip("Layer chứa các vật thể có thể click (ví dụ: Layer 'GridCell')")]
        [SerializeField] private LayerMask interactableLayer;
        [SerializeField] private float rayDistance = 100f;

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void Update()
        {
            // Bắt sự kiện nhấn chuột trái (hoặc chạm màn hình cảm ứng)
            if (Input.GetMouseButtonDown(0))
            {
                ProcessClick();
            }
        }

        private void ProcessClick()
        {
            // 1. CHẶN CLICK XUYÊN UI: 
            // Nếu người chơi đang click vào một nút UI (như nút spawn súng), ngừng xử lý Raycast xuống không gian 3D
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (mainCamera == null) return;

            // 2. BẮN TIA RAYCAST:
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            // Dùng LayerMask gạt bỏ mọi tính toán va chạm với các layer không liên quan (như Đạn, Voxel lớp trong, UI...)
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, interactableLayer))
            {
                // 3. XỬ LÝ SỰ KIỆN TỐI ƯU:
                // Sử dụng TryGetComponent thay vì GetComponent để đạt Zero GC Allocation

                // Cách A: Nếu bạn dùng Interface IInteractable (Kiến trúc linh hoạt)
                if (hit.collider.TryGetComponent(out IInteractable interactableTarget))
                {
                    interactableTarget.OnInteract();
                }

                /* // Cách B: Gọi trực tiếp vào script Cell của bạn (nếu không muốn dùng Interface)
                if (hit.collider.TryGetComponent(out Cell clickedCell))
                {
                    clickedCell.OnCellClicked();
                }
                */
            }
        }
    }
}