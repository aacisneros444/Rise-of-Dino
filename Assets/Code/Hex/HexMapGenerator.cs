using UnityEngine;
using System.Collections.Generic;

namespace Assets.Code.Hex
{
    /// <summary>
    /// A class to model a procedural hexagonal map generator.
    /// </summary>
    public class HexMapGenerator : MonoBehaviour
    {
        [Tooltip("The scriptable object which holds map generation parameters.")]
        [SerializeField] private HexMapParameterData _parameterData;

        [Header("Size")]
        [Tooltip("The size of the map in terms of chunks.")]
        private int _mapChunkSize;

        [Header("Terrain Generation")]
        [Tooltip("The probability that the cells of a created piece of land" +
            "will diverge more from the landmass center and be more irregular.")]
        [Range(0f, 0.5f)]
        private float _jitterProbability = 0.25f;

        [Tooltip("The minimum amount of cells a generated landmass can have.")]
        [Range(30, 500)]
        private int _chunkSizeMin = 30;

        [Tooltip("The maximum amount of cells a generated landmass can have.")]
        [Range(30, 500)]
        private int _chunkSizeMax = 200;

        [Tooltip("The percentage of the map that should be covered by land.")]
        [Range(5, 95)]
        private int _landPercentage = 50;

        [Tooltip("The uniform water level of the map. A higher value means" +
            " more water submersion, and thus higher elevations to overcome the water level " +
            "and meet the specified land percentage.")]
        [Range(1, 5)]
        private int _waterLevel = 3;

        [Tooltip("The probability that the cells of a created piece of land " +
            "become an abnormal cliff / high land.")]
        [Range(0f, 1f)]
        private float _highRiseProbability = 0.25f;

        [Tooltip("The probability that the cells of a created piece of land " +
            "sink / become low land.")]
        [Range(0f, 1f)]
        private float _sinkProbability = 0.2f;

        [Tooltip("The minimum elevation a cell can have.")]
        [Range(-4f, 0f)]
        private int _elevationMinimum = -2;

        [Tooltip("The maximum elevation a cell can have.")]
        [Range(4f, 10f)]
        private int _elevationMaximum = 8;

        [Header("Map Border")]
        [Tooltip("The number of cells that will be water on the map border" +
            " (left and right sides of the map).")]
        [Range(0, 50)]
        private int _mapBorderX = 5;

        [Tooltip("The number of cells that will be water on the map border" +
            " (top and bottom side of the map).")]
        [Range(0, 50)]
        private int _mapBorderZ = 5;

        [Header("Map Divisions")]
        [Tooltip("The number of continent-like regions for the map to have.")]
        [Range(1, 4)]
        private int _regionCount = 1;

        [Tooltip("The number of cells to be water between regions. " +
            "Since both adjacent regions use this value, the true region border " +
            "is half this value. Note: when using smaller values, this does not " +
            "guarantee there will always be water between regions. Sometimes, " +
            "land bridges will form.")]
        [Range(2, 20)]
        private int _regionBorder = 10;

        [Header("Erosion")]
        [Tooltip("The percentage of erodible land that should be smoothed out. " +
            "A value of 0 means a generated map will maintain all its sharp height " +
            " differences, while a value of 100 means that it will lose them all.")]
        [Range(0, 100)]
        private int _erosionPercentage = 50;

        /// <summary>
        /// The height difference a cell needs to have to a neighbor
        /// to make the cell a candidate for erosion.
        /// </summary>
        private readonly int ErosionThreshold = 2;

        [Header("Territories")]
        [Tooltip("The prefab containing the necessary mesh rendering components " +
            "for HexTerritories.")]
        private HexTerritoryMesh _territoryMeshPrefab;

        [Tooltip("The number of territories to be created for the map.")]
        private int _territoryCount = 10;

        [Header("Terrain Type Generation")]
        [Tooltip("The perlin noise scalar value. A higher value means a more zoomed out" +
            " noise map (more variation) in biome types.")]
        private float _perlinScale = 4;

        [Tooltip("The minumum number of terrain variant chunks to create.")]
        private int _numTerrainVariantChunksMin = 50;

        [Tooltip("The maximum number of terrain variant chunks to create.")]
        private int _numTerrainVariantChunksMax = 100;

        [Tooltip("The minimum amount of cells a terrain variant chunk can have.")]
        private int _terrainVariantChunkSizeMin = 25;

        [Tooltip("The maximum amount of cells a terrain variant chunk can have.")]
        private int _terrainVariantChunkSizeMax = 75;

        /// <summary>
        /// Used to keep track of unique int id's that have been used for territories.
        /// </summary>
        private int _territoryIdCounter = 0;

        /// <summary>
        /// A struct to represent a region of a map.
        /// </summary>
        private struct MapRegion
        {
            /// <summary>
            /// Valid range for land chunk centers. These values depend
            /// on the set mapBorderX and mapBorderZ values.
            /// </summary>
            public int XMin, XMax, ZMin, ZMax;
        }

