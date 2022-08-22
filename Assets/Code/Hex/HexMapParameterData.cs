using UnityEngine;
using Assets.Code.Hex;

/// <summary>
/// A scriptable object to hold world generation parameters for
/// the HexMapGenerator class.
/// </summary>
[CreateAssetMenu(fileName = "New Hex Map Parameter Data",
menuName = "Hex/Map/Hex Map Parameter Data")]
public class HexMapParameterData : ScriptableObject
{
    [Header("Size")]
    [Tooltip("The size of the map in terms of chunks.")]
    [SerializeField] private int _mapChunkSize = 4;
    public int MapChunkSize { get { return _mapChunkSize; } }

    [Header("Terrain Generation")]
    [Tooltip("The probability that the cells of a created piece of land" +
            "will diverge more from the landmass center and be more irregular.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float _jitterProbability = 0.25f;
    public float JitterProbability { get { return _jitterProbability; } }

    [Tooltip("The minimum amount of cells a generated landmass can have.")]
    [Range(30, 500)]
    [SerializeField] private int _chunkSizeMin = 30;
    public int ChunkSizeMin { get { return _chunkSizeMin; } }

    [Tooltip("The maximum amount of cells a generated landmass can have.")]
    [Range(30, 500)]
    [SerializeField] private int _chunkSizeMax = 200;
    public int ChunkSizeMax { get { return _chunkSizeMax; } }

    [Tooltip("The percentage of the map that should be covered by land.")]
    [Range(5, 95)]
    [SerializeField] private int _landPercentage = 50;
    public int LandPercentage { get { return _landPercentage; } }

    [Tooltip("The uniform water level of the map. A higher value means" +
        " more water submersion, and thus higher elevations to overcome the water level " +
        "and meet the specified land percentage.")]
    [Range(1, 5)]
    [SerializeField] private int _waterLevel = 3;
    public int WaterLevel { get { return _waterLevel; } }

    [Tooltip("The probability that the cells of a created piece of land " +
        "become an abnormal cliff / high land.")]
    [Range(0f, 1f)]
    [SerializeField] private float _highRiseProbability = 0.25f;
    public float HighRiseProbability { get { return _highRiseProbability; } }

    [Tooltip("The probability that the cells of a created piece of land " +
        "sink / become low land.")]
    [Range(0f, 1f)]
    [SerializeField] private float _sinkProbability = 0.2f;
    public float SinkProbability { get { return _sinkProbability; } }

    [Tooltip("The minimum elevation a cell can have.")]
    [Range(-4f, 0f)]
    [SerializeField] private int _elevationMinimum = -2;
    public int ElevationMinimum { get { return _elevationMinimum; } }

    [Tooltip("The maximum elevation a cell can have.")]
    [Range(4f, 10f)]
    [SerializeField] private int _elevationMaximum = 8;
    public int ElevationMaximum { get { return _elevationMaximum; } }

    [Header("Map Border")]
    [Tooltip("The number of cells that will be water on the map border" +
        " (left and right sides of the map).")]
    [Range(0, 50)]
    [SerializeField] private int _mapBorderX = 5;
    public int MapBorderX { get { return _mapBorderX; } }

    [Tooltip("The number of cells that will be water on the map border" +
        " (top and bottom side of the map).")]
    [Range(0, 50)]
    [SerializeField] private int _mapBorderZ = 5;
    public int MapBorderZ { get { return _mapBorderZ; } }

    [Header("Map Divisions")]
    [Tooltip("The number of continent-like regions for the map to have.")]
    [Range(1, 4)]
    [SerializeField] private int _regionCount = 1;
    public int RegionCount { get { return _regionCount; } }

    [Tooltip("The number of cells to be water between regions. " +
        "Since both adjacent regions use this value, the true region border " +
        "is half this value. Note: when using smaller values, this does not " +
        "guarantee there will always be water between regions. Sometimes, " +
        "land bridges will form.")]
    [Range(2, 20)]
    [SerializeField] private int _regionBorder = 10;
    public int RegionBorder { get { return _regionBorder; } }

    [Header("Erosion")]
    [Tooltip("The percentage of erodible land that should be smoothed out. " +
        "A value of 0 means a generated map will maintain all its sharp height " +
        " differences, while a value of 100 means that it will lose them all.")]
    [Range(0, 100)]
    [SerializeField] private int _erosionPercentage = 50;
    public int ErosionPercentage { get { return _erosionPercentage; } }

    [Header("Territories")]
    [Tooltip("The prefab containing the necessary mesh rendering components " +
            "for HexTerritories.")]
    [SerializeField] private HexTerritoryMesh _territoryMeshPrefab;
    public HexTerritoryMesh TerritoryMeshPrefab { get { return _territoryMeshPrefab; } }

    [Tooltip("The number of territories to be created for the map.")]
    [SerializeField] private int _territoryCount = 10;
    public int TerritoryCount { get { return _territoryCount; } }

    [Header("Terrain Type Generation")]
    [Tooltip("The perlin noise scalar value. A higher value means a more zoomed out" +
        " noise map (more variation) in biome types.")]
    [SerializeField] private float _perlinScale = 4;
    public float PerlinScale { get { return _perlinScale; } }

    [Tooltip("The minumum number of terrain variant chunks to create.")]
    [SerializeField] private int _numTerrainVariantChunksMin = 50;
    public int NumTerrainVariantChunksMin { get { return _numTerrainVariantChunksMin; } }

    [Tooltip("The maximum number of terrain variant chunks to create.")]
    [SerializeField] private int _numTerrainVariantChunksMax = 100;
    public int NumTerrainVariantChunksMax { get { return _numTerrainVariantChunksMax; } }

    [Tooltip("The minimum amount of cells a terrain variant chunk can have.")]
    [SerializeField] private int _terrainVariantChunkSizeMin = 25;
    public int TerrainVariantChunkSizeMin { get { return _terrainVariantChunkSizeMin; } }

    [Tooltip("The maximum amount of cells a terrain variant chunk can have.")]
    [SerializeField] private int _terrainVariantChunkSizeMax = 75;
    public int TerrainVariantChunkSizeMax { get { return _terrainVariantChunkSizeMax; } }

    [Header("River Generation")]
    [Tooltip("The perlin noise scale for river generation. A higher value means" +
    "more zoomed out noise generation, while a smaller value means a more " +
    "zoomed in noise generation.")]
    [SerializeField] private int _riverPerlinNoiseScale = 3;
    public int RiverPerlinNoiseScale { get { return _riverPerlinNoiseScale; } }

    [Tooltip("A value to raise ridged perlin noise sampling to. A higher value " +
        "makes ridges more pronounced.")]
    [SerializeField] private int _riverWidthExponent = 4;
    public int RiverWidthExponent { get { return _riverWidthExponent; } }
}
