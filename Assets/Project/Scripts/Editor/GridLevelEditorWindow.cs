using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using CubeLand.Gameplay;
using Newtonsoft.Json;

public class GridLevelEditorWindow : EditorWindow
{
    // --- HỆ THỐNG TRẠNG THÁI LIÊN KẾT Ụ SÚNG ---
    private enum LinkEditMode { Normal, Linking, Unlinking }
    private LinkEditMode currentLinkMode = LinkEditMode.Normal;
    private List<GridSlotNode> temporaryLinkGroup = new List<GridSlotNode>();
    private int maxLinkGroupIdCounter = 1;

    private LevelData targetData;

    // Dữ liệu phân tích từ Voxel JSON (Giữ nguyên gốc để làm mốc tính toán)
    private Dictionary<string, int> baseVoxelColors = new Dictionary<string, int>();
    // Dữ liệu hiển thị thực tế sau khi đã trừ đi lượng đạn đã phân bổ vào map
    private Dictionary<string, int> remainingColors = new Dictionary<string, int>();
    private List<string> availableHexColors = new List<string>();

    private Vector2 scrollPos;
    private int uniqueIdCounter = 1;

    // --- BIẾN QUẢN LÝ KÉO THẢ (DRAG & DROP) ---
    private int draggingIndex = -1;
    private int hoverInsertIndex = -1;
    private Vector2 dragMouseOffset;
    private int draggedIndex = -1;
    private int dropTargetIndex = -1;

    [MenuItem("Tools/Grid Level Editor Tool")]
    public static void ShowWindow()
    {
        GetWindow<GridLevelEditorWindow>("Grid Level Editor");
    }

