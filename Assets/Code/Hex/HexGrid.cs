using UnityEngine;

namespace Assets.Code.Hex
{
    /// <summary>
    /// A class to model a grid of hexagonal cells.
    /// </summary>
    public class HexGrid : MonoBehaviour
    {
        /// <summary>
        /// A prefab for grid chunks.
        /// </summary>
        [SerializeField] private HexGridChunk _hexGridChunkPrefab;

        /// <summary>
        /// Number of chunks of hexagonal cells in X and Z directions.
        /// </summary>
        private int _chunkCountX = 4, _chunkCountZ = 4;

        /// <summary>
        /// The width and height of a chunk of hexagonal cells in a hex
        /// grid. For example, if 16, the chunk will have (16 * 16) 256 cells.
        /// </summary>
        public const int ChunkSize = 32;

        /// <summary>
        /// Width (X) and Height (Z) of grid.
        /// The number of cells in the X and Z directions.
        /// </summary>
        [HideInInspector]
        public int CellCountX, CellCountZ;

        /// <summary>
        /// The number of cells in the HexGrid.
        /// </summary>
        public int CellCount { get; private set; }

        /// <summary>
        /// The grid cells.
        /// </summary>
        private HexCell[] _cells;

        /// <summary>
        /// The grid chunks.
        /// </summary>
        private HexGridChunk[] _chunks;

        /// <summary>
        /// Denotes whether the HexGrid has been generated or not.
        /// </summary>
        public bool HasGenerated { get; private set; }

        /// <summary>
        /// Set the HexGrid size in terms of chunks.
        /// </summary>
        public void SetGridSize(int chunkCountX, int chunkCountZ)
        {
            _chunkCountX = chunkCountX;
            _chunkCountZ = chunkCountZ;
            CellCountX = _chunkCountX * ChunkSize;
            CellCountZ = _chunkCountZ * ChunkSize;
            CellCount = CellCountX * CellCountZ;
        }

        /// <summary>
        /// Create the HexGrid.
        /// </summary>
        /// <param name="useChunks">Denotes whether to assign cells to chunks.
        /// Use true for mesh visualization, false for headless grid.</param>
        public void CreateGrid(bool useChunks)
        {
            if (useChunks)
            {
                CreateChunks();
            }
            CreateCells();
            HasGenerated = true;
        }

        /// <summary>
        /// Create chunks of hexagonal cells.
        /// </summary>
        private void CreateChunks()
        {
            _chunks = new HexGridChunk[_chunkCountX * _chunkCountZ];

            for (int z = 0, i = 0; z < _chunkCountZ; z++)
            {
                for (int x = 0; x < _chunkCountX; x++, i++)
                {
                    _chunks[i] = Instantiate(_hexGridChunkPrefab, transform);
                }
            }
        }

        /// <summary>
        /// Create the grid cells.
        /// </summary>
        private void CreateCells()
        {
            _cells = new HexCell[CellCountZ * CellCountX];

            for (int z = 0, i = 0; z < CellCountZ; z++)
            {
                for (int x = 0; x < CellCountX; x++, i++)
                {
                    CreateCell(x, z, i);
                }
            }
        }

        /// <summary>
        /// Create a single HexCell.
        /// </summary>
        /// <param name="x">The cell's x position in the grid (x offset coordinate).</param>
        /// <param name="z">The cell's z position in the grid (z offset coordinate).</param>
        /// <param name="i">The cell's grid index.</param>
        private void CreateCell(int x, int z, int i)
        {
            // Add half of z minus the integer division of z divided by 2 to x to ensure
            // rows are shifted back and forth depending on even or odd row (z) number.
            // 0 if even, 0.5f if odd.
            float xShift = z * 0.5f - z / 2;
            Vector3 worldPosition = new Vector3
            {
                x = (x + xShift) * HexMetrics.HorizontalDistanceToNeighbor,
                z = z * HexMetrics.VerticalDistanceToNeighbor
            };

            HexCell cell = new HexCell(worldPosition,
                HexCoordinates.FromOffsetCoordinates(x, z), i);
            SetCellNeighbors(cell, x, z, i);
            _cells[i] = cell;

            // Check to see if using chunks, assign to respective chunk if so.
            if (_chunks != null)
            {
                AssignCellToChunk(cell, x, z);
            }
        }

