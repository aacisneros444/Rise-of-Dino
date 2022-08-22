using System.Collections.Generic;
using Assets.Code.Hex;
using Assets.Code.GameTime;
using Mirror;
using Assets.Code.Networking;
using DG.Tweening;
using UnityEngine;
using System;
using Assets.Code.Networking.Messaging;
using Assets.Code.FX;

namespace Assets.Code.Units
{
    /// <summary>
    /// A unit component to provide functionality for units 
    /// to move around on a HexGrid.
    /// </summary>
    public class Mobile
    {
        /// <summary>
        /// The Unit this Mobile component belongs to.
        /// </summary>
        private readonly Unit _unit;

        /// <summary>
        /// The currently traveled path. None if null.
        /// If not null, this means the unit is currently moving.
        /// </summary>
        private List<HexCell> _activePath;

        /// <summary>
        /// The index for the HexCell the unit is currently at
        /// in the active path.
        /// </summary>
        private int _activePathIndex;

        /// <summary>
        /// The number of ticks elapsed since the last move.
        /// </summary>
        private int _ticksSinceLastMove;

        /// <summary>
        /// Denotes whether or not this mobile component
        /// is already listening for ticks.
        /// </summary>
        private bool _isTickListener;

        /// <summary>
        /// Denotes whether or not the unit is moving.
        /// </summary>
        public bool IsMoving { get; set; }

        /// <summary>
        /// An event to notify subscribers on the server that a new
        /// move request has been received from the client.
        /// </summary>
        public event Action ServerClientMoveRequest;

        /// <summary>
        /// An event to notify subscribers on the server that the unit 
        /// has completed its path.
        /// </summary>
        public event Action ServerCompletedPath;

        /// <summary>
        /// An event to notify subscribers on the client that the unit 
        /// has moved.
        /// </summary>
        public event Action<Unit> ClientUnitMoved;


        /// <summary>
        /// Create a new Mobile component, assigning it its owner
        /// Unit and immediately priming it for movement.
        /// </summary>
        /// <param>The component's owner unit.</param>
        public Mobile(Unit unit)
        {
            _unit = unit;
            _ticksSinceLastMove = unit.Data.MoveTicks;
        }

