using System;
using System.Collections;
using System.Collections.Generic;
using CubeLand.Gameplay;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelData", menuName = "CubeLand/Level Data")]
public class LevelData : ScriptableObject
{
    public VoxelData voxelData;
    public GridLevelData gridData;
}

[Serializable]
public class GridLevelData
{
    public int columnCount = 5; // Số cột (3, 4, 5...)
    public List<GridSlotNode> slots = new List<GridSlotNode>();
}
