using UnityEngine;

namespace CubeLand.Gameplay
{
    public enum GameState { Init, Playing, Win, Lose }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.Init;
        public GunTurretConfig gunTurretConfig;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
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
    }
}