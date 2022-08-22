using UnityEngine;
using Assets.Code.GameTime;
using Assets.Code.Networking;
using Assets.Code.Networking.Messaging;
using Mirror;
using Assets.Code.FX;
using DG.Tweening;
using Assets.Code.Hex;

namespace Assets.Code.Units
{
    /// <summary>
    /// A unit component to provide functionality for units 
    /// to attack other units.
    /// </summary>
    public class Attacker
    {
        /// <summary>
        /// The Unit this Attacker component belongs to.
        /// </summary>
        private readonly Unit _unit;

        /// <summary>
        /// The current target Unit to attack.
        /// </summary>
        public Unit Target { get; private set; }

        /// <summary>
        /// Denotes whether or not the unit is attacking.
        /// </summary>
        private bool _isAttacking;

        /// <summary>
        /// The number of ticks elapsed since the last attack.
        /// </summary>
        private int _ticksSinceLastAttack;

        /// <summary>
        /// Denotes whether or not this Attacker component
        /// is already listening for ticks.
        /// </summary>
        private bool _isTickListener;

        /// <summary>
        /// Denotes whether or not this Attacker component
        /// is already listening to the Mobile component
        /// for path completion.
        /// </summary>
        private bool _isPathCompletedListener;

        /// <summary>
        /// Denotes whether or not this Attacker component
        /// is already listening to the Mobile component
        /// for a client move request.
        /// </summary>
        private bool _isClientMoveRequestListener;

        /// <summary>
        /// Create a new Attacker component, assigning it its owner
        /// Unit and immediately priming it for attacking.
        /// </summary>
        /// <param>The component's owner unit.</param>
        public Attacker(Unit unit)
        {
            _unit = unit;
            _ticksSinceLastAttack = unit.Data.AttackTicks;
            _unit.Health.Damaged += TryTargetingAttackingUnit;
        }

        /// <summary>
        /// Ask the unit to attack another unit. If it is in range, 
        /// the unit will attack as soon as it can. If it is out of range
        /// and a path can be found to the target unit, it will travel 
        /// that path and then attack. If no path can be found, nothing
        /// occurs.
        /// </summary>
        /// <param name="target">The target unit.</param>
        public void TryAttack(Unit target)
        {
            _unit.Mobile.StopMoving();
            ClearTarget();
            Target = target;
            // Subscribe to the ServerClientMoveRequest event in the Mobile
            // component to know when to call the ClearTarget method. Mark
            // the _isClientMoveRequestListener flag to prevent multiple
            // subscriptions.
            if (!_isClientMoveRequestListener)
            {
                _unit.Mobile.ServerClientMoveRequest += ClearTarget;
                _isClientMoveRequestListener = true;
            }
            CheckIfCanAttack();
        }

        /// <summary>
        /// Check if the unit can attack.
        /// </summary>
        private void CheckIfCanAttack()
        {
            if (Target != null)
            {
                if (IsInRange())
                {
                    if (TargetStillValid())
                    {
                        _isAttacking = true;

                        // If enough ticks have already elapsed immediately attack the target.
                        if (_ticksSinceLastAttack == _unit.Data.AttackTicks)
                        {
                            AttackTarget();
                        }

                        // Subscribe to the Tick event to know when to attack in the future.
                        // Mark the _isTickListener flag to guard against multiple subscriptions
                        // to the Tick event.
                        if (!_isTickListener)
                        {
                            TickSystem.Tick += OnTick;
                            _isTickListener = true;
                        }
                    }
                    else
                    {
                        ClearTarget();
                    }
                }
                else
                {
                    // Subscibe to the ServerCompletedPath event of the Mobile component
                    // to know when to check if this unit can attack again. Subscribe
                    // early before checking if a path even exists since the TryMove
                    // method may move immediately.
                    _unit.Mobile.ServerCompletedPath += CompletedPathCheckIfCanAttack;
                    _isPathCompletedListener = true;

                    if (!_unit.Mobile.TryMoveToTarget(Target))
                    {
                        // No path in attack range to target, clear target.
                        ClearTarget();
                    }
                }
            }
            else
            {
                ClearTarget();
            }       
        }

        /// <summary>
        /// Upon completing the path to get into range, check to see
        /// if attacking is now possible.
        /// </summary>
        private void CompletedPathCheckIfCanAttack()
        {
            _unit.Mobile.ServerCompletedPath -= CompletedPathCheckIfCanAttack;
            _isPathCompletedListener = false;
            CheckIfCanAttack();
        }

        /// <summary>
        /// Check if the unit is in range to attack the set target.
        /// </summary>
        /// <returns>True if in range to attack, false otherwise.</returns>
        public bool IsInRange()
        {
            return _unit.OccupiedCell.Coordinates.DistanceTo(
                Target.OccupiedCell.Coordinates) <= _unit.Data.AttackRange;
        }

        /// <summary>
        /// Check if the unit is in range to attack a potential target.
        /// </summary>
        /// <param name="target">The potential target.</param>
        /// <returns>True if in range to potential target, false otherwise.</returns>
        public bool IsInRange(Unit target)
        {
            return _unit.OccupiedCell.Coordinates.DistanceTo(
                target.OccupiedCell.Coordinates) <= _unit.Data.AttackRange;
        }

