using System;
using System.Collections.Generic;
using UnityEngine;

namespace CubeLand.Gameplay
{
    public enum SlotType { Empty, Shooter, Key, Lock }
    public enum ShooterType { Normal, Hidden, Frozen }

    [Serializable]
    public class ShooterConfig
    {
        public ShooterType type = ShooterType.Normal;
        // public string colorHex; // Màu của súng (Lấy từ bảng màu voxel còn lại)
        public List<string> colorsHex = new List<string>();
        public int ammoCount = 15; // Số đạn cấu hình
        public int freezeTurns = 0; // Số lượt để mở khóa súng đóng băng
        public int linkGroupId = 0;
    }

    [Serializable]
    public class GridSlotNode
    {
        public int id; // ID định danh hoặc liên kết Cặp (Key - Lock)
        public SlotType slotType = SlotType.Empty;
        public ShooterConfig shooter = new ShooterConfig();
    }
}