        /// <summary>
        /// The continental map regions.
        /// </summary>
        private List<MapRegion> _regions;

        [Tooltip("The perlin noise scale for river generation. A higher value means" +
        "more zoomed out noise generation, while a smaller value means a more " +
        "zoomed in noise generation.")]
        private int _riverPerlinNoiseScale = 3;

        [Tooltip("A value to raise ridged perlin noise sampling to. A higher value makes " +
            "ridges more pronounced.")]
        private int _riverWidthExponent = 4;

        /// <summary>
        /// A list where each index corresponds to a HexGrid HexCell.
        /// The values in this list represent the elevation of a cell.
        /// </summary>
        private List<int> _elevationMap;

        /// <summary>
        /// A priority queue for HexCells. Useful for various
        /// terrain generation algorithms.
        /// </summary>
        private HexCellPriorityQueue _searchFrontier;

        /// <summary>
        /// A queue of HexCells to use for generation operations.
        /// </summary>
        private Queue<HexCell> _queueFrontier;

        /// <summary>
        /// An integer denoting what search phase search operations are currently
        /// on. Using this to compare against HexCells, it can be determined if
        /// that cell has not been processed or has already been processed.
        /// <para>If a cell has the same SearchPhase as this value, it has been processed.</para>
        /// <para>If a cell has a SearchPhase less than this value, it has not been processed.</para>
        /// </summary>
        private int _searchFrontierPhase;

        /// <summary>
        /// Create the necessary data structures.
        /// </summary>
        private void Awake()
        {
            // Load parameter data.
            _mapChunkSize = _parameterData.MapChunkSize;
            _jitterProbability = _parameterData.JitterProbability;
            _chunkSizeMin = _parameterData.ChunkSizeMin;
            _chunkSizeMax = _parameterData.ChunkSizeMax;
            _landPercentage = _parameterData.LandPercentage;
            _waterLevel = _parameterData.WaterLevel;
            _highRiseProbability = _parameterData.HighRiseProbability;
            _sinkProbability = _parameterData.SinkProbability;
            _elevationMinimum = _parameterData.ElevationMinimum;
            _elevationMaximum = _parameterData.ElevationMaximum;
            _mapBorderX = _parameterData.MapBorderX;
            _mapBorderZ = _parameterData.MapBorderZ;
            _regionCount = _parameterData.RegionCount;
            _regionBorder = _parameterData.RegionBorder;
            _erosionPercentage = _parameterData.ErosionPercentage;
            _territoryMeshPrefab = _parameterData.TerritoryMeshPrefab;
            _territoryCount = _parameterData.TerritoryCount;
            _perlinScale = _parameterData.PerlinScale;
            _numTerrainVariantChunksMin = _parameterData.NumTerrainVariantChunksMin;
            _numTerrainVariantChunksMax = _parameterData.NumTerrainVariantChunksMax;
            _terrainVariantChunkSizeMin = _parameterData.TerrainVariantChunkSizeMin;
            _terrainVariantChunkSizeMax = _parameterData.TerrainVariantChunkSizeMax;
            _riverPerlinNoiseScale = _parameterData.RiverPerlinNoiseScale;
            _riverWidthExponent = _parameterData.RiverWidthExponent;

            _regions = new List<MapRegion>();
            _searchFrontier = new HexCellPriorityQueue();
            _queueFrontier = new Queue<HexCell>();
            _elevationMap = new List<int>();
        }

        /// <summary>
        /// Generate a new hex map on the given HexGrid.
        /// </summary>
        /// <param name="hexGrid">The HexGrid to generate a map on.</param>
        /// <param name="useMeshes">If true, will create meshes for 
        /// visualization. If false, will run headless.</param>>
        private void GenerateMap(HexGrid hexGrid, bool useMeshes)
        {
            hexGrid.SetGridSize(_mapChunkSize, _mapChunkSize);
            hexGrid.CreateGrid(useMeshes);

            _elevationMap.Clear();
            for (int i = 0; i < hexGrid.CellCount; i++)
            {
                _elevationMap.Add(0);
            }

            CreateRegions(hexGrid);
            CreateLand(hexGrid);
            GenerateRivers(hexGrid);
            ErodeLand(hexGrid);

            float[] heatMap = GenerateNoiseMap(hexGrid);
            SetTerrainTypes(hexGrid, heatMap);

            if (useMeshes)
            {
                hexGrid.RefreshMeshes();
            }
        }

        /// <summary>
        /// Generate a new hex map on the given HexGrid using a random seed.
        /// </summary>
        /// <param name="hexGrid">The HexGrid to generate a map on.</param>
        /// <param name="useMeshes">If true, will create meshes for 
        /// visualization. If false, will run headless.</param>>
        /// <returns>The seed set for the new map.</returns>
        public int GenerateNewMap(HexGrid hexGrid, bool useMeshes)
        {
            int seed = SetNewRandomSeed();
            GenerateMap(hexGrid, useMeshes);
            return seed;
        }