        /// <summary>
        /// Increment the number of ticks since the last attack
        /// every tick until the number of ticks required for attacking 
        /// is met. If the number of ticks elapsed since the last
        /// attack is the number of ticks necessary to attack, attack
        /// the set target unit if it exists.
        /// </summary>
        private void OnTick()
        {
            _ticksSinceLastAttack++;

            // Check every tick to ensure the target is still alive 
            // or is on the same side (in the case of attacking a 
            // HexTerritoryCapital Unit which can change player ownership).
            // If not alive or has changed team, set the _target variable 
            // to null so that the garbage collector can reclaim the memory 
            // of the dead target unit or stop attacking.
            if (_isAttacking)
            {
                if (!TargetStillValid())
                {
                    ClearTarget();
                }
            }

            if (_ticksSinceLastAttack == _unit.Data.AttackTicks)
            {
                if (_isAttacking)
                {
                    AttackTarget();
                }
                else
                {
                    // Not attacking, meaning the previous
                    // target unit has already been destroyed and
                    // the _ticksSinceLastAttack is now the number of 
                    // ticks needed to attack again. Unsubscribe from
                    // TickSystem Tick event.
                    TickSystem.Tick -= OnTick;
                    _isTickListener = false;
                }
            }
        }

        /// <summary>
        /// Determines if the set target is still a valid target.
        /// </summary>
        /// <returns>True if valid target, false otherwise.</returns>
        private bool TargetStillValid()
        {
            return Target.Health.IsAlive &&
                Target.OwnerPlayerId != _unit.OwnerPlayerId &&
                IsInRange();
        }

        /// <summary>
        /// Attack the target unit. Inform clients that the unit attacked.
        /// </summary>
        private void AttackTarget()
        {
            _ticksSinceLastAttack = 0;

            UnitAttackedMessage msg = new UnitAttackedMessage
            {
                UnitCellIndex = _unit.OccupiedCell.Index,
                TargetCellIndex = Target.OccupiedCell.Index
            };

            foreach (NetworkConnection conn in GameNetworkManager.NetworkPlayers.Keys)
            {
                conn.Send(msg);
            }

            int damageToDeal = _unit.Data.AttackDamage;
            if (_unit.Data.IsSwimmer && !_unit.Data.IsAmphibious && 
                Target.Data.IsWalker && !Target.Data.IsSwimmer &&
                Target.OccupiedCell.TerrainType.Id == (int)HexMapTerrain.TerrainType.ShallowWater)
            {
                // Double damage if water unit is attacking a land unit in shallow water.
                damageToDeal *= 2;
            }
            if (_unit.Data.IsWalker && !_unit.Data.IsAmphibious && 
                Target.Data.IsSwimmer && !Target.Data.IsWalker &&
                _unit.OccupiedCell.TerrainType.IsLand)
            {
                // Double damage if land unit is attack a water unit from land.
                damageToDeal *= 2;
            }
            Target.Health.DealDamage(damageToDeal, _unit);
        }

        /// <summary>
        /// Clear the target and unsubscribe to any previously subscribed
        /// events to prevent multiple subscriptions and unintended behavior.
        /// </summary>
        public void ClearTarget()
        {
            Target = null;
            _isAttacking = false;
            if (_isPathCompletedListener)
            {
                _unit.Mobile.ServerCompletedPath -= CompletedPathCheckIfCanAttack;
                _isPathCompletedListener = false;
            }
        }

        /// <summary>
        /// Try attacking in return when attacked by another unit if no target
        /// already exists and is not moving.
        /// </summary>
        /// <param name="attackingUnit">The unit that attacked this unit.</param>
        private void TryTargetingAttackingUnit(Unit attackingUnit)
        {
            if (Target == null && !_unit.Mobile.IsMoving)
            {
                TryAttack(attackingUnit);
            }
        }

        /// <summary>
        /// Execute select logic before unit is destroyed. 
        /// </summary>
        public void PrepareForDisposal()
        {
            ClearTarget();
            if (_isTickListener)
            {
                TickSystem.Tick -= OnTick;
            }
        }

        /// <summary>
        /// Play visual attack effects on the client.
        /// </summary>
        /// <param name="targetPosition">The cell of the targeted unit.</param>
        public void ClientAttack(HexCell targetCell)
        {
            if (_unit.OccupiedCell.IsVisible)
            {
                // Animate an attack movement.
                DOTween.Sequence()
                    .Append(_unit.VisualPrefabInstance.transform.DORotate(new Vector3(90, -25, 0), 0.125f))
                    .Append(_unit.VisualPrefabInstance.transform.DORotate(new Vector3(90, 0, 0), 0.375f));
            }

            if (targetCell.IsVisible)
            {
                // Play the designated visual attack effect for the unit.
                ProceduralEffectManager.Instance.PlayEffect((int)_unit.Data.AttackEffect,
                    targetCell.VisualPosition, _unit.VisualPrefabInstance.color);
            }
        }
    }
}