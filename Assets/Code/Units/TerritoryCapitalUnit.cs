using Assets.Code.Hex;
using UnityEngine;

namespace Assets.Code.Units
{
    /// <summary>
    /// A descendant of the Unit class. Meant to be specifically
    /// used as a immobile unit at the heart of HexTerritories.
    /// If killed, the unit's owner territory will change ownership
    /// to the player who's unit killed this unit.
    /// </summary>
    public class TerritoryCapitalUnit : Unit
    {
        /// <summary>
        /// The TerritoryCapitalUnit's owner HexTerritory.
        /// </summary>
        public HexTerritory OwnerTerritory;

        /// <summary>
        /// Denotes whether or not the VisualOutlineInstance has been made
        /// visible at least once.
        /// </summary>
        private bool _outlineMadeVisible;

        /// <summary>
        /// Create a new TerritoryCapitalUnit, and specify its UnitData type,
        /// occupied HexCell, owner player, and owner HexTerritory.
        /// </summary>
        /// <param name="data">The UnitData for the TerritoryCapital unit type.</param>
        /// <param name="occupiedCell">The HexCell to spawn the unit on.</param>
        /// <param name="ownerPlayerId">The id of the player who owns this unit.</param>
        /// <param name="ownerTerritory">The TerritoryCapitalUnit's owner HexTerritory.</param>
        public TerritoryCapitalUnit(UnitData data, HexCell occupiedCell, 
            int ownerPlayerId, HexTerritory ownerTerritory) : 
            base(data, occupiedCell, ownerPlayerId, false)
        {
            Health = new TerritoryCapitalHealth(this);
            ReinforcementSeeker = new ReinforcementSeeker(this);
            OwnerTerritory = ownerTerritory;
        }

        /// <summary>
        /// Override to not fade the visual outline when fading after the
        /// VisualOutlineInstance has been made visible at least one. This allows
        /// the player to see territory capitals they have already discovered.
        /// </summary>
        /// <param name="visible">Denotes whether or not to make the visual 
        /// prefab instance visible or invisible.</param>
        /// <param name="fadeOutline">Denotes whether or not to vade the visual
        /// outline as well.</param>
        public override void ChangeVisualPrefabVisibility(bool visible, bool fadeOutline)
        {
            if (!_outlineMadeVisible)
            {
                base.ChangeVisualPrefabVisibility(visible, fadeOutline);
                _outlineMadeVisible = true;
            }
            else
            {
                base.ChangeVisualPrefabVisibility(visible, false);
            }
        }
    }
}