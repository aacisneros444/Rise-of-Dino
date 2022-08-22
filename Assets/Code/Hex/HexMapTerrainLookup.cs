using UnityEngine;

namespace Assets.Code.Hex
{
    /// <summary>
    /// A class to serve as a lookup for HexMapTerrain scriptable objects.
    /// As such, it is a singleton for easy access.
    /// </summary>
    public class HexMapTerrainLookup : MonoBehaviour
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static HexMapTerrainLookup Instance { get; private set; }

        [Tooltip("The lookup array for HexMapTerrain scriptable objects. " +
            "Each array index corresponds to a different terrain type.")]
        [SerializeField] private HexMapTerrain[] _terrainTypes;

        /// <summary>
        /// Set this as a singleton.
        /// </summary>
        private void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Get a HexMapTerrain scriptable object.
        /// </summary>
        /// <param name="id">The id/index for the desired HexMapTerrain.</param>
        /// <returns>The HexMapTerrain for the given id/index.</returns>
        public HexMapTerrain GetHexMapTerrain(int id)
        {
            if (id < 0)
            {
                throw new System.ArgumentException("id must be non-negative.");
            }
            else if (id >= _terrainTypes.Length)
            {
                id = _terrainTypes.Length - 1;
            }
            return _terrainTypes[id];
        }
    }
}