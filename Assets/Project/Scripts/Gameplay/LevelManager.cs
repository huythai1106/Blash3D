using UnityEngine;

namespace CubeLand.Gameplay
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [SerializeField] private LevelData currentLevelData;
        [SerializeField] private LevelCreator levelCreator;
        [SerializeField] private Board board;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (currentLevelData != null)
            {
                LoadLevel(currentLevelData);
            }
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        public void LoadLevel(LevelData levelData)
        {
            currentLevelData = levelData;

            // 1. Khởi tạo mô hình Voxel phía trên (Sumo/Cage)
            levelCreator.GenerateVoxelModel(levelData.voxelData);

            // 2. Khởi tạo ma trận bàn cờ và ụ súng phía dưới
            board.InitializeBoard(levelData.gridData);

            GameManager.Instance.ChangeState(GameState.Playing);
        }

        public void CheckWinCondition()
        {
            // Nếu số lượng voxel trên model mục tiêu đã bị bắn hạ hết -> WIN
            if (levelCreator.ActiveVoxelCount <= 0)
            {
                GameManager.Instance.ChangeState(GameState.Win);
            }
        }
    }
}