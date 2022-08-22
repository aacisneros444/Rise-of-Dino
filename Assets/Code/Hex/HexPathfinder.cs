using UnityEngine;
using System.Collections.Generic;
using Assets.Code.Units;

namespace Assets.Code.Hex
{
    /// <summary>
    /// A singleton class to conduct pathfinding operations on a HexGrid.
    /// </summary>
    public class HexPathfinder : MonoBehaviour
    {
        /// <summary>
        /// The singleton instance of the HexPathfinder.
        /// </summary>
        public static HexPathfinder Instance { get; private set; }

        /// <summary>
        /// A priority queue of HexCells used for A*.
        /// </summary>
        private HexCellPriorityQueue _searchFrontier;

        /// <summary>
        /// A regular queue of HexCells used for pathfinding operations.
        /// </summary>
        private Queue<HexCell> _queueFrontier;

        /// <summary>
        /// An integer denoting what search phase the pathfinder is currently
        /// on. Using this to compare against HexCells, it can be determined if
        /// that cell has not been processed, is being processed, or has already been
        /// processed.
        /// <para>If a cell has a SearchPhase of one more than this value, it has been processed.</para>
        /// <para>If a cell has the same SearchPhase as this value, it is being processed.</para>
        /// <para>If a cell has a SearchPhase less than this value, it has not been processed.</para>
        /// </summary>
        private int _searchFrontierPhase;

        /// <summary>
        /// The amount to add to the searchFrontierPhase to bring all cells
        /// into the available open set (candidates for searching).
        /// </summary>
        private const int NewSearchPhaseAddAmount = 2;

        /// <summary>
        /// Create the search frontier. Set this as a singleton.
        /// </summary>
        private void Awake()
        {
            Instance = this;
            _searchFrontier = new HexCellPriorityQueue();
            _queueFrontier = new Queue<HexCell>();
        }

        /// <summary>
        /// Find the shortest path of HexCells from a starting cell to a target cell.
        /// </summary>
        /// <param name="fromCell">The starting cell.</param>
        /// <param name="toCell">The target cell.</param>
        /// <returns>A list of HexCells, the shortest path from fromCell to toCell.
        /// Null if no path can be found.</returns>
        public List<HexCell> FindPath(HexCell fromCell, HexCell toCell)
        {
            if (fromCell == null || toCell == null)
            {
                //throw new System.ArgumentException("fromCell and toCell must not be null.");
                Debug.LogWarning("fromCell or toCell was null.");
                return null;
            }

            if (Search(fromCell, toCell, fromCell.Unit))
            {
                List<HexCell> path = new List<HexCell>();
                HexCell current = toCell;
                while (current != fromCell)
                {
                    path.Add(current);
                    current = current.PrevInPath;
                }
                path.Reverse();
                return path;
            }
            return null;
        }

