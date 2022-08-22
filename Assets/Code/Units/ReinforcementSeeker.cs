using System.Collections.Generic;
using Assets.Code.Hex;
using Assets.Code.GameTime;

namespace Assets.Code.Units
{
    /// <summary>
    /// A unit component to provide functionality for units
    /// to call nearby units to target an attacking unit when
    /// attacked.
    /// </summary>
    public class ReinforcementSeeker
    {
        /// <summary>
        /// The unit this ReinforcementSeeker component
        /// belongs to.
        /// </summary>
        private readonly Unit _unit;

        /// <summary>
        /// The tile radius to seek out friendly units.
        /// </summary>
        private const int SeekRange = 2;

        /// <summary>
        /// Create a ReinforcementSeeker component, assigning
        /// its owner unit and subscribing to the damaged event
        /// of the unit's health component to know when to seek
        /// reinforcements.
        /// </summary>
        /// <param name="unit">The owner unit.</param>
        public ReinforcementSeeker(Unit unit)
        {
            _unit = unit;
            _unit.Health.Damaged += SeekHelpOnDamage;
        }

        /// <summary>
        /// Upon taking damage, seek help from friendly units in the SeekRange
        /// to attack the attacking unit.
        /// </summary>
        /// <param name="attacker">The unit that attacked this unit.</param>
        public void SeekHelpOnDamage(Unit attacker)
        {
            List<Unit> friendlyUnitsInRange = HexPathfinder.Instance.
                GetAllFriendlyAttackerUnitsInRange(_unit.OccupiedCell, SeekRange, _unit.OwnerPlayerId);

            List<Unit> unitsThatCanHelp = new List<Unit>();
            for (int i = 0; i < friendlyUnitsInRange.Count; i++)
            {
                Unit unit = friendlyUnitsInRange[i];
                if (!unit.Mobile.IsMoving &&
                    unit.Attacker.Target == null &&
                    unit.Attacker.IsInRange(attacker))
                {
                    unit.OperationTick = TickSystem.TickNumber;
                    unitsThatCanHelp.Add(unit);
                }
            }

            for (int i = 0; i < unitsThatCanHelp.Count; i++)
            {
                unitsThatCanHelp[i].Attacker.TryAttack(attacker);
            }
         }

    }
}