        /// <summary>
        /// Ask the unit to move to a cell. If a path can be found, the journey begins.
        /// If not, nothing occurs.
        /// </summary>
        /// <param name="toCell">The destination cell.</param>
        /// <param name="fromClient">Denotes whether or not the move
        /// request is from the client. True if from the client, false
        /// if from the server.</param>
        /// <returns>True if a path was found and will begin moving, 
        /// false otherwise.</returns>
        public bool TryMove(HexCell toCell, bool fromClient)
        {
            if (fromClient)
            {
                ServerClientMoveRequest?.Invoke();
            }

            List<HexCell> path = 
                HexPathfinder.Instance.FindPath(_unit.OccupiedCell, toCell);
            if (path != null)
            {
                // Reset path data to the new path.
                _activePath = path;
                _activePathIndex = 0;

                // Mark the unit as moving.
                IsMoving = true;

                // If enough ticks have already elapsed, immediately move to the 
                // next HexCell in the path.
                if (_ticksSinceLastMove == _unit.Data.MoveTicks &&
                    _activePath[0].Unit == null)
                {
                    TravelPath();
                }

                // Guard against multiple subscriptions to the Tick event. 
                // If the unit was not previously listening for ticks, subscribe
                // to the TickSystem.
                if (!_isTickListener)
                {
                    TickSystem.Tick += OnTick;
                    _isTickListener = true;
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// Ask the unit to move in range to a target unit. If a path can be found, 
        /// the journey begins. If not, nothing occurs.
        /// </summary>
        /// <param name="target">The target unit.</param>
        /// <returns>True if a path could be found, false otherwise.</returns>
        public bool TryMoveToTarget(Unit target)
        {
            HexCell attackFromCell = HexPathfinder.Instance.
                GetClosestCellInRangeToTarget(_unit.OccupiedCell,
                target.OccupiedCell, _unit.Data.AttackRange);
            if (attackFromCell != null)
            {
                return TryMove(attackFromCell, false);
            }
            return false;
        }

        /// <summary>
        /// Increment the number of ticks since the last movement
        /// every tick until the number of ticks required for movement 
        /// is met. If the number of ticks elapsed since the last
        /// movement is the number of ticks necessary to move, travel
        /// one HexCell in the active path if it exists and the next 
        /// cell is unoccupied by any other unit. In that case, try to
        /// calculate a new path and continue traveling.
        /// </summary>
        private void OnTick()
        {
            _ticksSinceLastMove++;
            if (_ticksSinceLastMove >= _unit.Data.MoveTicks)
            {
                if (_activePath != null)
                {
                    HexCell nextInPath = _activePath[_activePathIndex];
                    if (nextInPath.Unit == null)
                    {
                        // If the active path exists and the next cell 
                        // in the path is unoccupied, travel.
                        TravelPath();
                    }
                    else if (nextInPath.Unit.OperationTick == _unit.OperationTick &&
                        nextInPath.Unit.OwnerPlayerId == _unit.OwnerPlayerId)
                    {
                        // Next cell is occupied, but unit is in same movement group. Wait to move.
                        if (!nextInPath.Unit.Mobile.IsMoving && TickSystem.TickNumber > _unit.OperationTick)
                        {
                            // Unit occupying next cell has finished moving, try finding another path to
                            // destination.
                            RetryOnDifferentPath();
                        }
                    }
                    else
                    {
                        // Destination cell was occupied, attempt to get a new path to the destination.
                        RetryOnDifferentPath();
                    }
                }
                else
                {
                    // There is no active path, meaning the previous
                    // path has already been completely traveled and 
                    // the _ticksSinceLastMove is now the number of 
                    // ticks needed to move again. Unsubscribe from
                    // TickSystem Tick event and mark _isTickListener as
                    // false.
                    TickSystem.Tick -= OnTick;
                    _isTickListener = false;
                }
            }
        }

        /// <summary>
        /// Destination cell was occupied, attempt to get a new path to the destination.
        /// </summary>
        private void RetryOnDifferentPath()
        {
            // Increment the operation tick by one to group the unit with any other moving
            // units from the same group who have not reached the destination but
            // separate it from those who already have.
            _unit.OperationTick++;
            if (_unit.Attacker.Target != null)
            {
                if (!TryMoveToTarget(_unit.Attacker.Target))
                {
                    StopMovingOnFailureToReachToCell();
                }
            }
            else
            {
                if (!TryMove(_activePath[_activePath.Count - 1], false))
                {
                    StopMovingOnFailureToReachToCell();
                }
            }
        }

        /// <summary>
        /// Stop moving when reaching the exact destination cell
        /// fails.
        /// </summary>
        private void StopMovingOnFailureToReachToCell()
        {
            IsMoving = false;
            TickSystem.Tick -= OnTick;
            _isTickListener = false;
        }

        /// <summary>
        /// Travel one HexCell in the active path.
        /// </summary>
        private void TravelPath()
        {
            _ticksSinceLastMove = 0;
            ServerMoveUnit(_activePath[_activePathIndex]);
            _activePathIndex++;
            if (_activePathIndex == _activePath.Count ||
                (_unit.Attacker.Target != null && _unit.Attacker.IsInRange()))
            {
                // Finished path or has a target and is in range.
                StopMoving();
                ServerCompletedPath?.Invoke();
            }
        }

        /// <summary>
        /// Stop moving if traveling.
        /// </summary>
        public void StopMoving()
        {
            if (IsMoving)
            {
                _activePath = null;
                IsMoving = false;
            }
        }

        /// <summary>
        /// Move the unit on the server to a new cell. Notify clients
        /// to reflect the change.
        /// </summary>
        /// <param name="newCell">The new cell to move the unit to.</param>
        private void ServerMoveUnit(HexCell newCell)
        {
            int oldCellIndex = _unit.OccupiedCell.Index;
            _unit.OccupiedCell = newCell;

            MoveUnitMessage msg = new MoveUnitMessage
            {
                UnitCellIndex = oldCellIndex,
                ToCellIndex = newCell.Index
            };

            foreach (NetworkConnection conn in GameNetworkManager.NetworkPlayers.Keys)
            {
                conn.Send(msg);
            }
        }

        /// <summary>
        /// Execute select logic before unit is destroyed. 
        /// </summary>
        public void PrepareForDisposal()
        {
            if (_isTickListener)
            {
                TickSystem.Tick -= OnTick;
            }
        }

        /// <summary>
        /// Move the unit on the client, playing an animation and sound.
        /// </summary>
        /// <param name="toCell">The destination cell.</param>
        public void ClientMoveUnit(HexCell toCell)
        {
            HexCell oldCell = _unit.OccupiedCell;
            bool oldCellWasVisible = oldCell.IsVisible;
            _unit.OccupiedCell = toCell;

            ClientUnitMoved?.Invoke(_unit);

            if (_unit.Data.IsAmphibious)
            {
                // Swap materials if going from land to water or vice versa.
                if (oldCell.TerrainType.IsLand && !toCell.TerrainType.IsLand)
                {
                    _unit.VisualOutlineInstance.material = _unit.Data.VisualMaterials[1];
                    _unit.VisualPrefabInstance.material = _unit.Data.VisualMaterials[1];
                }
                else if (!oldCell.TerrainType.IsLand && toCell.TerrainType.IsLand)
                {
                    _unit.VisualOutlineInstance.material = _unit.Data.VisualMaterials[0];
                    _unit.VisualPrefabInstance.material = _unit.Data.VisualMaterials[0];
                }
            } 

            if (toCell.IsVisible || oldCellWasVisible)
            {
                // Animate the movement.
                DOTween.Sequence()
                    .Append(_unit.VisualPrefabInstance.transform.DORotate(new Vector3(90, 10, 0), 0.125f))
                    .Append(_unit.VisualPrefabInstance.transform.DORotate(new Vector3(90, -10, 0), 0.25f))
                    .Append(_unit.VisualPrefabInstance.transform.DORotate(new Vector3(90, -0, 0), 0.125f));
                _unit.VisualPrefabInstance.transform.DOMove(toCell.VisualPosition, 0.5f);

                if (_unit.Data.IsAmphibious)
                {
                    // Play a movement sound depending on toCell's terrain type.
                    AudioClip soundToPlay = oldCell.TerrainType.IsLand ?
                        _unit.Data.MoveSounds[0] : _unit.Data.MoveSounds[1];
                    SoundManager.Instance.PlaySoundAtPoint(soundToPlay, toCell.Position);
                }
                else
                {
                    // Play a movement sound.
                    SoundManager.Instance.PlaySoundAtPoint(_unit.Data.MoveSounds[0], toCell.Position);
                }
            }
            else
            {
                _unit.VisualPrefabInstance.transform.position = toCell.VisualPosition;
            }
        }
    }
}