#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Buffers.Binary;
using UnityEditor;
using UnityEngine;
using CubeLand.Gameplay;

public class VoxToJsonEditorWindow : EditorWindow
{
    private string _voxFilePath = "";
    private bool _beautifyJson = true;

    [MenuItem("Tools/Vox to JSON Converter")]
    public static void ShowWindow()
    {
        var window = GetWindow<VoxToJsonEditorWindow>("Vox to JSON");
        window.minSize = new Vector2(400, 180);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Vox to JSON Converter", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Giao diện chọn File đầu vào
        EditorGUILayout.BeginHorizontal();
        _voxFilePath = EditorGUILayout.TextField("Source .vox File", _voxFilePath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFilePanel("Select MagicaVoxel File", "Assets", "vox");
            if (!string.IsNullOrEmpty(path))
            {
                // Convert sang relative path nếu nằm trong Project để tiện hiển thị
                if (path.StartsWith(Application.dataPath))
                {
                    _voxFilePath = "Assets" + path.Substring(Application.dataPath.Length);
                }
                else
                {
                    _voxFilePath = path;
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        _beautifyJson = EditorGUILayout.Toggle("Beautify JSON (Indent)", _beautifyJson);
        EditorGUILayout.Space(20);

        // Nút kích hoạt Export
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_voxFilePath));
        if (GUILayout.Button("Convert and Export JSON", GUILayout.Height(40)))
        {
            ProcessConversion();
        }
        EditorGUI.EndDisabledGroup();
    }

    private void ProcessConversion()
    {
        // Chuẩn hóa đường dẫn tuyệt đối để System.IO đọc được
        string fullInputPath = _voxFilePath.StartsWith("Assets")
            ? Path.Combine(Directory.GetParent(Application.dataPath).FullName, _voxFilePath)
            : _voxFilePath;

        if (!File.Exists(fullInputPath))
        {
            EditorUtility.DisplayDialog("Error", "Không tìm thấy file .vox mục tiêu!", "OK");
            return;
        }

        string defaultTargetName = Path.GetFileNameWithoutExtension(fullInputPath) + ".json";
        string savePath = EditorUtility.SaveFilePanel("Save Exported JSON", "Assets", defaultTargetName, "json");

        if (string.IsNullOrEmpty(savePath)) return;

        try
        {
            EditorUtility.DisplayProgressBar("Processing", "Parsing .vox binary data...", 0.3f);

            VoxelData resultData = ParseVoxFile(fullInputPath);

            EditorUtility.DisplayProgressBar("Processing", "Serializing to JSON...", 0.7f);

            string jsonString = JsonUtility.ToJson(resultData, _beautifyJson);
            File.WriteAllText(savePath, jsonString);

            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();

            EditorUtility.DisplayDialog("Success", $"Đã xuất JSON thành công tại:\n{savePath}", "OK");
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[Vox2Json] Gặp lỗi khi chuyển đổi: {ex.Message}\n{ex.StackTrace}");
            EditorUtility.DisplayDialog("Failure", $"Thất bại: {ex.Message}", "OK");
        }
    }

    private VoxelData ParseVoxFile(string path)
    {
        byte[] fileBytes = File.ReadAllBytes(path);
        ReadOnlySpan<byte> span = fileBytes;

        // 1. Kiểm tra định dạng RIFF "VOX "
        if (Encoding.ASCII.GetString(span.Slice(0, 4)) != "VOX ")
            throw new InvalidDataException("Đây không phải định dạng file .vox chuẩn.");

        int offset = 20; // Bỏ qua Header (8 bytes) và cấu trúc bao MAIN chunk (12 bytes)

        int origSizeX = 0, origSizeY = 0, origSizeZ = 0;
        List<byte[]> rawVoxels = new List<byte[]>();
        string[] hexPalette = new string[256];
        bool hasCustomPalette = false;

        // 2. Phân tích luồng Binary sử dụng Span dịch offset liên tục
        while (offset < span.Length)
        {
            // Tránh tràn mảng khi đọc chunk header kế tiếp
            if (offset + 12 > span.Length) break;

            string chunkId = Encoding.ASCII.GetString(span.Slice(offset, 4));
            int chunkBytes = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset + 4, 4));

            offset += 12; // Nhảy qua Header của chunk hiện tại

            if (chunkId == "SIZE")
            {
                origSizeX = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset, 4));
                origSizeY = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset + 4, 4));
                origSizeZ = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset + 8, 4));
            }
            else if (chunkId == "XYZI")
            {
                int numVoxels = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset, 4));
                int dataStart = offset + 4;

                for (int i = 0; i < numVoxels; i++)
                {
                    // Trích xuất mảng 4 byte [X, Y, Z, I] mà không tạo bản sao heap nặng nề
                    rawVoxels.Add(span.Slice(dataStart + (i * 4), 4).ToArray());
                }
            }
            else if (chunkId == "RGBA")
            {
                hasCustomPalette = true;
                for (int i = 0; i < 256; i++)
                {
                    int pOffset = offset + (i * 4);
                    hexPalette[i] = $"#{span[pOffset]:X2}{span[pOffset + 1]:X2}{span[pOffset + 2]:X2}{span[pOffset + 3]:X2}";
                }
            }

            offset += chunkBytes; // Đẩy con trỏ nhảy qua vùng dữ liệu của chunk vừa rồi
        }

        if (!hasCustomPalette)
        {
            Array.Fill(hexPalette, "#FFFFFFFF");
        }

        // 3. Đổ dữ liệu vào Wrapper và Map tọa độ chính xác sang hệ Y-up của Unity
        VoxelData wrapper = new VoxelData
        {
            size = new VolumeSize { x = origSizeX, y = origSizeZ, z = origSizeY },
            voxels = new List<VoxelJsonData>(rawVoxels.Count)
        };

        foreach (var v in rawVoxels)
        {
            int colorIndex = v[3] - 1; // Hệ màu MagicaVoxel chạy từ 1-256
            string colorHex = (colorIndex >= 0 && colorIndex < 256) ? hexPalette[colorIndex] : "#FFFFFFFF";

            wrapper.voxels.Add(new VoxelJsonData
            {
                x = v[0],
                y = v[2], // Đổi trục Z-up gốc thành Y-up của Unity
                z = v[1], // Đổi trục Y gốc thành Z của Unity
                color = colorHex,
                type = VoxelType.Normal // Mặc định gán tất cả voxel là Normal, có thể mở rộng sau này nếu cần
            });
        }

        return wrapper;
    }
}
#endif