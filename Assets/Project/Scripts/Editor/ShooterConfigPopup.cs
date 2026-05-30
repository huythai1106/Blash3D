using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CubeLand.Gameplay;

public class ShooterConfigPopUp : EditorWindow
{
    private ShooterType shooterType;
    private int ammoCount;
    private int freezeTurns;

    // Danh sách các màu mà ụ súng này đang sở hữu
    private List<string> selectedColors = new List<string>();

    // Danh sách toàn bộ các màu hợp lệ đọc từ file Voxel JSON
    private List<string> poolAvailableColors = new List<string>();

    // Kho đạn thực tế hiện tại của map (đã nhận từ Window chính)
    private Dictionary<string, int> mapRemainingColors = new Dictionary<string, int>();

    // Lưu lại số lượng đạn cũ trước khi sửa (dùng để tính toán hoàn tác bộ nhớ tạm)
    private int originalAmmoCount = 0;
    private List<string> originalColors = new List<string>();
    private bool isEditMode = false;

    // Callback trả data về Editor Window chính
    private Action<ShooterConfig> onSaveCallback;

    /// <summary>
    /// Hàm khởi tạo PopUp chỉnh sửa / tạo mới
    /// </summary>
    public static void Init(ShooterConfig existingConfig, Dictionary<string, int> currentRemainingColors, List<string> availableColors, Action<ShooterConfig> onSave)
    {
        ShooterConfigPopUp window = GetWindow<ShooterConfigPopUp>(true, "Cấu hình Ụ Súng Đa Màu", true);
        window.minSize = new Vector2(360, 450);
        window.maxSize = new Vector2(360, 650);

        window.onSaveCallback = onSave;
        window.poolAvailableColors = availableColors != null ? new List<string>(availableColors) : new List<string>();

        // Nhận kho đạn từ Window chính sang để check Over-flow
        window.mapRemainingColors = currentRemainingColors != null ? new Dictionary<string, int>(currentRemainingColors) : new Dictionary<string, int>();

        if (existingConfig != null)
        {
            window.isEditMode = true;
            window.shooterType = existingConfig.type;
            window.ammoCount = existingConfig.ammoCount;
            window.freezeTurns = existingConfig.freezeTurns;
            window.selectedColors = existingConfig.colorsHex != null ? new List<string>(existingConfig.colorsHex) : new List<string>();

            // Backup lại thông số cũ để làm mốc tính toán bù trừ ảo
            window.originalAmmoCount = existingConfig.ammoCount;
            window.originalColors = new List<string>(window.selectedColors);
        }
        else
        {
            window.isEditMode = false;
            window.shooterType = ShooterType.Normal;
            window.ammoCount = 10;
            window.freezeTurns = 0;
            window.selectedColors = new List<string>();

            if (window.poolAvailableColors.Count > 0)
            {
                window.selectedColors.Add(window.poolAvailableColors[0]);
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        GUILayout.Label("THÔNG TIN CƠ BẢNỤ SÚNG", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        shooterType = (ShooterType)EditorGUILayout.EnumPopup("Loại ụ súng:", shooterType);
        ammoCount = EditorGUILayout.IntSlider("Số lượng đạn (Chung):", ammoCount, 1, 200); // Sử dụng Slider cho trực quan khi test warning

        if (shooterType == ShooterType.Frozen)
        {
            freezeTurns = EditorGUILayout.IntField("Số lượt đóng băng:", freezeTurns);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // --- KHU VỰC THÊM MÀU ---
        GUILayout.BeginHorizontal();
        GUILayout.Label("DANH SÁCH MÀU SỞ HỮU:", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.2f, 0.6f, 1f);
        if (GUILayout.Button("＋ Thêm màu", GUILayout.Width(95), GUILayout.Height(18)))
        {
            ShowColorSelectionMenu();
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        if (selectedColors.Count == 0)
        {
            EditorGUILayout.HelpBox("Ụ súng này chưa có màu nào! Vui lòng bấm nút (＋ Thêm màu) bên trên.", MessageType.Error);
        }

        // --- HIỂN THỊ DANH SÁCH MÀU & KIỂM TRA LIMIT KHO ĐẠN ---
        EditorGUILayout.BeginVertical("box");
        int indexToRemove = -1;
        bool hasOverallocatedColor = false; // Biến đánh dấu xem có màu nào bị quá tải không
        List<string> errorMessages = new List<string>();

        for (int i = 0; i < selectedColors.Count; i++)
        {
            string currentHex = selectedColors[i];
            GUILayout.BeginHorizontal("box");

            // 1. Vẽ Box Color Preview
            string safeHex = currentHex.StartsWith("#") ? currentHex : "#" + currentHex;
            if (ColorUtility.TryParseHtmlString(safeHex, out Color previewColor))
            {
                GUI.backgroundColor = previewColor;
                GUILayout.Box("", GUILayout.Width(20), GUILayout.Height(18));
                GUI.backgroundColor = Color.white;
            }

            // 2. Tính toán số lượng đạn ảo thực tế còn lại trong kho cho màu này
            int inWarehouse = mapRemainingColors.ContainsKey(currentHex) ? mapRemainingColors[currentHex] : 0;

            // Logic bù trừ: Nếu đang ở chế độ Edit và màu này vốn dĩ đã nằm trong súng từ trước,
            // ta phải cộng trả lại số đạn gốc của nó vào kho ảo trước khi trừ đi lượng đạn mới chỉnh sửa.
            if (isEditMode && originalColors.Contains(currentHex))
            {
                inWarehouse += originalAmmoCount;
            }

            int finalCalculatedRemaining = inWarehouse - ammoCount;

            // 3. Render thông tin & Check hiển thị Warning màu chữ
            if (finalCalculatedRemaining < 0)
            {
                hasOverallocatedColor = true;
                errorMessages.Add($"Màu {currentHex} bị thiếu {Mathf.Abs(finalCalculatedRemaining)} viên trong kho tổng!");

                GUI.skin.label.normal.textColor = Color.red;
                GUILayout.Label($"{currentHex} (Kho: {inWarehouse} | Sau cấu hình: {finalCalculatedRemaining})", EditorStyles.boldLabel);
                GUI.skin.label.normal.textColor = Color.white;
            }
            else
            {
                GUILayout.Label($"{currentHex} (Kho còn lại: {finalCalculatedRemaining} viên)", EditorStyles.label);
            }

            GUILayout.FlexibleSpace();

            // 4. Nút Xóa (X) màu
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(16)))
            {
                indexToRemove = i;
            }
            GUI.backgroundColor = Color.white;

            GUILayout.EndHorizontal();
        }

        if (indexToRemove >= 0)
        {
            selectedColors.RemoveAt(indexToRemove);
        }
        EditorGUILayout.EndVertical();

        // --- KHU VỰC BOX CẢNH BÁO TOÀN CỤC ---
        if (hasOverallocatedColor)
        {
            EditorGUILayout.Space(5);
            string fullWarningText = "CẢNH BÁO VƯỢT QUÁ SỐ LƯỢNG VOXEL TRONG FILE JSON:\n" + string.Join("\n", errorMessages);
            EditorGUILayout.HelpBox(fullWarningText, MessageType.Warning);
        }

        GUILayout.FlexibleSpace();

        // --- KHU VỰC BUTTONS FOOTER ---
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Hủy bỏ (Cancel)", GUILayout.Height(30)))
        {
            Close();
        }

        // Nếu có màu bị quá tải, ta nhuộm nút Save thành màu vàng cam cảnh báo (vẫn cho lưu nếu dev cố tình force map)
        if (hasOverallocatedColor) GUI.backgroundColor = new Color(1f, 0.6f, 0f);
        else GUI.backgroundColor = new Color(0.1f, 0.6f, 0.2f);

        string saveBtnText = hasOverallocatedColor ? "Vẫn Lưu (Bỏ qua cảnh báo)" : "Lưu cấu hình (Save)";
        if (GUILayout.Button(saveBtnText, GUILayout.Height(30)))
        {
            if (selectedColors.Count == 0)
            {
                EditorUtility.DisplayDialog("Lỗi cấu hình", "Ụ súng bắt buộc phải sở hữu ít nhất 1 màu sắc để hoạt động!", "OK");
                return;
            }

            ShooterConfig finalConfig = new ShooterConfig()
            {
                type = this.shooterType,
                ammoCount = this.ammoCount,
                freezeTurns = this.freezeTurns,
                colorsHex = new List<string>(this.selectedColors)
            };

            onSaveCallback?.Invoke(finalConfig);
            Close();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);
    }

    private void ShowColorSelectionMenu()
    {
        GenericMenu menu = new GenericMenu();
        if (poolAvailableColors == null || poolAvailableColors.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("Không tìm thấy màu nào trong file JSON"));
            menu.ShowAsContext();
            return;
        }

        foreach (string colorHex in poolAvailableColors)
        {
            bool isAlreadyAdded = selectedColors.Contains(colorHex);
            string menuPath = isAlreadyAdded ? $"{colorHex} (Đang sở hữu)" : colorHex;

            menu.AddItem(new GUIContent(menuPath), isAlreadyAdded, () =>
            {
                if (!selectedColors.Contains(colorHex)) selectedColors.Add(colorHex);
                else selectedColors.Remove(colorHex);
            });
        }
        menu.ShowAsContext();
    }
}