        /// <summary>
        /// Set the adjacency list of HexCell neighbors in a HexCell.
        /// Note: Sets neighbors in western and southern directions. The 
        /// SetNeighbor method in HexCell takes care of the opposite directions.
        /// </summary>
        /// <param name="cell">The HexCell.</param>
        /// <param name="x">The cell's x position in the grid (x offset coordinate).</param>
        /// <param name="z">The cell's z position in the grid (z offset coordinate).</param>
        /// <param name="i">The cell's grid index.</param>
        private void SetCellNeighbors(HexCell cell, int x, int z, int i)
        {
            // if not first cell in row, the cell has a western neighbor at index i - 1.
            if (x > 0)
            {
                cell.SetNeighbor(HexDirection.W, _cells[i - 1]);
            }
            // if not in the first row, the cell has other connections
            if (z > 0)
            {
                // if row is even, the cell has a southeastern neighbor at index i - Width
                if (z % 2 == 0)
                {
                    cell.SetNeighbor(HexDirection.SE, _cells[i - CellCountX]);
                    // if not first cell in row, the cell has a southwestern neighbor at index i - Width - 1
                    if (x > 0)
                    {
                        cell.SetNeighbor(HexDirection.SW, _cells[i - CellCountX - 1]);
                    }
                }
                else
                {
                    // if row is odd, the cell has a southwestern neighbor at index i - Width
                    cell.SetNeighbor(HexDirection.SW, _cells[i - CellCountX]);
                    if (x < CellCountX - 1)
                    {
                        // if cell is not last in the odd row, it has a southeastern neighbor at index
                        // i - Width + 1
                        cell.SetNeighbor(HexDirection.SE, _cells[i - CellCountX + 1]);
                    }
                }
            }
        }

        /// <summary>
        /// Assign a grid cell to a HexGridChunk.
        /// </summary>
        /// <param name="cell">The HexCell.</param>
        /// <param name="x">The cell's x position in the grid (x offset coordinate).</param>
        /// <param name="z">The cell's z position in the grid (z offset coordinate).</param>
        private void AssignCellToChunk(HexCell cell, int x, int z)
        {
            // Dividing x and z by the ChunkSize defines which chunk (x, z)
            // the cell belongs to.
            int chunkX = x / ChunkSize;
            int chunkZ = z / ChunkSize;

            // To get the index for the chunk, add chunkX (how far down the chunk is in a row)
            // + chunkZ * ChunkCountX to account for number of chunks in previous rows.
            HexGridChunk chunk = _chunks[chunkX + chunkZ * _chunkCountX];

            // Find the "local" index of the cell within the chunk.
            int localX = x - chunkX * ChunkSize;
            int localZ = z - chunkZ * ChunkSize;

            // Assign the cell to the chunk by obtaining its local chunk index by again
            // adding the X component plus the Z component multiplied by the number
            // of cells per row to account of previous cells.
            chunk.AssignCell(localX + localZ * ChunkSize, cell);
        }

        /// <summary>
        /// Refresh all the HexGridChunk meshes.
        /// </summary>
        public void RefreshMeshes()
        {
            for (int i = 0; i < _chunks.Length; i++)
            {
                _chunks[i].RefreshMesh();
                _chunks[i].CreateHexFogMesh();
            }
        }

        /// <summary>
        /// Get the index of a cell from a world space position.
        /// </summary>
        /// <param name="position">The world space position.</param>
        /// <returns>The index of the cell at the given world space position.</returns>
        private int GetIndexFromPosition(Vector3 position)
        {
            HexCoordinates coordinates = HexCoordinates.FromPosition(position);
            // Add X for how far down the cell is in a row + (Z * Width (which is _cellCountX))
            // to account for number of cells in previous rows. Finally add the integer division
            // of Z / 2 to account for horizontal shifting every two rows.
            return coordinates.X + coordinates.Z * CellCountX + coordinates.Z / 2;
        }

        /// <summary>
        /// Get a HexCell at a world space position.
        /// </summary>
        /// <param name="position">The world space position.</param>
        /// <returns>The HexCell at the given world space position.</returns>
        public HexCell GetCellAtPosition(Vector3 position)
        {
            if (_cells == null)
            {
                throw new System.InvalidOperationException("HexGrid must be generated.");
            }
            int cellIndex = GetIndexFromPosition(position);
            if (cellIndex >= 0 && cellIndex < _cells.Length)
            {
                return _cells[cellIndex];
            }
            return null;
        }

        /// <summary>
        /// Get a HexCell from its grid index.
        /// </summary>
        /// <param name = "index">The cell's grid index.</param>
        /// <returns>The HexCell at the given grid index.</returns>
        public HexCell GetCell(int index)
        {
            return _cells[index];
        }

        /// <summary>
        /// Get a HexCell from its offset coordinates.
        /// </summary>
        /// <param name="xOffset">The x offset coordinate.</param>
        /// <param name="zOffset">The z offset coordinate</param>
        /// <returns>The HexCell for the given offset coordinates</returns>
        public HexCell GetCell(int xOffset, int zOffset)
        {
            return _cells[xOffset + zOffset * CellCountX];
        }

        /// <summary>
        /// Reset the search phases of all HexCells.
        /// </summary>
        public void ResetAllSearchPhases()
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i].SearchPhase = 0;
            }
        }
    }
}