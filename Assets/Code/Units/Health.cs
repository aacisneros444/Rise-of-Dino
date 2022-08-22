using UnityEngine;
using Mirror;
using Assets.Code.Networking;
using Assets.Code.Networking.Messaging;
using DG.Tweening;
using Assets.Code.FX;
using System;

namespace Assets.Code.Units
{
    /// <summary>
    /// A unit component to model unit health.
    /// </summary>
    public class Health
    {
        /// <summary>
        /// The Unit this Health component belongs to.
        /// </summary>
        protected Unit _unit;

        /// <summary>
        /// The unit's current health value.
        /// </summary>
        public int Value;

        /// <summary>
        /// Denotes whether the unit is alive or not.
        /// </summary>
        public bool IsAlive { get { return Value > 0; } }

        /// <summary>
        /// The max amount for the unit visual's sprite on the client 
        /// to display a "glitch" effect. More glitching means the unit
        /// is more hurt.
        /// </summary>
        private const float VisualMaxGlitch = 20;
        
        /// <summary>
        /// An event to notify listeners when the unit gets damaged.
        /// </summary>
        public event Action<Unit> Damaged;

        /// <summary>
        /// An event to notify listeners when the unit has been damaged
        /// on the client, providing the amount of damage taken and the position
        /// the damage was taken.
        /// </summary>
        public static event Action<int, Vector3> ClientUnitDamaged;

        /// <summary>
        /// An event to notify listeners when the unit has been healed
        /// on the client, providing the amount of health healed and the position
        /// the healing was done.
        /// </summary>
        public static event Action<int, Vector3> ClientUnitHealed;

        /// <summary>
        /// An event to notify listeners on the client when the unit's 
        /// health value has changed.
        /// </summary>
        public event Action<Unit> ClientUnitHealthChanged;

        /// <summary>
        /// An event to notify listeners when the unit has 0 health and
        /// is about to destroyed.
        /// </summary>
        public event Action<Unit> ClientUnitDying;

        /// <summary>
        /// An event to notify listeners when the unit has 0 health and
        /// is about to be destroyed.
        /// </summary>
        public event Action ClientDying;

        /// <summary>
        /// Create a new Health component. Immediately set the 
        /// current health value to the maximum for the unit.
        /// </summary>
        /// <param name="unit">The component's owner unit.</param>
        public Health(Unit unit)
        {
            _unit = unit;
            Value = unit.Data.Health;
        }

        /// <summary>
        /// Deal an amount of damage to the Unit. If the unit dies, destroy
        /// it and inform clients to do the same. If not, send clients
        /// updated health value.
        /// </summary>
        /// <param name="amount">The amount of damage to deal.</param>
        /// <param name="attackingUnit">The unit dealing damage to this unit.</param>
        public virtual void DealDamage(int amount, Unit attackingUnit)
        {
            // Only deal damage if this unit is alive.
            if (Value > 0)
            {
                Value -= amount;
                Value = Value < 0 ? 0 : Value;
                if (Value > 0)
                {
                    SendHealthValueToClients();
                }
                if (Value <= 0)
                {
                    // Award the attacking unit's owner player evo points if still connected.
                    if (GameNetworkManager.NetworkPlayersById.TryGetValue(attackingUnit.OwnerPlayerId,
                        out Networking.NetworkPlayer networkPlayer))
                    {
                        networkPlayer.UpgradePoints += _unit.Data.Level;
                    }
                    // Destroy the unit on the server.
                    LateUnitDestroyer.Enqueue(_unit);
                }
                else
                {
                    InvokeDamagedEvent(attackingUnit);
                }
            }
        }

        /// <summary>
        /// Heal the unit by a certain amount. Inform clients of
        /// the updated health value.
        /// </summary>
        /// <param name="amount">The amount to heal the unit.</param>
        public void Heal(int amount)
        {
            // Only heal if health is below the maximum.
            if (Value < _unit.Data.Health)
            {
                Value += amount;
                if (Value > _unit.Data.Health)
                {
                    Value = _unit.Data.Health;
                }
                SendHealthValueToClients();
            }
        }

