using UnityEngine;

namespace Assets.Code.Hex
{
    /// <summary>
    /// A scriptable object that holds data pertaining to a terrain type
    /// for a HexCell.
    /// </summary>
    [CreateAssetMenu(fileName = "New Hex Map Terrain",
    menuName = "Hex/Map/Hex Map Terrain")]
    public class HexMapTerrain : ScriptableObject
    {
        /// <summary>
        /// An enumerated type to describe possible terrain
        /// types for HexCells.
        /// </summary>
        public enum TerrainType
        {
            Dirt, Grass, TallGrass, WetGrass, Ice,
            WhiteSand, Sand, DryDirt, Snow, ShallowWater,
            DeepWater
        }

        [Tooltip("The unique identifier for this terrain type." +
            " The index denoting what terrain type this is.")]
        [SerializeField] private int _id;

        /// <summary>
        /// The unique identifier for this terrain type. The index
        /// denoting what terrain type this is.
        /// </summary>
        public int Id { get { return _id; } }


        [Tooltip("The name of this terrain type.")]
        [SerializeField] private string _terrainName;

        /// <summary>
        /// The name of this terrain type.
        /// </summary>
        public string Name { get { return _terrainName; } }


        [Tooltip("The uv texture coordinates for this terrain type.")]
        [SerializeField] private Vector2 _uvCoordinates;

        /// <summary>
        /// The uv texture coordinates for this terrain type.
        /// </summary>
        public Vector2 UvCoordinates { get { return _uvCoordinates; } }


        [Tooltip("Denotes whether the cell is land or water. True if land, false if water.")]
        [SerializeField] private bool isLand;

        /// <summary>
        /// Denotes whether the cell is land or water. True if land, false if water.
        /// </summary>
        public bool IsLand { get { return isLand; } }

        [Tooltip("Similar variants of this type of terrain.")]
        [SerializeField] private HexMapTerrain[] _variants;

        /// <summary>
        /// Denotes whether or not this terrain type has variants.
        /// </summary>
        public bool HasVariants { get { return _variants != null && _variants.Length > 0; } }

        /// <summary>
        /// Get a random terrain variant of this HexMapTerrain type.
        /// </summary>
        /// <returns>A terrain variant of the HexMapTerrain type.</returns>
        public HexMapTerrain GetTerrainVariant()
        {
            return _variants[Random.Range(0, _variants.Length)];
        }
    }
}