        /// <summary>
        /// Conduct an A* pathfinding search on the HexGrid the given cells
        /// belong to and see if there is a path between them, taking into account
        /// unit data.
        /// </summary>
        /// <param name="fromCell">The starting cell.</param>
        /// <param name="toCell">The target cell.</param>
        /// <param name="unit">The unit a path is being searched for.</param>
        /// <returns>True if a path exists between from the starting cell to 
        /// the target cell, false otherwise.</returns>
        private bool Search(HexCell fromCell, HexCell toCell, Unit unit)
        {
            PrepFrontierForNewSearch();

            fromCell.SearchPhase = _searchFrontierPhase;
            fromCell.Distance = 0;
            _searchFrontier.Enqueue(fromCell);

            while (!_searchFrontier.IsEmpty)
            {
                HexCell current = _searchFrontier.Dequeue();
                // At this point, incrementing a cell's SearchPhase
                // would make it one more than the search frontier phase,
                // thereby marking it as processed and no longer in the
                // frontier.
                current.SearchPhase++;
                if (current == toCell)
                {
                    // Path to target cell found.
                    return true;
                }

                for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
                {
                    HexCell neighbor = current.GetNeighbor(dir);

                    // Check to see if neighbor is valid for searching.
                    if (neighbor != null && neighbor.SearchPhase <= _searchFrontierPhase && 
                        neighbor.CanUnitTravelOn(unit) &&
                        (neighbor.Unit == null || 
                        (neighbor.Unit.Data.CanMove &&
                        neighbor.Unit.OperationTick == unit.OperationTick &&
                        neighbor.Unit.OwnerPlayerId == unit.OwnerPlayerId) || 
                        (neighbor.Explored && !neighbor.IsVisible)))
                    {
                        // Add to neighborDistance here based on cell terrain weights.
                        int neighborDistance = current.Distance + 5;

                        if (neighbor.SearchPhase < _searchFrontierPhase)
                        {
                            // Never visited neighbor, so neighborDistance is the lowest
                            // distance to the neighboring cell so far.
                            neighbor.SearchPhase = _searchFrontierPhase;
                            neighbor.Distance = neighborDistance;
                            neighbor.PrevInPath = current;
                            neighbor.SearchHeuristic = neighbor.Coordinates.DistanceTo(toCell.Coordinates);
                            _searchFrontier.Enqueue(neighbor);
                        }
                        else if (neighborDistance < neighbor.Distance)
                        {
                            // We've visited this neighbor, but there is a shorter
                            // distance to it from the current cell.
                            int oldPriority = neighbor.SearchPriority;
                            neighbor.Distance = neighborDistance;
                            neighbor.PrevInPath = current;
                            // Since priority is based on a cell's distance, its state
                            // in the priority queue must change.
                            _searchFrontier.Change(neighbor, oldPriority);
                        }
                    }
                }
            }
            // No path to target cell found.
            return false;
        }

        /// <summary>
        /// Clear the search frontier and increment the searchFrontierPhase
        /// by two to allow all cells to be available for searching.
        /// </summary>
        public void PrepFrontierForNewSearch()
        {
            _searchFrontierPhase += NewSearchPhaseAddAmount;
            _searchFrontier.Clear();
            _queueFrontier.Clear();
        }

        /// <summary>
        /// Find the closest cell to a starting cell from a target cell 
        /// within a certain range.
        /// </summary>
        /// <param name="fromCell">The starting cell.</param>
        /// <param name="targetCell">The target cell.</param>
        /// <param name="range">The maximum distance from the target cell.</param>
        /// <returns>The closest cell to a starting cell from a target cell 
        /// within the given range.</returns>
        public HexCell GetClosestCellInRangeToTarget(HexCell fromCell, HexCell targetCell, int range)
        {
            Unit unitSearchingFor = fromCell.Unit;

            PrepFrontierForNewSearch();

            List<HexCell> surroundingCells = new List<HexCell>();
            targetCell.Distance = 0;
            targetCell.SearchPhase = _searchFrontierPhase + 1;
            _queueFrontier.Enqueue(targetCell);

            while (_queueFrontier.Count > 0)
            {
                HexCell current = _queueFrontier.Dequeue();
                for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
                {
                    HexCell neighbor = current.GetNeighbor(dir);
                    if (neighbor != null && neighbor.SearchPhase <= _searchFrontierPhase && 
                        neighbor.Unit == null && neighbor.CanUnitTravelOn(unitSearchingFor))
                    {
                        neighbor.SearchPhase = _searchFrontierPhase + 1;
                        neighbor.Distance = current.Distance + 1;
                        if (neighbor.Distance < range)
                        {
                            surroundingCells.Add(neighbor);
                            _queueFrontier.Enqueue(neighbor);
                        }
                        else if (neighbor.Distance == range)
                        {
                            surroundingCells.Add(neighbor);
                        }
                    }
                }
            }

            HexCell closestCell = null;
            int closestDistance = int.MaxValue;
            for (int i = 0; i < surroundingCells.Count; i++)
            {
                int distance = fromCell.Coordinates.DistanceTo(surroundingCells[i].Coordinates);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestCell = surroundingCells[i];
                }
            }
            return closestCell;
        }

