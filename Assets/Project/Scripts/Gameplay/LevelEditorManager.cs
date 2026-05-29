using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace CubeLand.Gameplay
{
    public class LevelEditorManager : MonoBehaviour
    {
        [Header("Cài đặt cơ bản")]
        public TextAsset rawJsonFile;
        public GameObject voxelPrefab;
        public LevelMark levelMark = LevelMark.Easy;
        public List<Color> palette = new List<Color>();
        [HideInInspector] public Color selectedColor = Color.white;

        // Quản lý trạng thái Editor
        public enum EditMode { None, Paint, Select, Fill, SelectFullLayer }
        [HideInInspector] public EditMode currentMode = EditMode.None;

        // Cấu trúc Node lưu trữ dữ liệu từng khối Voxel (Giữ nguyên định nghĩa của bạn ở đây)

        [HideInInspector] public Dictionary<Vector3Int, VoxelNode> voxelMap = new Dictionary<Vector3Int, VoxelNode>();
        [HideInInspector] public List<VoxelNode> selectedVoxels = new List<VoxelNode>();

        [HideInInspector] public int currentVisibleLayer = 0;
        [HideInInspector] public int maxLayerDepth = 0;
        [HideInInspector] public Vector3Int gridSize;

        private static readonly Vector3Int[] Dirs = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down, new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };
        private static readonly Vector3Int[] Dirs26 = Generate26Directions();
        private static Vector3Int[] Generate26Directions()
        {
            List<Vector3Int> list = new List<Vector3Int>();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        if (x == 0 && y == 0 && z == 0) continue;
                        list.Add(new Vector3Int(x, y, z));
                    }
                }
            }
            return list.ToArray();
        }

        public void ClearAll()
        {
            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in transform)
            {
                children.Add(child.gameObject);
            }
            foreach (GameObject child in children)
            {
                DestroyImmediate(child);
            }

            voxelMap.Clear();
            selectedVoxels.Clear();
            currentVisibleLayer = 0;
            maxLayerDepth = 0;

            Debug.Log("[LevelEditor] Đã xóa sạch toàn bộ các Voxel.");
        }

        public void LoadRawFile()
        {
            if (rawJsonFile == null || voxelPrefab == null) return;
            ClearAll();

            VoxJsonWrapper data = JsonUtility.FromJson<VoxJsonWrapper>(rawJsonFile.text);
            gridSize = new Vector3Int(data.size.x, data.size.y, data.size.z);

            foreach (var v in data.voxels)
            {
                Vector3Int pos = new Vector3Int(v.x, v.y, v.z);
                CreateVoxel(pos, HexToColor(v.color), 0);
            }
            Debug.Log($"[LevelEditor] Đã tải {data.voxels.Count} voxel từ file raw.");
            CalculateLayers();
        }

        public void Solidify()
        {
            if (voxelMap.Count == 0) return;

            int gridX = gridSize.x + 3;
            int gridY = gridSize.y + 3;
            int gridZ = gridSize.z + 3;

            byte[] grid = new byte[gridX * gridY * gridZ];

            int GetIndex(int x, int y, int z) => x + (y * gridX) + (z * gridX * gridY);

            foreach (var kvp in voxelMap)
            {
                int ix = kvp.Key.x + 1;
                int iy = kvp.Key.y + 1;
                int iz = kvp.Key.z + 1;

                if (ix >= 0 && ix < gridX && iy >= 0 && iy < gridY && iz >= 0 && iz < gridZ)
                {
                    grid[GetIndex(ix, iy, iz)] = 1;
                }
            }

            Queue<Vector3Int> queue = new Queue<Vector3Int>();
            queue.Enqueue(Vector3Int.zero);
            grid[GetIndex(0, 0, 0)] = 2;

            while (queue.Count > 0)
            {
                Vector3Int curr = queue.Dequeue();
                foreach (var dir in Dirs)
                {
                    Vector3Int n = curr + dir;
                    if (n.x >= 0 && n.x < gridX && n.y >= 0 && n.y < gridY && n.z >= 0 && n.z < gridZ)
                    {
                        int idx = GetIndex(n.x, n.y, n.z);
                        if (grid[idx] == 0) { grid[idx] = 2; queue.Enqueue(n); }
                    }
                }
            }

            for (int x = 1; x <= gridSize.x; x++)
            {
                for (int y = 1; y <= gridSize.y; y++)
                {
                    for (int z = 1; z <= gridSize.z; z++)
                    {
                        if (grid[GetIndex(x, y, z)] == 0)
                        {
                            Vector3Int pos = new Vector3Int(x - 1, y - 1, z - 1);
                            if (!voxelMap.ContainsKey(pos)) CreateVoxel(pos, Color.white, -1);
                        }
                    }
                }
            }
            CalculateLayers();
        }

        private void CalculateLayers()
        {
            Queue<VoxelNode> queue = new Queue<VoxelNode>();

            foreach (var kvp in voxelMap)
            {
                kvp.Value.layerDepth = -1;
                bool isExposed = false;
                foreach (var dir in Dirs)
                {
                    if (!voxelMap.ContainsKey(kvp.Key + dir)) { isExposed = true; break; }
                }

                if (isExposed)
                {
                    kvp.Value.layerDepth = 0;
                    queue.Enqueue(kvp.Value);
                }
            }

            maxLayerDepth = 0;
            while (queue.Count > 0)
            {
                VoxelNode curr = queue.Dequeue();
                foreach (var dir in Dirs)
                {
                    if (voxelMap.TryGetValue(curr.pos + dir, out var neighbor))
                    {
                        if (neighbor.layerDepth == -1)
                        {
                            neighbor.layerDepth = curr.layerDepth + 1;
                            if (neighbor.layerDepth > maxLayerDepth) maxLayerDepth = neighbor.layerDepth;
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }
            currentVisibleLayer = 0;
            UpdateVisibility();
        }

        public void UpdateVisibility()
        {
            int activeCount = 0;
            foreach (var node in voxelMap.Values)
            {
                bool shouldBeActive = node.layerDepth >= currentVisibleLayer;
                node.obj.SetActive(shouldBeActive);
                if (shouldBeActive) activeCount++;
            }
        }

        private void CreateVoxel(Vector3Int pos, Color color, int depth)
        {
            GameObject go = Instantiate(voxelPrefab, pos, Quaternion.identity, transform);
            go.name = $"Voxel_{pos.x}_{pos.y}_{pos.z}";
            Renderer rend = go.GetComponent<Renderer>();

            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Color", color);
            rend.SetPropertyBlock(mpb);

            voxelMap[pos] = new VoxelNode
            {
                obj = go,
                layerDepth = depth,
                pos = pos,
                renderer = rend,
                currentColor = color
            };
        }

        public void ChangeVoxelColor(VoxelNode node, Color color)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Color", color);
            node.renderer.SetPropertyBlock(mpb);
            node.currentColor = color;
        }

        public void LoadLevelFromPath(string absolutePath)
        {
            if (!File.Exists(absolutePath)) return;

            ClearAll();
            string jsonText = File.ReadAllText(absolutePath);
            VoxJsonWrapper data = JsonConvert.DeserializeObject<VoxJsonWrapper>(jsonText);

            gridSize = new Vector3Int(data.size.x, data.size.y, data.size.z);

            foreach (var v in data.voxels)
            {
                Vector3Int pos = new Vector3Int(v.x, v.y, v.z);
                CreateVoxel(pos, HexToColor(v.color), 0);
            }

            CalculateLayers();
            Debug.Log($"[LevelEditor] Đã load file thành công: {Path.GetFileName(absolutePath)}");
        }

        public List<VoxelNode> GetFloodFillNodes(Vector3Int startPos, Color targetColor)
        {
            List<VoxelNode> nodesToFill = new List<VoxelNode>();

            if (!voxelMap.TryGetValue(startPos, out var startNode)) return nodesToFill;

            int targetLayer = startNode.layerDepth;
            Queue<Vector3Int> queue = new Queue<Vector3Int>();
            HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

            queue.Enqueue(startPos);
            visited.Add(startPos);

            while (queue.Count > 0)
            {
                Vector3Int currentPos = queue.Dequeue();

                if (voxelMap.TryGetValue(currentPos, out var currentNode))
                {
                    if (currentNode.layerDepth == targetLayer && currentNode.currentColor == targetColor)
                    {
                        nodesToFill.Add(currentNode);

                        foreach (var dir in Dirs26)
                        {
                            Vector3Int neighborPos = currentPos + dir;
                            if (!visited.Contains(neighborPos))
                            {
                                visited.Add(neighborPos);
                                queue.Enqueue(neighborPos);
                            }
                        }
                    }
                }
            }
            return nodesToFill;
        }

        public void SaveLevelToPath(string absolutePath)
        {
            if (voxelMap.Count == 0) return;

            Dictionary<string, int> colorStats = new Dictionary<string, int>();

            foreach (var node in voxelMap.Values)
            {
                string hex = ColorToHex(node.currentColor);
                if (colorStats.ContainsKey(hex)) colorStats[hex]++;
                else colorStats[hex] = 1;
            }

            VoxJsonWrapper outputData = new VoxJsonWrapper
            {
                size = new VolumeSize { x = gridSize.x, y = gridSize.y, z = gridSize.z },
                voxels = new List<VoxelJsonData>(voxelMap.Count),
                colorCounts = colorStats,
                mark = levelMark,
            };

            foreach (var kvp in voxelMap)
            {
                outputData.voxels.Add(new VoxelJsonData
                {
                    x = kvp.Key.x,
                    y = kvp.Key.y,
                    z = kvp.Key.z,
                    color = ColorToHex(kvp.Value.currentColor)
                });
            }

            string jsonOutput = JsonConvert.SerializeObject(outputData, Formatting.Indented);
            File.WriteAllText(absolutePath, jsonOutput);
            Debug.Log($"[LevelEditor] Đã lưu file thành công: {Path.GetFileName(absolutePath)}");
        }

        private Color HexToColor(string hex)
        {
            if (!hex.StartsWith("#")) hex = "#" + hex;
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }

        private string ColorToHex(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }

        public int GetTotalVoxelCount()
        {
            return voxelMap.Count;
        }

        public int GetActiveVoxelCount()
        {
            int count = 0;
            foreach (var node in voxelMap.Values)
            {
                if (node.obj.activeInHierarchy) count++;
            }
            return count;
        }

        public Dictionary<string, int> colorStatsCache = new Dictionary<string, int>();

        public void RefreshColorStatistics()
        {
            colorStatsCache.Clear();
            foreach (var v in voxelMap.Values)
            {
                string hex = ColorToHex(v.currentColor);
                if (colorStatsCache.ContainsKey(hex)) colorStatsCache[hex]++;
                else colorStatsCache[hex] = 1;
            }
            Debug.Log("[LevelEditor] Thống kê màu đã được cập nhật.");
        }

        // ==========================================
        // --- HỆ THỐNG CUSTOM UNDO ---
        // ==========================================
        public class VoxelAction
        {
            public Dictionary<VoxelNode, Color> oldColors = new Dictionary<VoxelNode, Color>();
        }

        [HideInInspector] public Stack<VoxelAction> undoStack = new Stack<VoxelAction>();
        private VoxelAction currentAction;

        public void BeginAction()
        {
            currentAction = new VoxelAction();
        }

        public void PaintVoxelWithUndo(VoxelNode node, Color newColor)
        {
            if (node.currentColor == newColor) return;

            if (currentAction != null && !currentAction.oldColors.ContainsKey(node))
            {
                currentAction.oldColors.Add(node, node.currentColor);
            }

            ChangeVoxelColor(node, newColor);
        }

        public void EndAction()
        {
            if (currentAction != null && currentAction.oldColors.Count > 0)
            {
                undoStack.Push(currentAction);
            }
            currentAction = null;
        }

        public void PerformCustomUndo()
        {
            if (undoStack.Count == 0)
            {
                Debug.Log("[Undo] Không có thao tác màu nào để quay lại.");
                return;
            }

            VoxelAction lastAction = undoStack.Pop();
            foreach (var kvp in lastAction.oldColors)
            {
                ChangeVoxelColor(kvp.Key, kvp.Value);
            }

            Debug.Log($"[Undo] Đã lùi lại {lastAction.oldColors.Count} khối.");
        }

        public void ResetPaletteToDefaults()
        {
            palette.Clear();
            InitializePaletteFromConstants();
        }

        public void InitializePaletteFromConstants()
        {
            palette.Clear();
            foreach (var hex in Constant.colorList)
            {
                palette.Add(HexToColor(hex));
            }
        }
    }
}