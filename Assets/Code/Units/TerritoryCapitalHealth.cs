using Mirror;
using Assets.Code.Networking;

namespace Assets.Code.Units
{
    /// <summary>
    /// A descendant of the unit Health component with slightly
    /// modified behavior. Instead of destroying the unit upon death,
    /// will update the ownership of the unit's owner HexTerritory.
    /// </summary>
    public class TerritoryCapitalHealth : Health
    {
        /// <summary>
        /// The Unit this Health component belongs to.
        /// </summary>
        public TerritoryCapitalUnit _territoryCapitalUnit;

        /// <summary>
        /// Create a new TerritoryCapitalHealth component. 
        /// Immediately set the current health value to the 
        /// maximum for the unit.
        /// </summary>
        /// <param name="unit">The component's owner unit.</param>
        public TerritoryCapitalHealth(TerritoryCapitalUnit unit) : base(unit)
        {
            _territoryCapitalUnit = unit;
        }

        /// <summary>
        /// Deal an amount of damage to the Unit. If the unit dies, change
        /// the TerritoryCapitalUnit's HexTerritory's player ownership. Otherwise,
        /// inform clients of the new health value.
        /// </summary>
        /// <param name="amount">The amount of damage to deal.</param>
        /// <param name="attacker">The unit dealing damage to this unit.</param>
        public override void DealDamage(int amount, Unit attacker)
        {
            Value -= amount;
            Value = Value < 0 ? 0 : Value;
            SendHealthValueToClients();
            if (Value <= 0)
            {
                Value = _territoryCapitalUnit.Data.Health;
                SendHealthValueToClients();
                _territoryCapitalUnit.OwnerTerritory.SetOwnership(attacker.OwnerPlayerId);
            }
            else
            {
                InvokeDamagedEvent(attacker);
            }
        }

        /// <summary>
        /// Set the unit's health value to a new value.
        /// For use on the client.
        /// </summary>
        /// <param name="newValue">The new health value.</param>
        public override void SetHealthValue(int newValue)
        {
            if (newValue < Value)
            {
                UnitVisualHurtFlicker();
                InvokeClientUnitDamagedEvent(Value - newValue);
            }
            else if (newValue > Value)
            {
                InvokeClientUnitHealedEvent(newValue - Value);
            }
            Value = newValue;
            UpdateUnitVisualGlitch();
            InvokeClientUnitHealthChangedEvent();
        }
    }
}