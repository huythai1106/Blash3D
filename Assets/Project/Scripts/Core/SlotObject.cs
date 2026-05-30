using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CubeLand.Gameplay
{
    public class SlotObject : MonoBehaviour
    {
        public int colIndex;

        private void Awake()
        {
            EventDispatcher.AddListener(Constant.OnBoardInitEvent, OnBoardInit);
            EventDispatcher.AddListener<int>(Constant.OnBoardUpdateEvent, OnBoardUpdate);
        }

        protected virtual void OnDestroy()
        {
            EventDispatcher.RemoveListener(Constant.OnBoardInitEvent, OnBoardInit);
            EventDispatcher.RemoveListener<int>(Constant.OnBoardUpdateEvent, OnBoardUpdate);
        }

        public virtual void OnBoardInit() { }
        public virtual void OnBoardUpdate(int colIndex) { }
    }

}
