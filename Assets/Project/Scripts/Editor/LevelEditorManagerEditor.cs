using System.Collections.Generic;
using CubeLand.Gameplay;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelEditorManager))]
public class LevelEditorManagerEditor : Editor
{
    private LevelEditorManager manager;
    private Vector2 startMousePos;
    private bool isDragging = false;

    private void OnEnable()
    {
        manager = (LevelEditorManager)target;
    }

    public override void OnInspectorGUI()
    {
        HandleShortcuts(Event.current);

        EditorGUILayout.HelpBox(
            $"Tổng số Voxel: {manager.GetTotalVoxelCount()}\n" +
            $"Voxel đang hiển thị: {manager.GetActiveVoxelCount()}",
            MessageType.Info
        );

        DrawDefaultInspector();
        GUILayout.Space(10);

        GUILayout.Label("File Management", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("📂 Mở File (Load JSON)"))
        {
            string path = EditorUtility.OpenFilePanel("Chọn file Level JSON", Application.dataPath, "json");
            if (!string.IsNullOrEmpty(path))
            {
                manager.LoadLevelFromPath(path);
                SceneView.RepaintAll();
            }
        }

        if (GUILayout.Button("💾 Lưu File (Save JSON)"))
        {
            string path = EditorUtility.SaveFilePanel("Lưu file Level JSON", Application.dataPath, "new_level_data", "json");
            if (!string.IsNullOrEmpty(path))
            {
                manager.SaveLevelToPath(path);
                if (path.StartsWith(Application.dataPath)) AssetDatabase.Refresh();
            }
        }

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("🗑️ CLEAR ALL"))
        {
            if (EditorUtility.DisplayDialog("Xác nhận xóa", "Bạn có chắc chắn muốn xóa sạch toàn bộ dữ liệu? Hành động này không thể hoàn tác!", "Xóa", "Hủy"))
            {
                Undo.RecordObject(manager, "Clear All");
                manager.ClearAll();
                SceneView.RepaintAll();
            }
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(15);

        // --- 2. BẢNG MÀU (PALETTE) TỰ ĐỘNG NGẮT DÒNG ---
        GUI.backgroundColor = Color.cyan;
        GUILayout.Label("Color Palette", EditorStyles.boldLabel);
        if (GUILayout.Button("Đặt lại Bảng Màu Mặc Định"))
        {
            Undo.RecordObject(manager, "Reset Palette");
            manager.ResetPaletteToDefaults();
            SceneView.RepaintAll();
            Repaint();
        }

        // Trừ đi khoảng 30 pixel để bù trừ cho thanh cuộn (scrollbar) và viền lề của Inspector
        float inspectorWidth = EditorGUIUtility.currentViewWidth - 30f;
        float buttonSize = 40f;
        float spacing = 5f; // Khoảng cách mặc định giữa các nút

        // Tính toán số lượng nút tối đa có thể hiển thị trên 1 hàng
        int itemsPerRow = Mathf.Max(1, Mathf.FloorToInt(inspectorWidth / (buttonSize + spacing)));

        GUILayout.BeginHorizontal();
        for (int i = 0; i < manager.palette.Count; i++)
        {
            // Nếu đã vẽ đủ số nút trên 1 hàng, kết thúc hàng hiện tại và mở hàng ngang mới
            if (i > 0 && i % itemsPerRow == 0)
            {
                GUILayout.EndHorizontal();
                GUILayout.Space(spacing); // Tách một chút khoảng cách theo chiều dọc
                GUILayout.BeginHorizontal();
            }

            Color c = manager.palette[i];
            GUI.backgroundColor = c;
            string btnText = manager.selectedColor == c ? "★" : "";

            if (GUILayout.Button(btnText, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
            {
                manager.selectedColor = c;
                if (manager.selectedVoxels.Count > 0)
                {
                    // --- BỌC UNDO CHO PALETTE ---
                    manager.BeginAction();
                    foreach (var node in manager.selectedVoxels)
                    {
                        manager.PaintVoxelWithUndo(node, c);
                        EditorUtility.SetDirty(node.renderer);
                    }
                    manager.EndAction();
                    SceneView.RepaintAll();
                }
            }
            GUI.backgroundColor = Color.white;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(15);

        GUILayout.Label("Processing Tools", EditorStyles.boldLabel);
        if (GUILayout.Button("Hiện Vỏ Từ File Raw (TextAsset)")) manager.LoadRawFile();
        if (GUILayout.Button("Làm Đặc (Solidify Inside)")) manager.Solidify();

        GUILayout.Space(10);

        GUILayout.Label($"Layer Visibility (Current Hidden: {manager.currentVisibleLayer} / {manager.maxLayerDepth})", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Ẩn Layer (Peel) (Q)") && manager.currentVisibleLayer <= manager.maxLayerDepth)
        {
            Undo.RecordObject(manager, "Peel Layer");
            manager.currentVisibleLayer++;
            manager.UpdateVisibility();
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("Hiện Layer (Unpeel) (E)") && manager.currentVisibleLayer > 0)
        {
            Undo.RecordObject(manager, "Unpeel Layer");
            manager.currentVisibleLayer--;
            manager.UpdateVisibility();
            SceneView.RepaintAll();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUILayout.Label("Interaction Modes (Scene View)", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();

        GUI.backgroundColor = manager.currentMode == LevelEditorManager.EditMode.Select ? Color.cyan : Color.white;
        if (GUILayout.Button("Nút Select (Kéo Vùng) (S)")) manager.currentMode = LevelEditorManager.EditMode.Select;

        GUI.backgroundColor = manager.currentMode == LevelEditorManager.EditMode.Paint ? Color.cyan : Color.white;
        if (GUILayout.Button("Nút Paint (Tô Lẻ) (P)")) manager.currentMode = LevelEditorManager.EditMode.Paint;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUI.backgroundColor = manager.currentMode == LevelEditorManager.EditMode.Fill ? Color.cyan : Color.white;
        if (GUILayout.Button("Nút Fill (Loang) (F)")) manager.currentMode = LevelEditorManager.EditMode.Fill;

        GUI.backgroundColor = manager.currentMode == LevelEditorManager.EditMode.SelectFullLayer ? Color.cyan : Color.white;
        if (GUILayout.Button("Nút Select Full Layer (L)")) manager.currentMode = LevelEditorManager.EditMode.SelectFullLayer;

        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Tô Màu Toàn Bộ Layer Hiện Tại (T)"))
        {
            PaintAllLayer(manager);
        }

        if (GUILayout.Button("Tô Màu sole Layer Hiện Tại (J)"))
        {
            PaintCheckerboard(manager);
        }

        if (manager.selectedVoxels.Count > 0 && GUILayout.Button("Bỏ chọn (Clear Selection)"))
        {
            manager.selectedVoxels.Clear();
            SceneView.RepaintAll();
        }

        GUILayout.Space(10);
        GUILayout.Label("Thống kê màu sắc", EditorStyles.boldLabel);

        if (GUILayout.Button("🔄 Cập nhật thống kê màu"))
        {
            manager.RefreshColorStatistics();
        }

        if (manager.colorStatsCache.Count > 0)
        {
            foreach (var kvp in manager.colorStatsCache)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{kvp.Key}: {kvp.Value} khối", GUILayout.Width(150));

                if (ColorUtility.TryParseHtmlString(kvp.Key, out Color c))
                {
                    GUI.backgroundColor = c;
                    GUILayout.Box("", GUILayout.Width(20), GUILayout.Height(20));
                    GUI.backgroundColor = Color.white;
                }
                GUILayout.EndHorizontal();
            }
        }
        else
        {
            GUILayout.Label("Chưa có dữ liệu. Nhấn nút để quét.");
        }

        if (GUI.changed) EditorUtility.SetDirty(manager);
    }

    private void PaintAllLayer(LevelEditorManager manager)
    {
        // --- BỌC UNDO ---
        manager.BeginAction();
        foreach (var node in manager.voxelMap.Values)
        {
            if (node.layerDepth == manager.currentVisibleLayer && node.obj.activeInHierarchy)
            {
                manager.PaintVoxelWithUndo(node, manager.selectedColor);
                EditorUtility.SetDirty(node.renderer);
            }
        }
        manager.EndAction();
        SceneView.RepaintAll();
    }

    private void PaintCheckerboard(LevelEditorManager manager)
    {
        // --- BỌC UNDO ---
        manager.BeginAction();
        foreach (var kvp in manager.voxelMap)
        {
            Vector3Int pos = kvp.Key;
            var node = kvp.Value;
            if (node.layerDepth == manager.currentVisibleLayer && node.obj.activeInHierarchy)
            {
                int coordinateSum = Mathf.Abs(pos.x + pos.y + pos.z);
                if (coordinateSum % 2 == 0)
                {
                    manager.PaintVoxelWithUndo(node, manager.selectedColor);
                    EditorUtility.SetDirty(node.renderer);
                }
            }
        }
        manager.EndAction();
        SceneView.RepaintAll();
    }

    private void OnSceneGUI()
    {
        Event e = Event.current;

        HandleShortcuts(e);

        if (manager.currentMode == LevelEditorManager.EditMode.None) return;

        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        if (manager.currentMode == LevelEditorManager.EditMode.Paint)
        {
            HandlePainting(e, controlID);
        }
        else if (manager.currentMode == LevelEditorManager.EditMode.Select)
        {
            HandleMarqueeSelection(e, controlID);
        }
        else if (manager.currentMode == LevelEditorManager.EditMode.Fill)
        {
            HandleFill(e, controlID);
        }
        else if (manager.currentMode == LevelEditorManager.EditMode.SelectFullLayer)
        {
            HandleMarqueeSelection(e, controlID, isFull: true);
        }
    }

    private void HandleShortcuts(Event e)
    {
        // Bỏ qua phím tắt nếu người dùng đang gõ chữ vào một ô Text Field nào đó trong Inspector
        if (EditorGUIUtility.editingTextField) return;

        // Chuyển từ KeyUp sang KeyDown để phản hồi tức thì
        if (e.type == EventType.KeyDown)
        {
            // GHI CHÚ: Trong code cũ của bạn có "&& e.capsLock". 
            // Điều đó bắt buộc user phải bật đèn Caps Lock mới dùng được phím tắt. 
            // Tôi đã tạm bỏ nó đi, nếu bạn cố tình làm vậy thì hãy thêm "&& e.capsLock" vào lại nhé.

            bool shortcutTriggered = true; // Biến kiểm tra xem có phím tắt nào được bấm không

            switch (e.keyCode)
            {
                case KeyCode.Space:
                    manager.currentMode = LevelEditorManager.EditMode.None;
                    manager.selectedVoxels.Clear();
                    break;
                case KeyCode.P:
                    manager.currentMode = LevelEditorManager.EditMode.Paint;
                    manager.selectedVoxels.Clear();
                    break;
                case KeyCode.S:
                    manager.currentMode = LevelEditorManager.EditMode.Select;
                    break;
                case KeyCode.F:
                    manager.currentMode = LevelEditorManager.EditMode.Fill;
                    manager.selectedVoxels.Clear();
                    break;
                case KeyCode.L:
                    manager.currentMode = LevelEditorManager.EditMode.SelectFullLayer;
                    manager.selectedVoxels.Clear();
                    break;
                case KeyCode.Q:
                    manager.selectedVoxels.Clear();
                    if (manager.currentVisibleLayer <= manager.maxLayerDepth)
                    {
                        Undo.RecordObject(manager, "Peel Layer");
                        manager.currentVisibleLayer++;
                        manager.UpdateVisibility();
                    }
                    break;
                case KeyCode.E:
                    manager.selectedVoxels.Clear();
                    if (manager.currentVisibleLayer > 0)
                    {
                        Undo.RecordObject(manager, "Unpeel Layer");
                        manager.currentVisibleLayer--;
                        manager.UpdateVisibility();
                    }
                    break;
                case KeyCode.T:
                    manager.selectedVoxels.Clear();
                    PaintAllLayer(manager);
                    break;
                case KeyCode.J:
                    manager.selectedVoxels.Clear();
                    PaintCheckerboard(manager);
                    break;
                case KeyCode.Z:
                    manager.selectedVoxels.Clear();
                    if (e.control || e.command)
                    {
                        manager.PerformCustomUndo();
                    }
                    break;
                default:
                    shortcutTriggered = false; // Phím bấm không thuộc danh sách trên
                    break;
            }

            // Nếu bấm đúng phím tắt, ta chặn event và ép Unity vẽ lại màn hình ngay
            if (shortcutTriggered)
            {
                e.Use();
                SceneView.RepaintAll(); // Cập nhật Scene
                Repaint(); // Cập nhật giao diện Inspector (chuyển màu nút bấm)
            }
        }
    }

    private void HandlePainting(Event e, int controlID)
    {
        // Ghi nhớ Action khi bắt đầu nhấn chuột
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            manager.BeginAction();
        }

        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && !e.alt)
        {
            GUIUtility.hotControl = controlID;
            RaycastHit hit;
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                Vector3Int pos = Vector3Int.RoundToInt(hit.transform.position);
                // Dùng "var" để tránh lỗi namespace Assembly
                if (manager.voxelMap.TryGetValue(pos, out var node))
                {
                    manager.PaintVoxelWithUndo(node, manager.selectedColor);
                    EditorUtility.SetDirty(node.renderer);
                    SceneView.RepaintAll();
                }
            }
            e.Use();
        }
        else if (e.type == EventType.MouseUp && e.button == 0)
        {
            // Kết thúc Action khi nhả chuột
            manager.EndAction();
            GUIUtility.hotControl = 0;
            e.Use();
        }
    }

    private void HandleMarqueeSelection(Event e, int controlID, bool isFull = false)
    {
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            GUIUtility.hotControl = controlID;
            startMousePos = e.mousePosition;
            isDragging = true;
            manager.selectedVoxels.Clear();
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && isDragging)
        {
            e.Use();
        }
        else if (e.type == EventType.MouseUp && e.button == 0 && isDragging)
        {
            isDragging = false;
            GUIUtility.hotControl = 0;

            Rect selectionRect = GetScreenRect(startMousePos, e.mousePosition);
            Camera cam = SceneView.currentDrawingSceneView.camera;

            foreach (var node in manager.voxelMap.Values)
            {
                if (isFull)
                {
                    if (!node.obj.activeInHierarchy)
                        continue;
                }
                else
                {
                    if (!node.obj.activeInHierarchy || node.layerDepth != manager.currentVisibleLayer)
                        continue;
                }

                Vector3 screenPos = cam.WorldToScreenPoint(node.obj.transform.position);
                screenPos.y = cam.pixelHeight - screenPos.y;

                if (selectionRect.Contains(screenPos))
                {
                    manager.selectedVoxels.Add(node);
                }
            }
            e.Use();
            SceneView.RepaintAll();
        }

        if (e.type == EventType.Repaint && isDragging)
        {
            Rect rect = GetScreenRect(startMousePos, e.mousePosition);
            Handles.BeginGUI();
            EditorGUI.DrawRect(rect, new Color(0, 1f, 1f, 0.2f));
            DrawRectOutline(rect, new Color(0, 1f, 1f, 1f));
            Handles.EndGUI();
            SceneView.RepaintAll();
        }

        if (e.type == EventType.Repaint && manager.selectedVoxels.Count > 0)
        {
            Handles.color = Color.cyan;
            foreach (var node in manager.selectedVoxels)
            {
                Handles.DrawWireCube(node.obj.transform.position, Vector3.one * 1.05f);
            }
        }
    }

    private void HandleFill(Event e, int controlID)
    {
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            GUIUtility.hotControl = controlID;
            RaycastHit hit;
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                Vector3Int pos = Vector3Int.RoundToInt(hit.transform.position);

                // Dùng "var" để tránh xung đột Assembly
                if (manager.voxelMap.TryGetValue(pos, out var clickedNode))
                {
                    Color targetColor = clickedNode.currentColor;

                    if (targetColor != manager.selectedColor)
                    {
                        // --- BỌC UNDO CHO FILL ---
                        manager.BeginAction();
                        var nodesToFill = manager.GetFloodFillNodes(pos, targetColor);

                        foreach (var node in nodesToFill)
                        {
                            manager.PaintVoxelWithUndo(node, manager.selectedColor);
                            EditorUtility.SetDirty(node.renderer);
                        }
                        manager.EndAction();
                        SceneView.RepaintAll();
                    }
                }
            }
            e.Use();
        }
        else if (e.type == EventType.MouseUp && e.button == 0)
        {
            GUIUtility.hotControl = 0;
            e.Use();
        }
    }

    private Rect GetScreenRect(Vector2 screenPos1, Vector2 screenPos2)
    {
        return Rect.MinMaxRect(
            Mathf.Min(screenPos1.x, screenPos2.x),
            Mathf.Min(screenPos1.y, screenPos2.y),
            Mathf.Max(screenPos1.x, screenPos2.x),
            Mathf.Max(screenPos1.y, screenPos2.y)
        );
    }

    private void DrawRectOutline(Rect rect, Color color)
    {
        EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, 1), color);
        EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax, rect.width, 1), color);
        EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, 1, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax, rect.yMin, 1, rect.height), color);
    }
}