using UnityEngine;
using System.Collections.Generic;

namespace Assets.Code.Hex
{
    /// <summary>
    /// A class to model a "chunk" of hexagonal cells withim
    /// a HexGrid. 
    /// <para>
    /// Note: A chunk does not create or manage any cells
    /// of its own: this job is left to the HexGrid. Rather, it serves
    /// as a smaller container of a subset of cells for more efficient
    /// mesh rendering.
    /// </para>
    /// </summary>
    public class HexGridChunk : MonoBehaviour
    {
        /// <summary>
        /// The cells assigned to this HexGridChunk.
        /// </summary>
        private HexCell[] _cells;

        /// <summary>
        /// The HexMesh to visualize the cells assigned to this chunk.
        /// </summary>
        [SerializeField] private HexMesh _hexMesh;

        /// <summary>
        /// A HexFogMesh to mask occupants of unexplored cells assigned
        /// to this chunk.
        /// </summary>
        [SerializeField] private HexFogMesh _hexFogMesh;

        /// <summary>
        /// A HexFogMesh to mask occupants of unexplored cells assigned
        /// to this chunk.
        /// </summary>
        public HexFogMesh HexFogMesh { get { return _hexFogMesh; } }

        /// <summary>
        /// Create the array to store HexCells.
        /// </summary>
        private void Awake()
        {
            _cells = new HexCell[HexGrid.ChunkSize * HexGrid.ChunkSize];
        }

        /// <summary>
        /// Assign a cell to this HexGridChunk.
        /// </summary>
        /// <param name="index">The local index of the cell.</param>
        /// <param name="cell">The HexCell to add.</param>
        public void AssignCell(int index, HexCell cell)
        {
            _cells[index] = cell;

            // Give a reference to the chunk to the cell and its local chunk index.
            cell.Chunk = this;
            cell.LocalChunkIndex = index;
        }

        /// <summary>
        /// Update the mesh to visualize the current state of the grid cells
        /// within this grid chunk.
        /// </summary>
        public void RefreshMesh()
        {
            _hexMesh.Create(_cells);
        }

        /// <summary>
        /// Create the initial fog of war mesh.
        /// </summary>
        public void CreateHexFogMesh()
        {
            _hexFogMesh.Create(_cells);
        }
    }
}