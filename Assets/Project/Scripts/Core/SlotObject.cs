using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CubeLand.Gameplay
{
    public class SlotObject : MonoBehaviour
    {
        public SlotType slotType;
        public int colIndex;
        public Cell cell;
        public Board board;

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

        public void Setup(Cell cell, Board board, SlotType type, int col)
        {
            this.slotType = type;
            this.colIndex = col;
            this.cell = cell;
            this.board = board;
        }

        public virtual void OnBoardInit() { }
        public virtual void OnBoardUpdate(int colIndex) { }
        public virtual void OnReachTop() { }
        public virtual void OnSlotClicked() { }
    }

}
