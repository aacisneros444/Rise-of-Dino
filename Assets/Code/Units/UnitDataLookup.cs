using UnityEngine;
using System.Collections.Generic;

namespace Assets.Code.Units
{
    /// <summary>
    /// A class to serve as a lookup for UnitData scriptable objects.
    /// As such, it is a singleton for easy access.
    /// </summary>
    public class UnitDataLookup : MonoBehaviour
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static UnitDataLookup Instance { get; private set; }

        [Tooltip("The lookup array for UnitData scriptable objects. " +
            "Each array index corresponds to a different unit type.")]
        [SerializeField] private UnitData[] _unitTypes;

        /// <summary>
        /// A dictionary holding lists of land units by level.
        /// </summary>
        private Dictionary<int, List<UnitData>> _landUnitsByLevel;

        /// <summary>
        /// A dictionary holding lists of water units by level.
        /// </summary>
        private Dictionary<int, List<UnitData>> _waterUnitsByLevel;

        [Tooltip("The specific UnitData scriptable obejct for the" +
            " HexTerritory capital unit.")]
        [SerializeField] private UnitData _capitalUnit;

        /// <summary>
        /// Set this as a singleton. Populate the land and water units by level
        /// dictionaries.
        /// </summary>
        private void Awake()
        {
            Instance = this;
            _landUnitsByLevel = new Dictionary<int, List<UnitData>>();
            _waterUnitsByLevel = new Dictionary<int, List<UnitData>>();
            foreach (UnitData unitData in _unitTypes)
            {
                if (unitData.IsWalker)
                {
                    if (!_landUnitsByLevel.ContainsKey(unitData.Level))
                    {
                        List<UnitData> unitDataList = new List<UnitData>();
                        unitDataList.Add(unitData);
                        _landUnitsByLevel.Add(unitData.Level, unitDataList);
                    }
                    else
                    {
                        _landUnitsByLevel[unitData.Level].Add(unitData);
                    }
                }
                if (unitData.IsSwimmer)
                {
                    if (!_waterUnitsByLevel.ContainsKey(unitData.Level))
                    {
                        List<UnitData> unitDataList = new List<UnitData>();
                        unitDataList.Add(unitData);
                        _waterUnitsByLevel.Add(unitData.Level, unitDataList);
                    }
                    else
                    {
                        _waterUnitsByLevel[unitData.Level].Add(unitData);
                    }
                }
            }
        }

        /// <summary>
        /// Get a UnitData scriptable object.
        /// </summary>
        /// <param name="id">The id/index for the desired UnitData.</param>
        /// <returns>The UnitData for the given id/index.</returns>
        public UnitData GetUnitData(int id)
        {
            if (id < 0 || id >= _unitTypes.Length)
            {
                throw new System.ArgumentException("Invalid unit id.");
            }
            return _unitTypes[id];
        }

        /// <summary>
        /// Get a random UnitData scriptable object that can walk on land.
        /// </summary>
        /// <param name="level">The level of the unit to retrieve.</param>
        /// <returns>A random UnitData that can walk land of the given level.</returns>
        public UnitData GetRandomLandUnit(int level)
        {
            List<UnitData> unitList = _landUnitsByLevel[level];
            return unitList[Random.Range(0, unitList.Count)];
        }

        /// <summary>
        /// Get a random UnitData scriptable object that can swim water.
        /// </summary>
        /// <param name="level">The level of the unit to retrieve.</param>
        /// <returns>A random UnitData that can swim in water of the given level.</returns>
        public UnitData GetRandomWaterUnit(int level)
        {
            List<UnitData> unitList = _waterUnitsByLevel[level];
            return unitList[Random.Range(0, unitList.Count)];
        }

        /// <summary>
        /// Get the UnitData scriptable object for a HexTerritory capital
        /// unit.
        /// </summary>
        /// <returns>The UnitData scriptable object for a HexTerritory capital
        /// unit.</returns>
        public UnitData GetTerritoryCapitalUnit()
        {
            return _capitalUnit;
        }
    }
}