        /// <summary>
        /// Generate a new hex map using a seed.
        /// </summary>
        /// <param name="hexGrid">The HexGrid to generate a map on.</param>
        /// <param name="seed">The seed for the new map.</param>
        /// <param name="useMeshes">If true, will create meshes for 
        /// visualization. If false, will run headless.</param>>
        public void GenerateNewMap(HexGrid hexGrid, int seed, bool useMeshes)
        {
            Random.InitState(seed);
            GenerateMap(hexGrid, useMeshes);
        }

        /// <summary>
        /// Set a new random seed for the random number generator.
        /// </summary>
        /// <returns>The new seed used.</returns>
        private int SetNewRandomSeed()
        {
            int seed = Random.Range(0, int.MaxValue);
            Random.InitState(seed);
            return seed;
        }

        /// <summary>
        /// Create the desired percentage of land.
        /// </summary>
        /// <param name="hexGrid">The HexGrid to generate the map on.</param>
        private void CreateLand(HexGrid hexGrid)
        {
            // Calculate the number of cells to be converted to land.
            int landBudget = Mathf.RoundToInt(hexGrid.CellCount * _landPercentage * 0.01f);

            // Use a guard loop in case of infinite loop that occurs due to not being
            // able to raise enough cells to make the land budget 0. This can occur when
            // using a high land percentage but using a large map border, forcing a limited
            // amount of cells to meet the same quota.
            int guard = 0;
            while (landBudget != 0 && guard < 10000)
            {
                bool sink = Random.value < _sinkProbability;
                for (int i = 0; i < _regions.Count; i++)
                {
                    MapRegion region = _regions[i];
                    int chunkSize = Random.Range(_chunkSizeMin, _chunkSizeMax + 1);
                    if (sink)
                    {
                        landBudget = SinkTerrain(hexGrid, chunkSize, landBudget, region);
                    }
                    else
                    {
                        landBudget = RaiseTerrain(hexGrid, chunkSize, landBudget, region);
                        if (landBudget == 0)
                        {
                            return;
                        }
                    }
                }
                guard++;
            }
            if (landBudget > 0)
            {
                Debug.LogWarning("Failed to use up " + landBudget + " land budget.");
            }
        }

        /// <summary>
        /// Raise the elevation of a cluster of cells to create land or raise
        /// existing land.
        /// </summary> 
        /// <param name="hexGrid">The HexGrid to generate the map on.</param>
        /// <param name="chunkSize">The number of cells for the chunk of land
        /// to be raised.</param>
        /// <param name="budget">The number of cells available to be
        /// made into land.</param>
        /// <param name="region">The constrained region of the map to
        /// raise cells in.</param>
        /// <returns>An integer denoting how many cells can still be
        /// turned into land to meet the set land percentage.</returns>
        private int RaiseTerrain(HexGrid hexGrid, int chunkSize, int budget, MapRegion region)
        {
            PrepFrontierForNewSearch();

            // Start with a random cell and reset its pathfinding properties.
            // Set its SearchPhase to the current search frontier phase to mark it
            // as processed.
            HexCell firstCell = GetRandomCellInRegion(hexGrid, region);
            firstCell.SearchPhase = _searchFrontierPhase;
            firstCell.Distance = 0;
            firstCell.SearchHeuristic = 0;
            _searchFrontier.Enqueue(firstCell);
            HexCoordinates center = firstCell.Coordinates;

            int rise = Random.value < _highRiseProbability ? 2 : 1;
            int size = 0;
            while (size < chunkSize && !_searchFrontier.IsEmpty)
            {
                HexCell current = _searchFrontier.Dequeue();
                int originalElevation = _elevationMap[current.Index];
                int newElevation = originalElevation + rise;
                if (newElevation <= _elevationMaximum)
                {
                    _elevationMap[current.Index] = newElevation;
                    // If the cell just became land, decrement the land budget.
                    if (originalElevation < _waterLevel && newElevation >= _waterLevel)
                    {
                        budget--;
                    }
                    if (budget > 0)
                    {
                        size++;
                        for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
                        {
                            HexCell neighbor = current.GetNeighbor(dir);
                            if (neighbor != null && neighbor.SearchPhase < _searchFrontierPhase)
                            {
                                neighbor.SearchPhase = _searchFrontierPhase;
                                // As a cell's distance and search heuristic both affect its
                                // SearchPriority and thus priority in the search frontier,
                                // set its distance relative to the first cell to prioritize
                                // chunk growth around the center cell. Set the cell's search
                                // heuristic to 1 instead of 0 if above some threshold to add some
                                // randomness to cell priorities and thereby the chunk shape.
                                neighbor.Distance = neighbor.Coordinates.DistanceTo(center);
                                neighbor.SearchHeuristic = Random.value < _jitterProbability ? 1 : 0;
                                _searchFrontier.Enqueue(neighbor);
                            }
                        }
                    }
                }
            }
            return budget;
        }

