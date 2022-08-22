using UnityEngine;
using Mirror;
using Assets.Code.Hex;
using Assets.Code.Networking;
using Assets.Code.Networking.Messaging;
using System.Collections.Generic;
using Assets.Code.GameTime;

namespace Assets.Code.Units
{
    /// <summary>
    /// A class to handle incoming NetworkMessages for both the client and server
    /// regarding units on a HexWorld's HexGrid.
    /// </summary>
    public class WorldUnitNetworkMessageAdapter : MonoBehaviour
    {
        /// <summary>
        /// The HexGrid for the generated HexWorld map.
        /// </summary>
        [SerializeField] private HexGrid _hexGrid;

        /// <summary>
        /// Register the necessary handler methods for receiving NetworkMessages.
        /// </summary>
        public void Awake()
        {
            NetworkClient.RegisterHandler<SpawnUnitMessage>(CreateUnitFromServer);
            NetworkClient.RegisterHandler<MoveUnitMessage>(MoveUnitFromServer);
            NetworkClient.RegisterHandler<UpdateUnitHealthMessage>(UpdateUnitHealthFromServer);
            NetworkClient.RegisterHandler<UnitAttackedMessage>(UnitAttackedFromServer);
            if (!NetworkClient.active)
            {
                NetworkServer.RegisterHandler<MoveOrderMessage>(MoveOrderRequestFromClient);
                NetworkServer.RegisterHandler<AttackOrderMessage>(AttackOrderRequestFromClient);
            }
        }

        /// <summary>
        /// Process a move order request after receiving a MoveOrderMessage from
        /// a client.
        /// </summary>
        /// <param name="conn">The client's associated NetworkConnection.</param>
        /// <param name="msg">The MoveOrderMessage sent by the client.</param>
        private void MoveOrderRequestFromClient(NetworkConnection conn, MoveOrderMessage msg)
        {
            // Validate request.
            List<Unit> validUnits = new List<Unit>();
            HexCell toCell = _hexGrid.GetCell(msg.ToCellIndex);
            if (toCell != null && toCell.Unit == null)
            {
                for (int i = 0; i < msg.UnitCellIndices.Length; i++)
                {
                    HexCell unitCell = _hexGrid.GetCell(msg.UnitCellIndices[i]);
                    if (unitCell != null && unitCell.Unit != null &&
                        unitCell.Unit.OwnerPlayerId == GameNetworkManager.NetworkPlayers[conn].PlayerId)
                    {
                        unitCell.Unit.OperationTick = TickSystem.TickNumber;
                        validUnits.Add(unitCell.Unit);
                    }
                }
            }

            // Order units to move.
            for (int i = 0; i < validUnits.Count; i++)
            {
                validUnits[i].Mobile.TryMove(toCell, true);
            }
        }

        /// <summary>
        /// Process an attack order request after receiving an AttackOrderMessage from 
        /// a client.
        /// </summary>
        /// <param name="conn">The client's associated NetworkConnection.</param>
        /// <param name="msg">The AttackOrderMessage sent by the client.</param>
        private void AttackOrderRequestFromClient(NetworkConnection conn, AttackOrderMessage msg)
        {
            // Validate request.
            List<Unit> validUnits = new List<Unit>();
            HexCell targetCell = _hexGrid.GetCell(msg.TargetCellIndex);
            if (targetCell != null && targetCell.Unit != null)
            {
                for (int i = 0; i < msg.UnitCellIndices.Length; i++)
                {
                    HexCell unitCell = _hexGrid.GetCell(msg.UnitCellIndices[i]);
                    if (unitCell != null && unitCell.Unit != null &&
                        unitCell.Unit.OwnerPlayerId == GameNetworkManager.NetworkPlayers[conn].PlayerId)
                    {
                        if (targetCell.Unit.OwnerPlayerId != unitCell.Unit.OwnerPlayerId)
                        {
                            //Debug.Log("Succesful attack request from client for unit: " +
                            //    unitCell.Unit.Data.name + " at cell: " + msg.UnitCellIndices[i] +
                            //    " to attack unit " + targetCell.Unit.Data.name +
                            //    " at cell: " + targetCell.Index);
                            unitCell.Unit.OperationTick = TickSystem.TickNumber;
                            validUnits.Add(unitCell.Unit);
                        }
                    }
                }
            }

            for (int i = 0; i < validUnits.Count; i++)
            {
                validUnits[i].Attacker.TryAttack(targetCell.Unit);
            }
        }


        /// <summary>
        /// Create a new unit after receiving a SpawnUnitMessage from the server.
        /// </summary>
        /// <param name="msg">The SpawnUnitMessage sent by the server.</param>
        private void CreateUnitFromServer(SpawnUnitMessage msg)
        {
            new Unit(UnitDataLookup.Instance.GetUnitData(msg.Id),
                _hexGrid.GetCell(msg.CellIndex), msg.OwnerPlayerId, true);
        }

        /// <summary>
        /// Move a unit after receiving a MoveUnitMessage from the server.
        /// </summary>
        /// <param name="msg">The MoveUnitMessage sent by the server.</param>>
        private void MoveUnitFromServer(MoveUnitMessage msg)
        {
            _hexGrid.GetCell(msg.UnitCellIndex).Unit.Mobile.ClientMoveUnit
                (_hexGrid.GetCell(msg.ToCellIndex));
        }

        /// <summary>
        /// Update a unit's health value after receiving an UpdateUnitHealthMessage
        /// from the server.
        /// </summary>
        /// <param name="msg">The UpdateUnitHealthMessage from the server.</param>
        private void UpdateUnitHealthFromServer(UpdateUnitHealthMessage msg)
        {
            _hexGrid.GetCell(msg.UnitCellIndex).Unit.Health.SetHealthValue(msg.Value);
        }

        /// <summary>
        /// Play a unit's attack effects after receiving a UnitAttackMessage from 
        /// the server.
        /// </summary>
        /// <param name="msg">The UnitAttackMessage from the server.</param>
        private void UnitAttackedFromServer(UnitAttackedMessage msg)
        {
            _hexGrid.GetCell(msg.UnitCellIndex).Unit.Attacker.
                ClientAttack(_hexGrid.GetCell(msg.TargetCellIndex));
        }
    }
}