        /// <summary>
        /// Get a list of all the friendly units that can attack (have the attacker component) 
        /// in a certain range from a given cell.
        /// </summary>
        /// <param name="fromCell">The given starting cell.</param>
        /// <param name="range">The range to look for friendly units.</param>
        /// <param name="ownerPlayerId">The owner player id units must match to be considered.</param>
        /// <returns>A list of all the friendly units in a certain range from a given cell.</returns>
        public List<Unit> GetAllFriendlyAttackerUnitsInRange(HexCell fromCell, int range, int ownerPlayerId)
        {
            PrepFrontierForNewSearch();

            List<Unit> friendlyUnitsInRange = new List<Unit>();
            fromCell.Distance = 0;
            fromCell.SearchPhase = _searchFrontierPhase + 1;
            _queueFrontier.Enqueue(fromCell);
            while(_queueFrontier.Count > 0)
            {
                HexCell current = _queueFrontier.Dequeue();
                for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
                {
                    HexCell neighbor = current.GetNeighbor(dir);
                    if (neighbor != null && neighbor.SearchPhase <= _searchFrontierPhase)
                    {
                        neighbor.SearchPhase = _searchFrontierPhase + 1;
                        neighbor.Distance = current.Distance + 1;
                        if (neighbor.Distance <= range)
                        {
                            if (neighbor.Unit != null && neighbor.Unit.Data.CanAttack &&
                                neighbor.Unit.OwnerPlayerId == ownerPlayerId)
                            {
                                friendlyUnitsInRange.Add(neighbor.Unit);
                            }
                        }
                        if (neighbor.Distance < range)
                        {
                            _queueFrontier.Enqueue(neighbor);
                        }
                    }
                }
            }
            return friendlyUnitsInRange;
        }

        /// <summary>
        /// Get a list of HexCells in range to a starting cell.
        /// </summary>
        /// <param name="fromCell">The starting cell.</param>
        /// <param name="range">The range to update visibility in.</param>
        /// <returns>A list of all the cells in range to the starting cell.</returns>
        public List<HexCell> GetCellsInRange(HexCell fromCell, int range)
        {
            PrepFrontierForNewSearch();

            fromCell.Distance = 0;
            fromCell.SearchPhase = _searchFrontierPhase + 1;
            _queueFrontier.Enqueue(fromCell);

            List<HexCell> cellsInRange = new List<HexCell>();
            cellsInRange.Add(fromCell);
            while (_queueFrontier.Count > 0)
            {
                HexCell current = _queueFrontier.Dequeue();
                for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
                {
                    HexCell neighbor = current.GetNeighbor(dir);
                    if (neighbor != null && neighbor.SearchPhase <= _searchFrontierPhase)
                    {
                        neighbor.Distance = current.Distance + 1;
                        neighbor.SearchPhase = _searchFrontierPhase + 1;
                        cellsInRange.Add(neighbor);
                        if (neighbor.Distance < range)
                        {
                            _queueFrontier.Enqueue(neighbor);
                        }
                    }
                }
            }
            return cellsInRange;
        }

        /// <summary>
        /// Get a list of all HexCells within a screen space bounds.
        /// For use with box selection on the client.
        /// </summary>
        /// <param name="centerCell">The cell at the center of the bounds.</param>
        /// <param name="min">The min bound.</param>
        /// <param name="max">The max bound.</param>
        /// <returns>All HexCells within the given bounds.</returns>
        public List<HexCell> GetCellsInScreenBounds(HexCell centerCell, Vector2 min, Vector2 max)
        {
            PrepFrontierForNewSearch();
            centerCell.SearchPhase = _searchFrontierPhase + 1;
            _queueFrontier.Enqueue(centerCell);

            List<HexCell> cellsInBounds = new List<HexCell>();
            cellsInBounds.Add(centerCell);

            while (_queueFrontier.Count > 0)
            {
                HexCell current = _queueFrontier.Dequeue();
                for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
                {
                    HexCell neighbor = current.GetNeighbor(dir);
                    if (neighbor != null && neighbor.SearchPhase <= _searchFrontierPhase)
                    {
                        neighbor.SearchPhase = _searchFrontierPhase + 1;
                        Vector2 neighborScreenPos = Camera.main.WorldToScreenPoint(neighbor.Position);
                        if (neighborScreenPos.x > min.x && neighborScreenPos.x < max.x &&
                            neighborScreenPos.y > min.y && neighborScreenPos.y < max.y)
                        {
                            cellsInBounds.Add(neighbor);
                            _queueFrontier.Enqueue(neighbor);
                        }
                    }
                }
            }
            return cellsInBounds;
        }
    }
}