        /// <summary>
        /// Send this unit's health value to all clients.
        /// </summary>
        public void SendHealthValueToClients()
        {
            UpdateUnitHealthMessage msg = new UpdateUnitHealthMessage
            {
                UnitCellIndex = _unit.OccupiedCell.Index,
                Value = Value
            };

            foreach (NetworkConnection conn in GameNetworkManager.NetworkPlayers.Keys)
            {
                conn.Send(msg);
            }
        }

        /// <summary>
        /// Invoke the damaged event. This method allows for
        /// descendant classes to also invoke the event.
        /// </summary>
        /// <param name="attackingUnit">The unit that attacked this unit.</param>
        protected void InvokeDamagedEvent(Unit attackingUnit)
        {
            Damaged?.Invoke(attackingUnit);
        }

        /// <summary>
        /// Set the unit's health value to a new value.
        /// For use on the client.
        /// </summary>
        /// <param name="newValue">The new health value.</param>
        public virtual void SetHealthValue(int newValue)
        {
            if (newValue < Value)
            {
                UnitVisualHurtFlicker();
                InvokeClientUnitDamagedEvent(Value - newValue);
                if (newValue <= 0)
                {
                    ClientUnitDying?.Invoke(_unit);
                    ClientDying?.Invoke();
                    _unit.Destroy();
                }
            }
            else if (newValue > Value)
            {
                // The unit was healed, not damaged.
                InvokeClientUnitHealedEvent(newValue - Value);
            }
            Value = newValue;
            UpdateUnitVisualGlitch();
            InvokeClientUnitHealthChangedEvent();
        }


        /// <summary>
        /// A protected method to allow for descendant classes
        /// to raise the client unit health changed event.
        /// </summary>
        protected void InvokeClientUnitHealthChangedEvent()
        {
            ClientUnitHealthChanged?.Invoke(_unit);
        }

        /// <summary>
        /// A protected method to allow for descendant classes
        /// to raise the client unit damaged event.
        /// </summary>
        /// <param name="damageTaken">The amount of damage taken.</param>
        protected void InvokeClientUnitDamagedEvent(int damageTaken)
        {
            if (_unit.OccupiedCell.IsVisible)
            {
                ClientUnitDamaged?.Invoke(damageTaken,
                    _unit.OccupiedCell.VisualPosition);
            }
        }

        /// <summary>
        /// A protected method to allow for descendant classes
        /// to raise the client unit healed event.
        /// </summary>
        /// <param name="amountHealed">The amount healed.</param>
        protected void InvokeClientUnitHealedEvent(int amountHealed)
        {
            if (_unit.OccupiedCell.IsVisible)
            {
                ClientUnitHealed?.Invoke(amountHealed,
                    _unit.OccupiedCell.VisualPosition);
            }
        }

        /// <summary>
        /// Update the unit's visual based on the current health value.
        /// A more hurt unit will display a more vigorous glitch effect.
        /// </summary>
        protected void UpdateUnitVisualGlitch()
        {
            float glitchAmount = VisualMaxGlitch * (1 - (float)Value / _unit.Data.Health);
            _unit.VisualPrefabInstance.material.SetFloat("_GlitchAmount", glitchAmount);
        }

        /// <summary>
        /// Animate a flicker effect for the unit's visual when hurt and
        /// play a hurt sound effect.
        /// </summary>
        protected void UnitVisualHurtFlicker()
        {
            if (IsAlive && _unit.OccupiedCell.IsVisible)
            {
                Color originalVisualColor = _unit.VisualPrefabInstance.color;
                Color hurtColor = new Color(originalVisualColor.r + 0.6f,
                    originalVisualColor.g + 0.6f, originalVisualColor.b + 0.6f);

                DOTween.Sequence()
                .Append(_unit.VisualPrefabInstance.DOColor(hurtColor, 0f))
                .AppendInterval(0.15f)
                .AppendCallback(_unit.SetVisualPrefabInstanceColor);

                SoundManager.Instance.PlaySoundAtPoint(_unit.Data.HurtSound, _unit.OccupiedCell.Position);
            }
        }
    }
}