using System.Collections.Generic;

namespace Assets.Code.Hex
{
    /// <summary>
    /// A class to manage updating HexCell Visibility for groups of HexCells. 
    /// </summary>
    public static class HexCellVisibilityManager
    {
        /// <summary>
        /// Denotes the range of HexCells visibility will be updated
        /// for on each update visibility request.
        /// </summary>
        private const int UnitVisibilityRange = 3;

        /// <summary>
        /// A set of HexFogMesh objects to be updated at the end of a call
        /// to the UpdateVisibilityAroundCell method.
        /// </summary>
        private static HashSet<HexFogMesh> s_fogMeshesToUpdate;


        static HexCellVisibilityManager()
        {
            s_fogMeshesToUpdate = new HashSet<HexFogMesh>();
        }

        /// <summary>
        /// Update the visibility around a HexCell.
        /// </summary>
        /// <param name="cell">The HexCell.</param>
        /// <param name="addVisibility">Denotes whether or not to add visiblity or substract it.</param>
        /// <param name="range">The range to update visibility in.</param>
        public static void UpdateVisibilityAroundCell(HexCell cell, bool addVisibility)
        {
            s_fogMeshesToUpdate.Clear();

            int visibilityToAdd = 1;
            if (!addVisibility)
            {
                visibilityToAdd = -1;
            }

            List<HexCell> cellsInVisibilityRange = 
                HexPathfinder.Instance.GetCellsInRange(cell, UnitVisibilityRange);
            for (int i = 0; i < cellsInVisibilityRange.Count; i++)
            {
                HexCell cellInVisibilityRange = cellsInVisibilityRange[i];
                if(cellInVisibilityRange.UpdateVisibility
                    (cellInVisibilityRange.Visibility + visibilityToAdd))
                {
                    s_fogMeshesToUpdate.Add(cellInVisibilityRange.Chunk.HexFogMesh);
                }
            }
          
            foreach (HexFogMesh fogMesh in s_fogMeshesToUpdate)
            {
                fogMesh.SetUVData();
            }
        }

        /// <summary>
        /// Update the visibility for a predefined group of HexCells.
        /// </summary>
        /// <param name="cells">The group of HexCells.</param>
        /// <param name="addVisibility">Denotes whether or not to add visiblity or substract it.</param>
        public static void UpdateVisibilityForCellGroup(List<HexCell> cells, bool addVisibility)
        {
            s_fogMeshesToUpdate.Clear();

            int visibilityToAdd = 1;
            if (!addVisibility)
            {
                visibilityToAdd = -1;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                HexCell cell = cells[i];
                if(cell.UpdateVisibility(cell.Visibility + visibilityToAdd))
                {
                    s_fogMeshesToUpdate.Add(cell.Chunk.HexFogMesh);
                }
            }

            foreach (HexFogMesh fogMesh in s_fogMeshesToUpdate)
            {
                fogMesh.SetUVData();
            }
        }
    }
}