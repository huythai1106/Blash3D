using UnityEngine;

namespace CubeLand.Gameplay
{
    public enum GameState { Init, Playing, Win, Lose }
    public enum GameInputState { None, Dragging }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.Init;
        private GameInputState currentInputState = GameInputState.None;
        public GameInputState CurrentInputState
        {
            get => currentInputState;
            set
            {
                if (currentInputState != value)
                {
                    currentInputState = value;
                    OnChangeInputState(value);
                }
            }
        }

        public GunTurretConfig gunTurretConfig;
        public LayerMask layerVoxel;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            Physics.IgnoreLayerCollision(6, 6, true);
        }

        private void OnDestroy()
        {
            Instance = null;
            Physics.IgnoreLayerCollision(6, 6, false);
        }

        public void ChangeState(GameState newState)
        {
            CurrentState = newState;
            switch (newState)
            {
                case GameState.Playing:
                    break;
                case GameState.Win:
                    Debug.Log("🎉 LEVEL CLEAR!");
                    break;
                case GameState.Lose:
                    Debug.Log("💥 GAME OVER!");
                    break;
            }
        }

        public void OnChangeInputState(GameInputState newInputState)
        {
        }
    }
}