    private void OnGUI()
    {
        // Theo dõi xem có bất kỳ biến động Object Field nào không
        EditorGUI.BeginChangeCheck();

        GUILayout.Label("GRID LEVEL DESIGNER TOOL (REORDERABLE & LINKABLE)", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // --- KHU VỰC KÉO THẢ DATA ---
        EditorGUILayout.BeginVertical("box");
        LevelData prevData = targetData;
        targetData = (LevelData)EditorGUILayout.ObjectField("Target Grid Data (SO)", targetData, typeof(LevelData), false);

        // Nếu vừa đổi sang một LevelData SO mới, thực hiện nạp dữ liệu lập tức
        if (targetData != prevData && targetData != null)
        {
            LoadFromScriptableObject();
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("📂 Đọc Dữ Liệu (Load SO)")) LoadFromScriptableObject();
        if (GUILayout.Button("💾 Lưu Dữ Liệu (Save SO)")) SaveToScriptableObject();
        GUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        if (EditorGUI.EndChangeCheck())
        {
            if (targetData != null) LoadFromScriptableObject();
        }

        // --- 1. BẢNG THÔNG TIN MÀU SẮC VOXEL CÒN LẠI ---
        DrawColorStatistics();

        // --- 2. BẢNG ĐIỀU KHIỂN CHẾ ĐỘ LIÊN KẾT ---
        DrawLinkControlPanel();

        EditorGUILayout.Space(15);

        if (targetData == null || targetData.gridData == null)
        {
            EditorGUILayout.HelpBox("Vui lòng kéo file ScriptableObject (LevelData) vào để bắt đầu thiết kế.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(10);
        GUILayout.Label("🛠️ TẠO BÀN CỜ NHANH (AUTO-GENERATION)", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.2f, 0.6f, 1f);
        if (GUILayout.Button("⚡ Mở bảng cấu hình tạo Board nhanh...", GUILayout.Height(28)))
        {
            if (targetData == null || targetData.gridData == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng kéo file ScriptableObject (LevelData) vào trước!", "OK");
            }
            else if (baseVoxelColors.Count == 0)
            {
                EditorUtility.DisplayDialog("Lỗi", "Kho dữ liệu Voxel JSON trống hoặc chưa được nạp!", "OK");
            }
            else
            {
                BoardGenConfigPopUp.Init((chosenGenType, chosenMaxAmmo) =>
                {
                    if (EditorUtility.DisplayDialog("Xác nhận", "Hành động này sẽ XÓA SẠCH bàn cờ hiện tại để sinh tự động. Bạn chắc chắn chứ?", "Tiến hành", "Hủy"))
                    {
                        ExecuteGenerateBoard(chosenGenType, chosenMaxAmmo);
                    }
                });
            }
        }
        GUI.backgroundColor = Color.white;

        // --- 3. CẤU HÌNH SỐ CỘT ---
        EditorGUI.BeginChangeCheck();
        int newCols = EditorGUILayout.IntSlider("Số lượng cột (Grid)", targetData.gridData.columnCount, 3, 5);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(targetData, "Change Column Count");
            targetData.gridData.columnCount = newCols;
            EditorUtility.SetDirty(targetData);
            Repaint();
        }

        EditorGUILayout.Space(10);

        // --- 4. BẢNG Ô BÀN CỜ (GRID LAYOUT + DRAG REORDER) ---
        GUILayout.Label("Cấu hình ô bàn cờ (Giữ chuột vào ô để kéo thả sắp xếp):", EditorStyles.boldLabel);

        // Chỉ cho phép xử lý kéo thả sắp xếp vị trí khi đang ở chế độ Normal thường
        if (currentLinkMode == LinkEditMode.Normal)
        {
            HandleDragAndDropEvents();
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        DrawGridSlots();
        EditorGUILayout.EndScrollView();

        if (currentLinkMode == LinkEditMode.Normal && draggingIndex >= 0 && draggingIndex < targetData.gridData.slots.Count)
        {
            DrawFloatingDragVoxel();
        }
    }

    // --- RENDER BẢNG ĐIỀU KHIỂN 3 NÚT LIÊN KẾT TÁC VỤ ---
    private void DrawLinkControlPanel()
    {
        EditorGUILayout.Space(5);
        GUILayout.Label("🔗 CHẾ ĐỘ LIÊN KẾT Ụ SÚNG", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        string modeNotice = "Chế độ: BÀN CỜ THƯỜNG (Kéo thả, chỉnh sửa click chuột phải)";
        MessageType noticeType = MessageType.Info;

        if (currentLinkMode == LinkEditMode.Linking)
        {
            modeNotice = $"ĐANG LIÊN KẾT: Click chuột trái chọn các ụ súng trên bàn cờ (Đã chọn tạm thời: {temporaryLinkGroup.Count} ô).";
            noticeType = MessageType.Warning;
        }
        else if (currentLinkMode == LinkEditMode.Unlinking)
        {
            modeNotice = "ĐANG HỦY LIÊN KẾT: Click chuột trái vào ụ súng bất kỳ để rã toàn bộ nhóm liên kết hiện có của nó.";
            noticeType = MessageType.Error;
        }
        EditorGUILayout.HelpBox(modeNotice, noticeType);

        GUILayout.BeginHorizontal();

        // 1. Nút Bắt đầu liên kết
        GUI.backgroundColor = currentLinkMode == LinkEditMode.Linking ? Color.yellow : Color.white;
        if (GUILayout.Button("🔗 Bắt đầu liên kết", GUILayout.Height(25)))
        {
            currentLinkMode = LinkEditMode.Linking;
            temporaryLinkGroup.Clear();
        }

        // 2. Nút Kết thúc liên kết
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
        if (GUILayout.Button("✔ Kết thúc liên kết", GUILayout.Height(25)))
        {
            if (currentLinkMode == LinkEditMode.Linking)
            {
                ApplyTemporaryLinks();
            }
            currentLinkMode = LinkEditMode.Normal;
        }

        // 3. Nút Hủy liên kết
        GUI.backgroundColor = currentLinkMode == LinkEditMode.Unlinking ? Color.red : Color.white;
        if (GUILayout.Button("❌ Hủy liên kết", GUILayout.Height(25)))
        {
            currentLinkMode = LinkEditMode.Unlinking;
            temporaryLinkGroup.Clear();
        }

        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        if (currentLinkMode != LinkEditMode.Normal)
        {
            if (GUILayout.Button("Thoát chế độ liên kết (Hủy bỏ thao tác)"))
            {
                currentLinkMode = LinkEditMode.Normal;
                temporaryLinkGroup.Clear();
            }
        }

        EditorGUILayout.EndVertical();
    }

    // --- LUỒNG CHỐT HẠ GỘP NHÓM LIÊN KẾT ---
    private void ApplyTemporaryLinks()
    {
        if (temporaryLinkGroup.Count < 2)
        {
            EditorUtility.DisplayDialog("Thông báo", "Cần click chọn ít nhất từ 2 ụ súng trở lên để tạo liên kết nhóm!", "OK");
            temporaryLinkGroup.Clear();
            return;
        }

        Undo.RecordObject(targetData, "Link Shooters Together");

        int assignedGroupId = maxLinkGroupIdCounter++;
        foreach (var slot in temporaryLinkGroup)
        {
            if (slot.shooter != null)
            {
                slot.shooter.linkGroupId = assignedGroupId;
            }
        }

        temporaryLinkGroup.Clear();
        EditorUtility.SetDirty(targetData);
        Debug.Log($"[LinkSystem] Đã đóng gói nhóm liên kết mới thành công có ID: {assignedGroupId}");
    }

    // --- LUỒNG GIẢI PHÓNG RÃ NHÓM ---
    private void BreakLinkGroup(int targetGroupId)
    {
        if (targetGroupId == 0) return;

        Undo.RecordObject(targetData, "Break Shooter Link Group");
        int count = 0;

        foreach (var slot in targetData.gridData.slots)
        {
            if (slot.slotType == SlotType.Shooter && slot.shooter != null && slot.shooter.linkGroupId == targetGroupId)
            {
                slot.shooter.linkGroupId = 0;
                count++;
            }
        }

        EditorUtility.SetDirty(targetData);
        Debug.Log($"[LinkSystem] Đã hủy toàn bộ liên kết của nhóm ID: {targetGroupId} (Gồm {count} ụ súng).");
    }

    private void ParseVoxelJson()
    {
        baseVoxelColors.Clear();
        availableHexColors.Clear();

        try
        {
            if (targetData != null && targetData.voxelData != null && targetData.voxelData.colorCountList != null)
            {
                foreach (var colorCount in targetData.voxelData.colorCountList)
                {
                    baseVoxelColors[colorCount.color] = colorCount.count;
                    availableHexColors.Add(colorCount.color);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GridEditor] Lỗi phân tích cú pháp JSON: {ex.Message}");
        }
    }

    private void RecalculateRemainingColors()
    {
        remainingColors.Clear();
        foreach (var kvp in baseVoxelColors)
        {
            remainingColors[kvp.Key] = kvp.Value;
        }

        if (targetData == null || targetData.gridData == null || targetData.gridData.slots == null) return;

        foreach (var slot in targetData.gridData.slots)
        {
            if (slot.slotType == SlotType.Shooter && slot.shooter != null && slot.shooter.colorsHex != null)
            {
                int ammoToSubtract = slot.shooter.ammoCount;

                foreach (var colorHex in slot.shooter.colorsHex)
                {
                    if (string.IsNullOrEmpty(colorHex)) continue;

                    if (remainingColors.ContainsKey(colorHex))
                    {
                        remainingColors[colorHex] -= ammoToSubtract;
                    }
                }
            }

            if (slot.id >= uniqueIdCounter) uniqueIdCounter = slot.id + 1;
        }
    }

    private void DrawColorStatistics()
    {
        GUILayout.Label("Thống kê Voxel Màu còn lại (Cần phân bổ vào ụ súng):", EditorStyles.boldLabel);
        if (remainingColors.Count == 0)
        {
            EditorGUILayout.HelpBox("Chưa có dữ liệu Voxel hoặc bàn cờ rỗng. Hãy nạp file dữ liệu hợp lệ.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginVertical("box");
        foreach (var key in availableHexColors)
        {
            int count = remainingColors.ContainsKey(key) ? remainingColors[key] : 0;
            GUILayout.BeginHorizontal();

            if (ColorUtility.TryParseHtmlString(key, out Color c))
            {
                GUI.backgroundColor = c;
                GUILayout.Box("", GUILayout.Width(15), GUILayout.Height(15));
                GUI.backgroundColor = Color.white;
            }

            GUILayout.Label($"{key}: {count} Voxel cần phân bổ", count > 0 ? EditorStyles.label : EditorStyles.miniLabel);
            GUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawGridSlots()
    {
        if (targetData == null || targetData.gridData == null || targetData.gridData.slots == null) return;

        float slotWidth = 120f;
        float slotHeight = 150f;
        float spacing = 10f;

        int columnCount = Mathf.Max(1, targetData.gridData.columnCount);
        int totalItems = targetData.gridData.slots.Count + 1;
        int rowCount = Mathf.CeilToInt((float)totalItems / columnCount);
        float totalHeight = rowCount * (slotHeight + spacing);

        Rect gridStartRect = GUILayoutUtility.GetRect(position.width, totalHeight);
        Event e = Event.current;
        dropTargetIndex = -1;

        for (int i = 0; i < targetData.gridData.slots.Count; i++)
        {
            var slotNode = targetData.gridData.slots[i];
            int row = i / columnCount;
            int col = i % columnCount;

            float x = gridStartRect.x + col * (slotWidth + spacing);
            float y = gridStartRect.y + row * (slotHeight + spacing);
            Rect slotRect = new Rect(x, y, slotWidth, slotHeight);

            // --- ĐÁNH DẤU HOVER KHI KÉO THẢ PHẦN TỬ TRONG CHẾ ĐỘ NORMAL ---
            if (currentLinkMode == LinkEditMode.Normal && draggedIndex >= 0 && draggedIndex != i && slotRect.Contains(e.mousePosition))
            {
                dropTargetIndex = i;
            }

            // --- BẮT SỰ KIỆN CLICK CHUỘT TRÊN Ô CỜ ---
            if (slotRect.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    if (currentLinkMode == LinkEditMode.Linking)
                    {
                        if (slotNode.slotType == SlotType.Shooter && slotNode.shooter != null)
                        {
                            if (!temporaryLinkGroup.Contains(slotNode)) temporaryLinkGroup.Add(slotNode);
                            else temporaryLinkGroup.Remove(slotNode);
                            e.Use();
                            Repaint();
                        }
                    }
                    else if (currentLinkMode == LinkEditMode.Unlinking)
                    {
                        if (slotNode.slotType == SlotType.Shooter && slotNode.shooter != null && slotNode.shooter.linkGroupId > 0)
                        {
                            BreakLinkGroup(slotNode.shooter.linkGroupId);
                            e.Use();
                            Repaint();
                        }
                    }
                    else // LinkEditMode.Normal
                    {
                        draggedIndex = i;
                    }
                }
                else if (currentLinkMode == LinkEditMode.Normal && (e.type == EventType.ContextClick || (e.type == EventType.MouseUp && e.button == 1)))
                {
                    if (slotNode.slotType == SlotType.Shooter)
                    {
                        ShowEditShooterPopUp(slotNode);
                        e.Use();
                    }
                }
            }

            bool isBeingDragged = (currentLinkMode == LinkEditMode.Normal && i == draggedIndex);
            bool isDropTarget = (currentLinkMode == LinkEditMode.Normal && i == dropTargetIndex);

            DrawSingleSlotInRect(slotNode, slotRect, isBeingDragged, isDropTarget);
        }

        // Nút Thêm Mới ô (Ẩn đi nếu không ở chế độ Normal để tránh Bug dữ liệu)
        if (currentLinkMode == LinkEditMode.Normal)
        {
            int addBtnIndex = targetData.gridData.slots.Count;
            int addRow = addBtnIndex / columnCount;
            int addCol = addBtnIndex % columnCount;
            Rect addBtnRect = new Rect(
                gridStartRect.x + addCol * (slotWidth + spacing),
                gridStartRect.y + addRow * (slotHeight + spacing),
                slotWidth, slotHeight
            );

            DrawAddButton(addBtnRect, e);
        }

        // Xử lý các Event thả chuột ở luồng Normal
        if (currentLinkMode == LinkEditMode.Normal)
        {
            if (e.type == EventType.MouseDrag && draggedIndex >= 0)
            {
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                if (draggedIndex >= 0)
                {
                    if (dropTargetIndex >= 0 && draggedIndex != dropTargetIndex)
                    {
                        MoveSlot(draggedIndex, dropTargetIndex);
                        GUIUtility.ExitGUI();
                    }
                    draggedIndex = -1;
                    e.Use();
                    Repaint();
                }
            }
        }
    }

    private void DrawSingleSlotInRect(GridSlotNode node, Rect rect, bool isDragged, bool isDropTarget)
    {
        bool isShooter = (node.slotType == SlotType.Shooter && node.shooter != null);
        int currentGroupId = isShooter ? node.shooter.linkGroupId : 0;

        // --- BƯỚC 1: ĐỒNG BỘ HIỂN THỊ VIỀN VÀ NỀN THEO TRẠNG THÁI LIÊN KẾT (THAY CHO GUI.COLOR TĨNH) ---
        if (currentLinkMode == LinkEditMode.Linking && temporaryLinkGroup.Contains(node))
        {
            // Nền vàng sáng nhạt khi click chọn tạm thời để chuẩn bị nối dây
            Handles.DrawSolidRectangleWithOutline(rect, new Color(1f, 0.92f, 0.016f, 0.15f), Color.yellow);
        }
        else if (isShooter && currentGroupId > 0)
        {
            // Tự động nhuộm màu viền bao quanh tương ứng với mã ID nhóm liên kết
            Color outlineColor = GetColorFromGroupId(currentGroupId);
            Handles.DrawSolidRectangleWithOutline(rect, new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.08f), outlineColor);

            // Vẽ thẻ chỉ thị màu nhỏ ở góc trên bên trái ô
            Rect indicatorRect = new Rect(rect.x + 4, rect.y + 4, 12, 12);
            EditorGUI.DrawRect(indicatorRect, outlineColor);
        }
        else
        {
            if (isDragged) GUI.color = new Color(1, 1, 1, 0.5f);
            else if (isDropTarget) GUI.color = new Color(0.5f, 1f, 0.5f, 1f);
            else GUI.color = Color.white;
        }

        GUILayout.BeginArea(rect, EditorStyles.helpBox);

        GUIStyle centerMini = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };

        string slotLabel = $"Ô: {node.slotType}";
        if (currentGroupId > 0) slotLabel += $" (🔗Nhóm {currentGroupId})";
        GUILayout.Label(slotLabel, centerMini);

        if (isShooter)
        {
            int colorCount = node.shooter.colorsHex.Count;
            if (colorCount > 0)
            {
                // SỬA LỖI: Sử dụng phương thức GetRect nhận 2 tham số float (width, height) 
                // thay vì truyền trực tiếp GUILayout.ExpandWidth để tránh lỗi gạch đỏ.
                Rect totalColorRect = GUILayoutUtility.GetRect(rect.width - 16f, 14f);
                float subWidth = totalColorRect.width / colorCount;

                for (int i = 0; i < colorCount; i++)
                {
                    string safeHex = node.shooter.colorsHex[i];
                    if (!safeHex.StartsWith("#")) safeHex = "#" + safeHex;

                    if (ColorUtility.TryParseHtmlString(safeHex, out Color sc))
                    {
                        Rect subColorRect = new Rect(totalColorRect.x + (i * subWidth), totalColorRect.y, subWidth, totalColorRect.height);
                        EditorGUI.DrawRect(subColorRect, sc);
                    }
                }
            }
            else
            {
                GUILayout.Box("Chưa chọn màu", GUILayout.ExpandWidth(true), GUILayout.Height(14));
            }

            GUILayout.Space(2);
            GUILayout.Label($"Đạn chung: {node.shooter.ammoCount}", centerMini);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.EnumPopup(node.shooter.type);
            EditorGUI.EndDisabledGroup();

            if (currentLinkMode == LinkEditMode.Normal)
            {
                GUIStyle hintStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 9, alignment = TextAnchor.MiddleCenter };
                GUILayout.Label("(Chuột phải để sửa)", hintStyle);
            }
            else
            {
                GUIStyle alertClickStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 10, alignment = TextAnchor.MiddleCenter };
                alertClickStyle.normal.textColor = currentLinkMode == LinkEditMode.Linking ? Color.yellow : Color.red;
                GUILayout.Label(currentLinkMode == LinkEditMode.Linking ? "[CLICK ĐỂ CHỌN]" : "[CLICK ĐỂ HỦY]", alertClickStyle);
            }
        }
        else
        {
            GUIStyle idStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
            GUILayout.Space(10);
            GUILayout.Label($"ID: {node.id}", idStyle);
        }

        GUILayout.FlexibleSpace();

        // Nút Xóa ô cờ (Chỉ hiện khi ở chế độ Normal thường)
        if (currentLinkMode == LinkEditMode.Normal)
        {
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Xóa", GUILayout.ExpandWidth(true), GUILayout.Height(18)))
            {
                Undo.RecordObject(targetData, "Delete Grid Slot");
                targetData.gridData.slots.Remove(node);
                RecalculateRemainingColors();
                EditorUtility.SetDirty(targetData);
                GUIUtility.ExitGUI();
            }
            GUI.backgroundColor = Color.white;
        }

        GUILayout.EndArea();
        GUI.color = Color.white; // Khôi phục cấu hình màu mặc định
    }

    private void DrawAddButton(Rect addBtnRect, Event e)
    {
        GUI.backgroundColor = new Color(0f, 0.4f, 0f);
        if (GUI.Button(addBtnRect, "＋\nTạo Ô Mới"))
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Ụ súng (Shooter)"), false, () => AddShooterWindow());
            menu.AddItem(new GUIContent("Cặp Khóa & Chìa (Key - Lock)"), false, () => AddKeyLockPair());
            menu.ShowAsContext();
            e.Use();
        }
        GUI.backgroundColor = Color.white;
    }

    private void HandleDragAndDropEvents()
    {
        // Event e = Event.current;
        // int columns = targetData != null && targetData.gridData != null ? targetData.gridData.columnCount : 3;
        // float viewWidth = position.width - 40f;
        // float slotSize = Mathf.Min(120f, viewWidth / columns);

        // switch (e.type)
        // {
        //     case EventType.MouseDown:
        //         if (e.button == 0 && targetData != null && targetData.gridData != null)
        //         {
        //             Vector2 mousePos = e.mousePosition;
        //             for (int i = 0; i < targetData.gridData.slots.Count; i++)
        //             {
        //                 int r = i / columns;
        //                 int c = i % columns;
        //                 Rect targetRect = new Rect(20f + c * (slotSize + 4), 135f + r * (slotSize + 5) - scrollPos.y, slotSize, slotSize);

        //                 if (targetRect.Contains(mousePos) && mousePos.y < targetRect.yMax - 18)
        //                 {
        //                     draggingIndex = i;
        //                     dragMouseOffset = mousePos - targetRect.position;
        //                     e.Use();
        //                     break;
        //                 }
        //             }
        //         }
        //         break;

        //     case EventType.MouseDrag:
        //         if (draggingIndex >= 0)
        //         {
        //             Repaint();
        //             e.Use();
        //         }
        //         break;

        //     case EventType.MouseUp:
        //         if (draggingIndex >= 0)
        //         {
        //             if (hoverInsertIndex >= 0 && hoverInsertIndex != draggingIndex && hoverInsertIndex != draggingIndex + 1)
        //             {
        //                 Undo.RecordObject(targetData, "Reorder Grid Slot");
        //                 GridSlotNode draggedItem = targetData.gridData.slots[draggingIndex];
        //                 targetData.gridData.slots.Insert(hoverInsertIndex, draggedItem);

        //                 if (draggingIndex >= hoverInsertIndex)
        //                     targetData.gridData.slots.RemoveAt(draggingIndex + 1);
        //                 else
        //                     targetData.gridData.slots.RemoveAt(draggingIndex);

        //                 EditorUtility.SetDirty(targetData);
        //             }
        //             draggingIndex = -1;
        //             hoverInsertIndex = -1;
        //             e.Use();
        //             Repaint();
        //         }
        //         break;
        // }
    }

    private void DrawFloatingDragVoxel()
    {
        int columns = targetData.gridData.columnCount;
        float viewWidth = position.width - 40f;
        float slotSize = Mathf.Min(120f, viewWidth / columns);

        Vector2 mousePos = Event.current.mousePosition;
        Rect floatingRect = new Rect(mousePos.x - dragMouseOffset.x, mousePos.y - dragMouseOffset.y, slotSize, slotSize);

        GridSlotNode node = targetData.gridData.slots[draggingIndex];

        GUI.color = new Color(1, 1, 1, 0.7f);
        GUILayout.BeginArea(floatingRect, EditorStyles.helpBox);

        GUIStyle centerMini = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
        GUILayout.Label($"Đang di dời\n{node.slotType}", centerMini);

        if (node.slotType == SlotType.Shooter && node.shooter != null && node.shooter.colorsHex != null)
        {
            int colorCount = node.shooter.colorsHex.Count;
            if (colorCount > 0)
            {
                // SỬA LỖI TƯƠNG TỰ: Thay đổi thành GetRect với tham số kích thước rõ ràng
                Rect totalColorRect = GUILayoutUtility.GetRect(slotSize - 16f, 14f);
                float subWidth = totalColorRect.width / colorCount;

                for (int i = 0; i < colorCount; i++)
                {
                    string safeHex = node.shooter.colorsHex[i];
                    if (!safeHex.StartsWith("#")) safeHex = "#" + safeHex;

                    if (ColorUtility.TryParseHtmlString(safeHex, out Color sc))
                    {
                        Rect subColorRect = new Rect(totalColorRect.x + (i * subWidth), totalColorRect.y, subWidth, totalColorRect.height);
                        EditorGUI.DrawRect(subColorRect, sc);
                    }
                }
            }
            else
            {
                GUILayout.Box("Chưa chọn màu", GUILayout.ExpandWidth(true), GUILayout.Height(14));
            }

            GUILayout.Space(2);
            GUILayout.Label($"Đạn: {node.shooter.ammoCount}", centerMini);
        }
        else
        {
            GUIStyle idStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 12 };
            GUILayout.Label($"ID: {node.id}", idStyle);
        }

        GUILayout.EndArea();
        GUI.color = Color.white;
    }

    private void AddKeyLockPair()
    {
        Undo.RecordObject(targetData, "Add Key-Lock Pair");
        int pairId = uniqueIdCounter++;
        GridSlotNode keyNode = new GridSlotNode { slotType = SlotType.Key, id = pairId };
        GridSlotNode lockNode = new GridSlotNode { slotType = SlotType.Lock, id = pairId };

        targetData.gridData.slots.Add(keyNode);
        targetData.gridData.slots.Add(lockNode);

        EditorUtility.SetDirty(targetData);
        Repaint();
    }

    private void AddShooterWindow()
    {
        ShooterConfigPopUp.Init(null, remainingColors, availableHexColors, (finalConfig) =>
        {
            Undo.RecordObject(targetData, "Add Shooter Slot");
            GridSlotNode newShooter = new GridSlotNode { slotType = SlotType.Shooter, shooter = finalConfig };
            targetData.gridData.slots.Add(newShooter);

            RecalculateRemainingColors();
            EditorUtility.SetDirty(targetData);
            Repaint();
        });
    }

    private void ShowEditShooterPopUp(GridSlotNode node)
    {
        ShooterConfigPopUp.Init(node.shooter, remainingColors, availableHexColors, (updatedConfig) =>
        {
            Undo.RecordObject(targetData, "Edit Shooter Data");
            node.shooter = updatedConfig;
            RecalculateRemainingColors();
            EditorUtility.SetDirty(targetData);
            Repaint();
        });
    }

    private void LoadFromScriptableObject()
    {
        if (targetData == null || targetData.gridData == null) return;

        ParseVoxelJson();
        RecalculateRemainingColors();

        // Quét tìm mốc ID nhóm lớn nhất đã lưu để tránh gán trùng ID khi tạo liên kết mới
        maxLinkGroupIdCounter = 1;
        foreach (var slot in targetData.gridData.slots)
        {
            if (slot.slotType == SlotType.Shooter && slot.shooter != null)
            {
                if (slot.shooter.linkGroupId >= maxLinkGroupIdCounter)
                {
                    maxLinkGroupIdCounter = slot.shooter.linkGroupId + 1;
                }
            }
        }

        Debug.Log("[GridEditor] Đã nạp cấu hình và đồng bộ dữ liệu hoàn chỉnh.");
    }

    private void SaveToScriptableObject()
    {
        if (targetData == null || targetData.gridData == null) return;
        EditorUtility.SetDirty(targetData);
        AssetDatabase.SaveAssets();
        Debug.Log($"[GridEditor] Đã lưu thông tin cấu hình vào file: {targetData.name}");
    }

    private void MoveSlot(int fromIndex, int toIndex)
    {
        Undo.RecordObject(targetData, "Reorder Grid Slot");
        var item = targetData.gridData.slots[fromIndex];
        targetData.gridData.slots.RemoveAt(fromIndex);
        targetData.gridData.slots.Insert(toIndex, item);
        EditorUtility.SetDirty(targetData);
    }

    // --- ALGORITHM FOR GROUPING (PRESETS FOR QUICK CONTRAST COLORS) ---
    private Color GetColorFromGroupId(int id)
    {
        Color[] presetColors = new Color[] {
            Color.cyan,
            new Color(1f, 0.5f, 0f),    // Cam tươi
            Color.magenta,
            new Color(0.5f, 0f, 1f),   // Tím neon
            new Color(0f, 1f, 0.5f),   // Xanh ngọc
            new Color(1f, 0.2f, 0.6f)  // Hồng cánh sen
        };
        return presetColors[(id - 1) % presetColors.Length];
    }

    private void ExecuteGenerateBoard(LayoutGenerationType type, int maxAmmo)
    {
        Undo.RecordObject(targetData, "Auto Generate Board Fast");
        targetData.gridData.slots.Clear();
        uniqueIdCounter = 1;

        Dictionary<string, int> temporaryWarehouse = new Dictionary<string, int>();
        List<string> activeColors = new List<string>();

        foreach (var kvp in baseVoxelColors)
        {
            if (kvp.Value > 0)
            {
                temporaryWarehouse[kvp.Key] = kvp.Value;
                activeColors.Add(kvp.Key);
            }
        }

        int sequentialColorIndex = 0;

        while (activeColors.Count > 0)
        {
            string chosenColor = "";

            if (type == LayoutGenerationType.Random)
            {
                int randIdx = UnityEngine.Random.Range(0, activeColors.Count);
                chosenColor = activeColors[randIdx];
            }
            else
            {
                if (sequentialColorIndex >= activeColors.Count) sequentialColorIndex = 0;
                chosenColor = activeColors[sequentialColorIndex];
            }

            int currentStock = temporaryWarehouse[chosenColor];
            int allocatedAmmo = Mathf.Min(maxAmmo, currentStock);

            ShooterConfig shooterConfig = new ShooterConfig()
            {
                type = ShooterType.Normal,
                freezeTurns = 0,
                ammoCount = allocatedAmmo,
                colorsHex = new List<string> { chosenColor },
                linkGroupId = 0 // Mặc định sinh nhanh không nhóm
            };

            GridSlotNode newSlot = new GridSlotNode()
            {
                slotType = SlotType.Shooter,
                id = 0,
                shooter = shooterConfig
            };

            targetData.gridData.slots.Add(newSlot);
            temporaryWarehouse[chosenColor] -= allocatedAmmo;

            if (temporaryWarehouse[chosenColor] <= 0)
            {
                activeColors.Remove(chosenColor);
                if (type == LayoutGenerationType.Sequential && sequentialColorIndex > 0)
                {
                    sequentialColorIndex--;
                }
            }

            if (type == LayoutGenerationType.Sequential)
            {
                sequentialColorIndex++;
            }
        }

        RecalculateRemainingColors();
        EditorUtility.SetDirty(targetData);
        Repaint();

        Debug.Log($"[GridEditor] Đã tạo board hoàn chỉnh qua bảng Popup! Số ụ súng tạo ra: {targetData.gridData.slots.Count} ô.");
    }
}