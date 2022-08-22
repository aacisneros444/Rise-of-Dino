using UnityEngine;
using Assets.Code.Units;

namespace Assets.Code.Hex
{
    /// <summary>
    /// A class to model a hexagonal grid cell.
    /// </summary>
    public class HexCell
    {
        /// <summary>
        /// The index of this cell in its HexGrid.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// The position of this HexCell in world space.
        /// </summary>
        public Vector3 Position { get; private set; }

        /// <summary>
        /// The chunk this HexCell belongs to (if using chunks).
        /// </summary>
        public HexGridChunk Chunk { get; set; }

        /// <summary>
        /// The index of this cell in its chunk (if using chunks).
        /// </summary>
        public int LocalChunkIndex { get; set; }

        /// <summary>
        /// The y axis adjustment for the VisualPosition property.
        /// </summary>
        private static readonly Vector3 VisualOffset = new Vector3(0f, 0.25f, 0f);

        /// <summary>
        /// The position of the HexCell in world space with y axis adjustment
        /// for visuals.
        /// </summary>
        public Vector3 VisualPosition { get { return Position + VisualOffset; } }

        /// <summary>
        /// The cube coordinates for this HexCell.
        /// </summary>
        public HexCoordinates Coordinates { get; private set; }

        /// <summary>
        /// An adjacency list that stores all neighboring HexCells.
        /// A neighbor in a certain direction can be accessed using a 
        /// HexDirection for its index.
        /// </summary>
        private HexCell[] _neighbors;

        /// <summary>
        /// Number of neighbors (6 for hexagon).
        /// </summary>
        private const int NumNeighbors = 6;

        /// <summary>
        /// A pathfinding property which denotes the distance of this cell
        /// from a starting cell.
        /// </summary>
        public int Distance { get; set; }

        /// <summary>
        /// A pathfinding property which denotes an estimate of the remaining
        /// distance to a target cell. Makes for more efficient pathfinding.
        /// </summary>
        public int SearchHeuristic { get; set; }

        /// <summary>
        /// An integer representing the search priority of this cell in a HexCellPriorityQueue.
        /// This is the sum of its distance from a starting cell and its distance from
        /// the target cell.
        /// </summary>
        public int SearchPriority { get { return Distance + SearchHeuristic; } }

        /// <summary>
        /// A pathfinding property which denotes whether this cell has not been processed,
        /// is being processed, or has already been processed.
        /// </summary>
        public int SearchPhase { get; set; }

        /// <summary>
        /// A pathfinding property used in HexCellPriorityQueues. This creates a linked
        /// list of HexCells possessing the same search priority.
        /// </summary>
        public HexCell NextWithSamePriority { get; set; }

        /// <summary>
        /// A pathfinding property. The previous cell in a path.
        /// </summary>
        public HexCell PrevInPath { get; set; }

        /// <summary>
        /// The terrain type of this HexCell.
        /// </summary>
        public HexMapTerrain TerrainType { get; set; }

        /// <summary>
        /// The HexTerritory this cell belongs to.
        /// </summary>
        public HexTerritory Territory { get; set; }

        /// <summary>
        /// The unit, if any, occupying this cell. Will be
        /// null if no unit occupies the cell.
        /// </summary>
        public Unit Unit { get; set; }

        /// <summary>
        /// The backing field for the Visibility property.
        /// </summary>
        private int _visibility;

        /// <summary>
        /// Denotes whether of not the cell is "visible." If 
        /// this value is 0, the cell is not visible. Any value > 0, 
        /// means the cell is visible, and will not be occluded by
        /// fog of war on the client.
        /// </summary>
        public int Visibility 
        {
            get { return _visibility; }
        }

        /// <summary>
        /// Denotes whether or not this cell and its contents are 
        /// currently visible (fog of war).
        /// </summary>
        public bool IsVisible { get { return _visibility > 0; } }

        /// <summary>
        /// Denotes whether or not this cell has been seen at least once.
        /// </summary>
        public bool Explored { get; private set; }

        /// <summary>
        /// Create a new HexCell.
        /// </summary>
        /// <param name="position">The HexCell's world space position.</param>
        /// <param name="coordinates">The HexCell's cube coordinates.</param>
        /// <param name="index">The HexCell's grid index.</param>
        public HexCell(Vector3 position, HexCoordinates coordinates, int index)
        {
            Position = position;
            Coordinates = coordinates;
            Index = index;
            _neighbors = new HexCell[NumNeighbors];
            TerrainType = HexMapTerrainLookup.Instance.GetHexMapTerrain(
                (int)HexMapTerrain.TerrainType.Sand);
        }

        /// <summary>
        /// Set a HexCell neighbor in a HexDirection. Also, set that neighbor's
        /// neighbor as this cell in the opposite of the given HexDirection.
        /// </summary>
        /// <param name="direction">The HexDirection the neighbor is in.</param>
        /// <param name="cell">The neighboring HexCell.</param>
        public void SetNeighbor(HexDirection direction, HexCell cell)
        {
            _neighbors[(int)direction] = cell;
            cell._neighbors[(int)direction.Opposite()] = this;
        }

        /// <summary>
        /// Get a neighboring cell in a HexDirection.
        /// </summary>
        /// <param name="direction">The HexDirection the neighbor is in.</param>
        /// <returns>The neighboring cell in the given HexDirection.</returns>
        public HexCell GetNeighbor(HexDirection direction)
        {
            return _neighbors[(int)direction];
        }

        /// <summary>
        /// Update the visibility of this HexCell.
        /// </summary>
        /// <param name="value">The new visibility value.</param>
        /// <returns>True if this cell just became invisible or just
        /// became visible.</returns>
        public bool UpdateVisibility(int value)
        {
            int oldValue = _visibility;
            _visibility = value >= 0 ? value : 0;

            // Check to see if cell just became invisible or just became visible.
            bool visibilityChangedStates = (oldValue == 0 && _visibility != 0) ||
                (oldValue != 0 && _visibility == 0);
            if (visibilityChangedStates)
            {
                if (IsVisible)
                {
                    Explored = true;
                }
                Chunk.HexFogMesh.UpdateFogAtCell(this);
                if (Unit != null)
                {
                    LateUnitVisualVisibilityUpdater.Instance.Enqueue(Unit);
                }
            }
            return visibilityChangedStates;
        }

        /// <summary>
        /// Determine if a given unit can travel on this cell.
        /// </summary>
        /// <param name="unit">The given unit.</param>
        /// <returns>True if the unit can travel on this cell, false otherwise.</returns>
        public bool CanUnitTravelOn(Unit unit)
        {
            return (TerrainType.IsLand && unit.Data.IsWalker) ||
                    (!TerrainType.IsLand && unit.Data.IsSwimmer) || 
                    TerrainType.Id == (int)HexMapTerrain.TerrainType.ShallowWater;
        }
    }
}