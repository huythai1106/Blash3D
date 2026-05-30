using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CubeLand.Gameplay;

public enum LayoutGenerationType
{
    Random,      // Ngẫu nhiên
    Sequential   // Theo lượt tuần tự
}

public class BoardGenConfigPopUp : EditorWindow
{
    private LayoutGenerationType genType = LayoutGenerationType.Random;
    private int maxAmmoPerShooter = 60;

    // Callback dùng để trả thông số ngược về cho Window chính khi nhấn "Generate"
    private Action<LayoutGenerationType, int> onGenerateCallback;

    /// <summary>
    /// Hàm khởi tạo mở Cửa sổ Popup cấu hình sinh board nhanh
    /// </summary>
    public static void Init(Action<LayoutGenerationType, int> onGenerate)
    {
        BoardGenConfigPopUp window = GetWindow<BoardGenConfigPopUp>(true, "Cấu hình tạo nhanh bàn cờ", true);
        window.minSize = new Vector2(320, 180);
        window.maxSize = new Vector2(320, 180);
        window.onGenerateCallback = onGenerate;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        GUILayout.Label("THÔNG SỐ SINH BOARD TỰ ĐỘNG", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        // 1. Chọn kiểu thuật toán sắp xếp
        genType = (LayoutGenerationType)EditorGUILayout.EnumPopup("Kiểu sắp xếp:", genType);

        // 2. Nhập giới hạn đạn lớn nhất
        maxAmmoPerShooter = EditorGUILayout.IntField("Số đạn tối đa/màu:", maxAmmoPerShooter);
        if (maxAmmoPerShooter <= 0) maxAmmoPerShooter = 1;

        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        // Khu vực nút xử lý dưới chân Popup
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Hủy bỏ", GUILayout.Height(28)))
        {
            Close();
        }

        GUI.backgroundColor = new Color(0.9f, 0.5f, 0f); // Màu cam nổi bật cho nút hành động mạnh
        if (GUILayout.Button("⚡ Bắt đầu tạo", GUILayout.Height(28)))
        {
            // Bắn dữ liệu cấu hình đã nhập về qua Callback
            onGenerateCallback?.Invoke(genType, maxAmmoPerShooter);
            Close();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);
    }
}