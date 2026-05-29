using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CubeLand.Gameplay
{
    [Serializable]
    public enum LevelMark
    {
        Easy,
        Medium,
        Hard,
        SuperHard
    }

    [Serializable]
    public struct VoxelJsonData
    {
        public int x;
        public int y;
        public int z;
        public string color;
    }

    [Serializable]
    public struct VolumeSize
    {
        public int x;
        public int y;
        public int z;
    }

    [Serializable]
    public struct ColorCount
    {
        public string color;
        public int count;
    }


    [Serializable]
    public class VoxJsonWrapper
    {
        public VolumeSize size;
        public List<VoxelJsonData> voxels;
        public Dictionary<string, int> colorCounts; // Tùy chọn: Thống kê số lượng mỗi màu xuất hiện
        public LevelMark mark; // Dùng để đánh dấu mức độ khó của voxel, có thể dùng để điều chỉnh gameplay sau này
        public Dictionary<int, ColorCount> colorsEachLayer; // Dùng để lưu trữ thông tin màu sắc và số lượng voxels của từng layer, hỗ trợ cho việc hiển thị thông tin chi tiết trong editor hoặc gameplay
    }

    public class VoxelNode
    {
        public GameObject obj;
        public int layerDepth;
        public Vector3Int pos;
        public Renderer renderer;
        public Color currentColor; // Cache màu để lưu file nhanh
    }

    public static class Constant
    {
        public static string[] colorList = new string[]
        {
            "#fe65ca",
            "#4aedfe",
            "#854af7",
            "#ffe334",
            "#58e33c",
            "#ff973a",
            "#ffffff",
            "#3c3b45",
            "#57a6ff",
            "#017d00",
            "#e1363d",
            "#4c6eed",
            "#218775",
            "#f7adff",
            "#a1b3fc",
            "#5f3b20",
            "#eed08f",
            "#fca1af",
            "#a23e5f",
            "#afdb8f",
            "#401a7e",
            "#d1505e",
            "#afb5db",
            "#5f6179",
            "#c80780",
            "#fbbc6e",
            "#840718",
            "#69b1b4",
        };
    }
}
