using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code.Units
{
    /// <summary>
    /// A class to set the final visibility state for units'
    /// visual instances at the end of a frame.
    /// </summary>
    public class LateUnitVisualVisibilityUpdater : MonoBehaviour
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static LateUnitVisualVisibilityUpdater Instance { get; private set; }

        /// <summary>
        /// The units to update visuals for at the end of the frame. A HashSet to prevent
        /// adding duplicates.
        /// </summary>
        private HashSet<Unit> _unitsToUpdateVisibilityFor;

        /// <summary>
        /// Set the singleton instance.
        /// </summary>
        private void Awake()
        {
            Instance = this;
            _unitsToUpdateVisibilityFor = new HashSet<Unit>();
        }

        /// <summary>
        /// Enqueue a unit to have its visual's visibility updated.
        /// </summary>
        /// <param name="unit">The unit to update visibility for.</param>
        public void Enqueue(Unit unit)
        {
            _unitsToUpdateVisibilityFor.Add(unit);
        }

        /// <summary>
        /// Update the unit visuals for all enqueued units.
        /// </summary>
        private void LateUpdate()
        {
            if (_unitsToUpdateVisibilityFor.Count > 0)
            {
                foreach (Unit unit in _unitsToUpdateVisibilityFor)
                {
                    unit.ChangeVisualPrefabVisibility(unit.OccupiedCell.IsVisible, true);
                }
                _unitsToUpdateVisibilityFor.Clear();
            } 
        }
    }
}