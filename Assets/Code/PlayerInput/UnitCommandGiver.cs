using UnityEngine;
using Assets.Code.Hex;
using Mirror;
using Assets.Code.Networking.Messaging;

namespace Assets.Code.PlayerInput
{
    /// <summary>
    /// A GameObject component to allow players to issue unit order requests
    /// to the server.
    /// </summary>
    public class UnitCommandGiver : MonoBehaviour
    {
        /// <summary>
        /// The HexGrid to get cells from.
        /// </summary>
        [SerializeField] private HexGrid _hexGrid;

        /// <summary>
        /// The class holding the currently selected unit.
        /// </summary>
        [SerializeField] private UnitSelector _unitSelector;


        /// <summary>
        /// If at least one unit is selected and the player left-clicks, 
        /// issue a unit move request if the clicked cell is empty.
        /// </summary>
        private void Update()
        {
            if (Input.GetMouseButtonUp(0) && NetworkClient.active &&
                _hexGrid.HasGenerated && _unitSelector.SelectedUnits.Count > 0 &&
                !_unitSelector.JustDragSelected)
            {
                HexCell clickedCell = _hexGrid.GetCellAtPosition(InputUtils.GetMousePoint());
                if (clickedCell != null)
                {
                    if (clickedCell.Unit == null)
                    {
                        GiveMoveOrder(clickedCell);
                        _unitSelector.DeselectUnits();
                        _unitSelector.PauseSelectionUntilMouseUp();
                    }
                    else if (clickedCell.Unit.OwnerPlayerId !=
                        Networking.NetworkPlayer.AuthorityInstance.PlayerId)
                    {
                        GiveAttackOrder(clickedCell);
                        _unitSelector.DeselectUnits();
                        _unitSelector.PauseSelectionUntilMouseUp();
                    }
                }
            }
        }

        /// <summary>
        /// Send a move order request to the server.
        /// </summary>
        /// <param name="toCell">The cell to move the currently 
        /// selected units to.</param>
        private void GiveMoveOrder(HexCell toCell)
        {
            MoveOrderMessage msg = new MoveOrderMessage
            {
                UnitCellIndices = _unitSelector.GetUnitCellIndicesArray(),
                ToCellIndex = toCell.Index
            };

            NetworkClient.Send(msg);
        }

        /// <summary>
        /// Send an attack order request to the server.
        /// </summary>
        /// <param name="targetCell">The cell that the target 
        /// unit to attack is on.</param>
        private void GiveAttackOrder(HexCell targetCell)
        {
            AttackOrderMessage msg = new AttackOrderMessage
            {
                UnitCellIndices = _unitSelector.GetUnitCellIndicesArray(),
                TargetCellIndex = targetCell.Index
            };

            NetworkClient.Send(msg);
        }
    }
}