using System.Collections.Generic;
using UnityEngine;

namespace CubeLand.Gameplay
{
    public class Board : MonoBehaviour
    {
        [SerializeField] private Transform boardRoot;
        [SerializeField] private GameObject cellBasePrefab;
        [SerializeField] private Transform boardOrigin;

        [Header("Cấu hình khoảng cách")]
        [SerializeField] private float cellWidth = 1.2f;
        [SerializeField] private float cellHeight = 1.5f;

        private int columnCount;
        private List<Cell>[] boardColumns;
        public bool isMovingCells = false; // Flag để khóa tương tác khi đang có Cell di chuyển

        public void InitializeBoard(GridLevelData gridData)
        {
            if (gridData == null || gridData.slots == null) return;

            this.columnCount = gridData.columnCount;
            this.boardColumns = new List<Cell>[columnCount];

            // 1. Khởi tạo các List cho từng cột
            for (int i = 0; i < columnCount; i++)
            {
                boardColumns[i] = new List<Cell>();
            }

            // 2. Chuyển đổi dữ liệu phẳng (flat list) từ gridData sang cấu trúc cột
            for (int i = 0; i < gridData.slots.Count; i++)
            {
                GridSlotNode node = gridData.slots[i];
                int col = i % columnCount; // Xác định cột dựa trên index

                // Vị trí hàng trong cột (index hiện tại của cột)
                int row = boardColumns[col].Count;

                // 3. Spawn Cell và thêm vào cột
                Cell newCell = SpawnCellPrefab(node, col, row);
                boardColumns[col].Add(newCell);
            }

            // 4. Thiết lập trạng thái tương tác ban đầu
            UpdateInteractableCells();
            EventDispatcher.PostEvent(Constant.OnBoardInitEvent);
        }

        public void UpdateInteractableCells()
        {
            for (int c = 0; c < columnCount; c++)
            {
                for (int r = 0; r < boardColumns[c].Count; r++)
                {
                    // Chỉ hàng đầu tiên (r == 0) mới cho phép bấm
                    boardColumns[c][r].SetInteractable(r == 0);
                }
            }
        }

        public void OnCellDisplaced(Cell cell, int colIndex)
        {
            // Xóa ô này khỏi hàng đợi của cột
            boardColumns[colIndex].Remove(cell);

            // ĐẨY CÁC CELL PHÍA DƯỚI TIẾN LÊN
            for (int r = 0; r < boardColumns[colIndex].Count; r++)
            {
                Vector3 targetPosition = CalculateCellPosition(colIndex, r);
                boardColumns[colIndex][r].MoveToPosition(targetPosition);
            }

            // Cập nhật lại quyền Click
            UpdateInteractableCells();
            EventDispatcher.PostEvent(Constant.OnBoardUpdateEvent, colIndex);
        }

        public Vector3 CalculateCellPosition(int col, int row)
        {
            float originOffset = (columnCount - 1) * 0.5f;
            float posX = (col - originOffset) * cellWidth;
            float posZ = -row * cellHeight;

            return boardOrigin.position + new Vector3(posX, 0f, posZ);
        }

        public Cell SpawnCellPrefab(GridSlotNode node, int col, int row)
        {
            Vector3 targetPos = CalculateCellPosition(col, row);

            // Spawn từ Pool
            GameObject cellGo = SimplePool.Instance.Spawn(cellBasePrefab, targetPos, Quaternion.identity);
            cellGo.transform.SetParent(boardRoot);

            Cell cellScript = cellGo.GetComponent<Cell>();
            cellScript.Setup(node, col, this); // Nạp data vào Cell

            return cellScript;
        }
    }
}