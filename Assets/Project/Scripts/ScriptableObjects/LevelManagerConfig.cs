using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelManagerConfig", menuName = "CubeLand/Level Data Config")]
public class LevelManagerConfig : ScriptableObject
{
    public List<LevelData> levels;
}