        /// <summary>
        /// Sink a piece of land.
        /// </summary>
        /// <param name="hexGrid">The HexGrid to generate the map on.</param>
        /// <param name="chunkSize">The number of cells for the chunk of land
        /// to be lowered.</param>
        /// <param name="budget">The number of cells available to be
        /// converted to land.</param>
        /// <param name="region">The constrained region of the map to
        /// lower cells in.</param>
        /// <returns>An integer denoting how many cells can still be
        /// turned into land to meet the set land percentage.</returns>
        private int SinkTerrain(HexGrid hexGrid, int chunkSize, int budget, MapRegion region)
        {
            PrepFrontierForNewSearch();

            // Start with a random cell and reset its pathfinding properties.
            // Set its SearchPhase to the current search frontier phase to mark it
            // as processed.
            HexCell firstCell = GetRandomCellInRegion(hexGrid, region);
            firstCell.SearchPhase = _searchFrontierPhase;
            firstCell.Distance = 0;
            firstCell.SearchHeuristic = 0;
            _searchFrontier.Enqueue(firstCell);
            HexCoordinates center = firstCell.Coordinates;

            int sink = Random.value < _highRiseProbability ? 2 : 1;
            int size = 0;
            while (size < chunkSize && !_searchFrontier.IsEmpty)
            {
                HexCell current = _searchFrontier.Dequeue();
                int originalElevation = _elevationMap[current.Index];
                int newElevation = originalElevation - sink;
                if (newElevation >= _elevationMinimum)
                {
                    _elevationMap[current.Index] = newElevation;
                    // If the cell just became water, reclaim some land budget.
                    if (originalElevation >= _waterLevel && newElevation < _waterLevel)
                    {
                        budget++;
                    }
                    size++;
                    for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
                    {
                        HexCell neighbor = current.GetNeighbor(dir);
                        if (neighbor != null && neighbor.SearchPhase < _searchFrontierPhase)
                        {
                            neighbor.SearchPhase = _searchFrontierPhase;
                            // As a cell's distance and search heuristic both affect its
                            // SearchPriority and thus priority in the search frontier,
                            // set its distance relative to the first cell to prioritize
                            // chunk growth around the center cell. Set the cell's search
                            // heuristic to 1 instead of 0 if above some threshold to add some
                            // randomness to cell priorities and thereby the chunk shape.
                            neighbor.Distance = neighbor.Coordinates.DistanceTo(center);
                            neighbor.SearchHeuristic = Random.value < _jitterProbability ? 1 : 0;
                            _searchFrontier.Enqueue(neighbor);
                        }
                    }
                }
            }
            return budget;
        }

        /// <summary>
        /// Clear the search frontier and increment the searchFrontierPhase
        /// to allow all cells to be available for searching.
        /// </summary>
        private void PrepFrontierForNewSearch()
        {
            _searchFrontierPhase++;
            _searchFrontier.Clear();
            _queueFrontier.Clear();
        }

        /// <summary>
        /// Retrieve a random cell from a map region of the given HexGrid.
        /// </summary>
        /// <param name="hexGrid">The given HexGrid.</param>
        /// <param name="region">The map region to draw a random cell from.</param>
        /// <returns>A random HexCell in the given MapRegion constraints from
        /// the referenced HexGrid.</returns>
        private HexCell GetRandomCellInRegion(HexGrid hexGrid, MapRegion region)
        {
            return hexGrid.GetCell(Random.Range(region.XMin, region.XMax),
                Random.Range(region.ZMin, region.ZMax));
        }

        /// <summary>
        /// Set the terrain types of cells based on properties set during map generation.
        /// </summary>
        /// <param name="hexGrid">The HexGrid to generate the map on.</param>
        private void SetTerrainTypes(HexGrid hexGrid, float[] heatMap)
        {
            for (int i = 0; i < hexGrid.CellCount; i++)
            {
                HexCell cell = hexGrid.GetCell(i);
                if (!IsCellUnderwater(cell))
                {
                    if (heatMap[i] > 0.55f)
                    {
                        cell.TerrainType = HexMapTerrainLookup.Instance.GetHexMapTerrain(
                            (int)HexMapTerrain.TerrainType.Sand);
                    }
                    else if (heatMap[i] > 0.35f)
                    {
                        cell.TerrainType = HexMapTerrainLookup.Instance.GetHexMapTerrain(
                            (int)HexMapTerrain.TerrainType.Grass);
                    }
                    else if (heatMap[i] > 0.25f)
                    {
                        cell.TerrainType = HexMapTerrainLookup.Instance.GetHexMapTerrain(
                            (int)HexMapTerrain.TerrainType.Dirt);
                    }
                    else
                    {
                        cell.TerrainType = HexMapTerrainLookup.Instance.GetHexMapTerrain(
                            (int)HexMapTerrain.TerrainType.Snow);
                    }
                }
                else
                {
                    if (heatMap[i] > 0.15f)
                    {
                        if (_elevationMap[i] >= 1)
                        {
                            cell.TerrainType = HexMapTerrainLookup.Instance.GetHexMapTerrain(
                                (int)HexMapTerrain.TerrainType.ShallowWater);
                        }
                        else
                        {
                            cell.TerrainType = HexMapTerrainLookup.Instance.GetHexMapTerrain(
                                (int)HexMapTerrain.TerrainType.DeepWater);
                        }
                    } 
                    else
                    {
                        cell.TerrainType = HexMapTerrainLookup.Instance.GetHexMapTerrain(
                            (int)HexMapTerrain.TerrainType.Ice);
                    }
                }
            }
            SetTerrainTypeVariants(hexGrid);
        }

        /// <summary>
        /// Set terrain type variants by generating chunks of a slightly different
        /// terrain type in different areas.
        /// </summary>
        /// <param name="hexGrid">The HexGrid to generate the map on.</param>
        private void SetTerrainTypeVariants(HexGrid hexGrid)
        {
            int numVariantChunks = Random.Range(_numTerrainVariantChunksMin,
                _numTerrainVariantChunksMax);
            int guard = 0;

            int numVariantChunkTarget = numVariantChunks;
            while (numVariantChunks > 0 && guard < 500)
            {
                if (GenerateTerrainVariantChunk(hexGrid))
                {
                    numVariantChunks--;
                }
                guard++;
            }
        }

        /// <summary>
        /// Create a terrain type variant chunk.
        /// </summary>
        /// <param name="hexGrid">The hex grid to generate the map on.</param>
        /// <returns>True if successfully created a chunk, false otherwise.</returns>
        private bool GenerateTerrainVariantChunk(HexGrid hexGrid)
        {
            HexCell firstCell = GetRandomCell(hexGrid);
            if (firstCell.TerrainType.HasVariants)
            {
                PrepFrontierForNewSearch();
                // Start with a random cell and reset its pathfinding properties.
                // Set its SearchPhase to the current search frontier phase to mark it
                // as processed.
                firstCell.SearchPhase = _searchFrontierPhase;
                firstCell.Distance = 0;
                firstCell.SearchHeuristic = 0;
                _searchFrontier.Enqueue(firstCell);
                HexCoordinates center = firstCell.Coordinates;
                HexMapTerrain initialTerrainType = firstCell.TerrainType;
                HexMapTerrain terrainVariant = firstCell.TerrainType.GetTerrainVariant();

                int chunkSize = Random.Range(_terrainVariantChunkSizeMin,
                    _terrainVariantChunkSizeMax);
                int size = 0;
                while (size < chunkSize && !_searchFrontier.IsEmpty)
                {
                    HexCell current = _searchFrontier.Dequeue();
                    current.TerrainType = terrainVariant;
                    size++;
                    for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
                    {
                        HexCell neighbor = current.GetNeighbor(dir);
                        if (neighbor != null && neighbor.SearchPhase < _searchFrontierPhase)
                        {
                            // Only add cell to the frontier if its terrain type was of the
                            // first cell's terrain type or it is not water and able to obtain a 
                            // random value past a threshold. The latter condition prevents a completely
                            // flat biome border.
                            if (neighbor.TerrainType == initialTerrainType || 
                                (neighbor.TerrainType.Id != (int)HexMapTerrain.TerrainType.DeepWater && 
                                Random.value > 0.33f))
                            {
                                neighbor.SearchPhase = _searchFrontierPhase;
                                // As a cell's distance and search heuristic both affect its
                                // SearchPriority and thus priority in the search frontier,
                                // set its distance relative to the first cell to prioritize
                                // chunk growth around the center cell. Set the cell's search
                                // heuristic to 1 instead of 0 if above some threshold to add some
                                // randomness to cell priorities and thereby the chunk shape.
                                neighbor.Distance = neighbor.Coordinates.DistanceTo(center);
                                neighbor.SearchHeuristic = Random.value < _jitterProbability ? 1 : 0;
                                _searchFrontier.Enqueue(neighbor);
                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determine if a cell is underwater.
        /// </summary>
        /// <param name="cell">The HexCell.</param>
        /// <returns>True if the cell is underwater, false otherwise.</returns>
        private bool IsCellUnderwater(HexCell cell)
        {
            return _waterLevel > _elevationMap[cell.Index];
        }

        /// <summary>
        /// Create MapRegions in order to divide the map into continent-like
        /// land masses.
        /// </summary>
        /// <param name="hexGrid">The HexGrid to generate the map on.</param>
        private void CreateRegions(HexGrid hexGrid)
        {
            _regions.Clear();

            MapRegion region = new MapRegion();
            switch(_regionCount)
            {
                default:
                    region.XMin = _mapBorderX;
                    region.XMax = hexGrid.CellCountX - _mapBorderX;
                    region.ZMin = _mapBorderZ;
                    region.ZMax = hexGrid.CellCountZ - _mapBorderZ;
                    _regions.Add(region);
                    break;
                case 2:
                    if (Random.value < 0.5f)
                    {
                        region.XMin = _mapBorderX;
                        region.XMax = hexGrid.CellCountX / 2 - _regionBorder;
                        region.ZMin = _mapBorderZ;
                        region.ZMax = hexGrid.CellCountZ - _mapBorderZ;
                        _regions.Add(region);
                        region.XMin = hexGrid.CellCountX / 2 + _regionBorder;
                        region.XMax = hexGrid.CellCountX - _mapBorderX;
                        _regions.Add(region);
                    }
                    else
                    {
                        region.XMin = _mapBorderX;
                        region.XMax = hexGrid.CellCountX - _mapBorderX;
                        region.ZMin = _mapBorderZ;
                        region.ZMax = hexGrid.CellCountZ / 2 - _regionBorder;
                        _regions.Add(region);
                        region.ZMin = hexGrid.CellCountZ / 2 + _regionBorder;
                        region.ZMax = hexGrid.CellCountZ - _mapBorderZ;
                        _regions.Add(region);
                    }
                    break;
                case 3:
                    region.XMin = _mapBorderX;
                    region.XMax = hexGrid.CellCountX / 3 - _regionBorder;
                    region.ZMin = _mapBorderZ;
                    region.ZMax = hexGrid.CellCountZ - _mapBorderZ;
                    _regions.Add(region);
                    region.XMin = hexGrid.CellCountX / 3 + _regionBorder;
                    region.XMax = hexGrid.CellCountX * 2 / 3 - _regionBorder;
                    _regions.Add(region);
                    region.XMin = hexGrid.CellCountX * 2 / 3 + _regionBorder;
                    region.XMax = hexGrid.CellCountX - _mapBorderX;
                    _regions.Add(region);
                    break;
                case 4:
                    region.XMin = _mapBorderX;
                    region.XMax = hexGrid.CellCountX / 2 - _regionBorder;
                    region.ZMin = _mapBorderZ;
                    region.ZMax = hexGrid.CellCountZ / 2 - _regionBorder;
                    _regions.Add(region);
                    region.XMin = hexGrid.CellCountX / 2 + _regionBorder;
                    region.XMax = hexGrid.CellCountX - _mapBorderX;
                    _regions.Add(region);
                    region.ZMin = hexGrid.CellCountZ / 2 + _regionBorder;
                    region.ZMax = hexGrid.CellCountZ - _mapBorderZ;
                    _regions.Add(region);
                    region.XMin = _mapBorderX;
                    region.XMax = hexGrid.CellCountX / 2 - _regionBorder;
                    _regions.Add(region);
                    break;
            }
        }

        /// <summary>
        /// Apply erosion to cells that qualify. This will smooth out sharp
        /// height differences in the terrain.
        /// </summary>
        /// <param name="hexGrid">The HexGrid to generate the map on.</param>
        private void ErodeLand(HexGrid hexGrid)
        {
            // Find the total number of cells that are candidates for erosion.
            List<HexCell> erodibleCells = new List<HexCell>();
            for (int i = 0; i < hexGrid.CellCount; i++)
            {
                HexCell cell = hexGrid.GetCell(i);
                if (IsErodible(cell))
                {
                    erodibleCells.Add(cell);
                }
            }

            // Determine how many cells need to be eroded based on the set
            // erosion percentage.
            int targetErodibleCount = 
                (int)(erodibleCells.Count * (100 - _erosionPercentage) * 0.01f);

            while (erodibleCells.Count > targetErodibleCount)
            {
                // Erosion step.
                int index = Random.Range(0, erodibleCells.Count);
                HexCell cell = erodibleCells[index];
                HexCell targetCell = GetErosionTarget(cell);
                _elevationMap[cell.Index]--;
                _elevationMap[targetCell.Index]++;

                // Remove cell if it is no longer erodable after the erosion step.
                if (!IsErodible(cell))
                {
                    // Swap current cell with with the last in the list and remove
                    // to avoid removing in the middle of a list. Processing order
                    // doesn't matter.
                    erodibleCells[index] = erodibleCells[erodibleCells.Count - 1];
                    erodibleCells.RemoveAt(erodibleCells.Count - 1);
                }

                // Cycle through cell neighbors, as the erosion step may have caused them
                // to now be erodable. If they are, add them to the erodible cells list.
                for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
                {
                    HexCell neighbor = cell.GetNeighbor(dir);
                    if(neighbor != null && 
                        _elevationMap[neighbor.Index] == _elevationMap[cell.Index] + ErosionThreshold &&
                        !erodibleCells.Contains(neighbor))
                    {
                        erodibleCells.Add(neighbor);
                    }
                }

                // Check to see if target cell is a candidate for erosion. If so,
                // add it to the erodible cells list. It is necessary to check this
                // here as the loop above only checks if a neighbor just became a cliff
                // relative to the eroded cell. The target cell's elevation increased,
                // so a call to IsErodible is necessary, as its neighbors may now meet
                // the erosion threshold.
                if (IsErodible(targetCell) && !erodibleCells.Contains(targetCell))
                {
                    erodibleCells.Add(targetCell);
                }

                // Cycle through target cell neighbors, as the erosion step may have caused them
                // to no longer be erodable if they already were. If they are not anymore, remove them
                // from the erodible cells list.
                for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
                {
                    HexCell neighbor = targetCell.GetNeighbor(dir);
                    if (neighbor != null && neighbor != cell &&
                        _elevationMap[neighbor.Index] == _elevationMap[targetCell.Index] + 1 &&
                        !IsErodible(neighbor))
                    {
                        erodibleCells.Remove(neighbor);
                    }
                }
            }
        }

        /// <summary>
        /// Determine if a cell is a candidate for erosion.
        /// </summary>
        /// <param name="cell">The HexCell.</param>
        /// <returns>True if erodable, false otherwise.</returns>
        private bool IsErodible(HexCell cell)
        {
            int erodibleElevation = _elevationMap[cell.Index] - ErosionThreshold;
            for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
            {
                HexCell neighbor = cell.GetNeighbor(dir);
                if (neighbor != null && _elevationMap[neighbor.Index] <= erodibleElevation)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Find an "erosion target" given an erodible HexCell. The given
        /// HexCell will be lowered in ErodeLand, but, in order to preserve
        /// land mass, one if its neighbors will get an increase in elevation.
        /// This is the erosion target.
        /// </summary>
        /// <param name="cell">The given HexCell.</param>
        /// <returns>A HexCell erosion target.</returns>
        private HexCell GetErosionTarget(HexCell cell)
        {
            List<HexCell> candidates = new List<HexCell>();
            int erodibleElevation = _elevationMap[cell.Index] - ErosionThreshold;
            for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
            {
                HexCell neighbor = cell.GetNeighbor(dir);
                if (neighbor != null && _elevationMap[neighbor.Index] <= erodibleElevation)
                {
                    candidates.Add(neighbor);
                }
            }
            HexCell target = candidates[Random.Range(0, candidates.Count)];
            return target;
        }

        /// <summary>
        /// Retrieve a random cell from the given HexGrid.
        /// </summary>
        /// <param name="hexGrid">The given HexGrid.</param>
        /// <returns>A random HexCell from the given HexGrid.</returns>
        private HexCell GetRandomCell(HexGrid hexGrid)
        {
            return hexGrid.GetCell(Random.Range(0, hexGrid.CellCount));
        }

        /// <summary>
        /// Create territorial divisions of the map for player ownership.
        /// </summary>
        /// <param name="hexGrid">The HexGrid to generate territories on.</param>
        public List<HexTerritory> CreateTerritories(HexGrid hexGrid, bool useMeshes)
        {
            _territoryIdCounter = 0;

            PrepFrontierForNewSearch();

            // A list of all the HexTerritories generated to return if needed.
            List<HexTerritory> hexTerritories = new List<HexTerritory>();

            // Create territories and searchFrontiers for each
            // and store them by territory id.
            Dictionary<int, HexTerritory> generatingTerritories = 
                new Dictionary<int, HexTerritory>();
            Dictionary<int, HexCellPriorityQueue> searchFrontiers = 
                new Dictionary<int, HexCellPriorityQueue>();

            // Create the necessary HexTerritories and HexCellPriorityQueues.
            // Add a capital cell to each territory and mark it as processed.
            int capitalsChosenSoFar = 0;
            while (capitalsChosenSoFar < _territoryCount)
            {
                HexCell capital = GetRandomCell(hexGrid);
                // Ensure capital cell has not already been chosen.
                if (capital.Territory == null)
                {
                    capitalsChosenSoFar++;
                    capital.SearchPhase = _searchFrontierPhase;
                    int territoryId = _territoryIdCounter++;
                    HexTerritory territory;
                    if (useMeshes)
                    {
                        territory = new HexTerritory(territoryId, capital,
                            Instantiate(_territoryMeshPrefab, transform));
                    }
                    else
                    {
                        territory = new HexTerritory(territoryId, capital);
                    }
                    generatingTerritories[territoryId] = territory;
                    hexTerritories.Add(territory);
                    searchFrontiers[territoryId] = new HexCellPriorityQueue();
                    searchFrontiers[territoryId].Enqueue(capital);
                }
            }

            // Expand territories in parallel using a breadth-first search until
            // the entire map is covered.
            List<int> toRemove = new List<int>();
            while (generatingTerritories.Count > 0)
            {
                foreach (int key in generatingTerritories.Keys)
                {
                    AdvanceTerritoryGeneration(generatingTerritories[key], searchFrontiers[key]);
                    // Territory expanded as much as possible, mark to stop generating.
                    if (searchFrontiers[key].IsEmpty)
                    {
                        toRemove.Add(key);
                    }
                }
                if (toRemove.Count > 0)
                {
                    for (int i = 0; i < toRemove.Count; i++)
                    {
                        int key = toRemove[i];
                        HexTerritory removingTerritory = generatingTerritories[key];
                        if (useMeshes)
                        {
                            removingTerritory.RefreshMesh(false);
                        }
                        removingTerritory.PickInitialUnitTypeToSpawn();
                        generatingTerritories.Remove(key);
                    }
                    toRemove.Clear();
                }
            }

            hexGrid.ResetAllSearchPhases();

            return hexTerritories;
        }

        /// <summary>
        /// Advance territory generation for a territory by one step.
        /// </summary>
        /// <param name="territory">The territory to expand.</param>
        /// <param name="searchFrontier">The territory's search frontier.</param>
        private void AdvanceTerritoryGeneration(HexTerritory territory,
            HexCellPriorityQueue searchFrontier)
        {
            HexCell current = searchFrontier.Dequeue();
            bool markedAsBorderCell = false;
            for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
            {
                HexCell neighbor = current.GetNeighbor(dir);
                if (neighbor != null)
                {
                    if (neighbor.SearchPhase < _searchFrontierPhase)
                    {
                        // Neighbor has not been searched, add to current territory to expand.
                        neighbor.Territory = territory;
                        territory.AddMemberCell(neighbor);

                        neighbor.SearchPhase = _searchFrontierPhase;
                        // As a cell's distance affects its SearchPriority and thus priority
                        // in the search frontier, set its distance relative to the first cell
                        // to prioritize territory growth around the center cell.
                        neighbor.Distance = neighbor.Coordinates.DistanceTo(territory.Capital.Coordinates);
                        searchFrontier.Enqueue(neighbor);
                    }
                    else if (!markedAsBorderCell && (neighbor.Territory.Id != current.Territory.Id))
                    {
                        // Neighbor has been searched and assigned a territory, mark current cell
                        // as a border cell within its own territory if not already.
                        territory.AddBorderCell(current);
                        markedAsBorderCell = true;
                    }
                }
            }
        }

        /// <summary>
        /// Generate a noise map.
        /// </summary>
        /// <param name="hexGrid">The HexGrid to generate the map on.</param>
        /// <return>A float array noise map.</return>
        private float[] GenerateNoiseMap(HexGrid hexGrid)
        {
            float[] noiseMap = new float[hexGrid.CellCount];

            // Generate a random integer to offset the perlin sampling.
            int randomMapOffset = Random.Range(0, 10000);

            for (int i = 0; i < hexGrid.CellCount; i++)
            {
                // Get the cell's offset coordinates from original generation.
                int xOffsetCoord = i % hexGrid.CellCountX;
                int zOffsetCoord = i / hexGrid.CellCountZ;

                // Get the cell's perlin sampling coordinates.
                float xSampleCoord = (float)xOffsetCoord / hexGrid.CellCountX * 
                    _perlinScale + randomMapOffset;
                float ySampleCoord = (float)zOffsetCoord / hexGrid.CellCountZ * 
                    _perlinScale + randomMapOffset;

                float sample = Mathf.PerlinNoise(xSampleCoord, ySampleCoord);
                noiseMap[i] = sample;
            }

            return noiseMap;
        }

        /// <summary>
        /// Generate rivers on the map.
        /// </summary>
        /// <param name="hexGrid">The HexGrid to generate the map on.</param>
        private void GenerateRivers(HexGrid hexGrid)
        {
            float[] ridgedNoiseMap = GenerateRidgedNoiseMap(hexGrid);
            for (int i = 0; i < ridgedNoiseMap.Length; i++)
            {
                if (!IsCellUnderwater(hexGrid.GetCell(i)))
                {
                    float noiseValue = ridgedNoiseMap[i];
                    if (noiseValue > 0.7f)
                    {
                        // Set the corresponding cell to deep water.
                        _elevationMap[i] = 0;
                    }
                    else if (noiseValue > 0.5f)
                    {
                        // Set the corresponding cell to shallow water.
                        _elevationMap[i] = _waterLevel - 1;
                    }
                }
            }
        }

        /// <summary>
        /// Generate a ridged noise map.
        /// </summary>
        /// <param name="hexGrid">The HexGrid to generate the map on.</param>
        /// <return>A float array ridged noise map.</return>
        private float[] GenerateRidgedNoiseMap(HexGrid hexGrid)
        {
            float[] noiseMap = new float[hexGrid.CellCount];

            // Generate a random integer to offset the perlin sampling.
            int randomMapOffset = Random.Range(0, 10000);

            for (int i = 0; i < hexGrid.CellCount; i++)
            {
                // Get the cell's offset coordinates from original generation.
                int xOffsetCoord = i % hexGrid.CellCountX;
                int zOffsetCoord = i / hexGrid.CellCountZ;

                // Get the cell's perlin sampling coordinates.
                float xSampleCoord = (float)xOffsetCoord / hexGrid.CellCountX *
                    _riverPerlinNoiseScale + randomMapOffset;
                float ySampleCoord = (float)zOffsetCoord / hexGrid.CellCountZ *
                    _riverPerlinNoiseScale + randomMapOffset;

                float sample = Mathf.Pow(
                    1f - Mathf.Abs(2 * (Mathf.PerlinNoise(xSampleCoord, ySampleCoord) - 0.5f)),
                    _riverWidthExponent);
                noiseMap[i] = sample;
            }

            return noiseMap;
        }
    }
}