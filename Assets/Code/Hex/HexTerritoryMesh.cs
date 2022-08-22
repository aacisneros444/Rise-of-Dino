using UnityEngine;
using System.Collections.Generic;
using Mirror;

namespace Assets.Code.Hex
{
    /// <summary>
    /// A GameObject component to render a HexTerritory's borders.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class HexTerritoryMesh : MonoBehaviour
    {
        /// <summary>
        /// The position of a hexagons six vertices relative to its center.
        /// Defined in clock-wise order, starting from the top (top pointed hexagons).
        /// The last element in this array is a duplicate of the first to avoid
        /// fencepost issues when adding 1 to an index variable.
        /// </summary>
        public static readonly Vector3[] Vertices =
        {
            new Vector3(0f, 0f, HexMetrics.OuterRadius),
            new Vector3(HexMetrics.InnerRadius, 0f, 0.5f * HexMetrics.OuterRadius),
            new Vector3(HexMetrics.InnerRadius, 0f, -0.5f * HexMetrics.OuterRadius),
            new Vector3(0f, 0f, -HexMetrics.OuterRadius),
            new Vector3(-HexMetrics.InnerRadius, 0f, -0.5f * HexMetrics.OuterRadius),
            new Vector3(-HexMetrics.InnerRadius, 0f, 0.5f * HexMetrics.OuterRadius),
            new Vector3(0f, 0f, HexMetrics.OuterRadius)
        };

        /// <summary>
        /// The border thickness value.
        /// </summary>
        private const float BorderThickness = 0.4f;

        /// <summary>
        /// A y offset to be applied to the final drawn mesh.
        /// </summary>
        private static readonly Vector3 YOffset = new Vector3(0f, 0.1f, 0f);

        /// <summary>
        /// The color of a territory that is owned by no one.
        /// </summary>
        private static readonly Color UnownedColor = Color.black;

        /// <summary>
        /// A static int to act as a reference to indicate if a HexTerritoryMesh should
        /// be updated. If neighboring HexTerritories have a lesser MeshUpdatePhase, 
        /// this indicates they should have their mesh refreshed if intended.
        /// </summary>
        private static int s_MeshUpdatePhase;

        /// <summary>
        /// The HexTerritoryMesh's update phase. If less than the static MeshUpdatePhase,
        /// this indicates that this mesh should be updated if inteded.
        /// </summary>
        public int MeshUpdatePhase { get; set; }


        private MeshRenderer _renderer;
        private MeshFilter _meshFilter;

        // Mesh Data
        private Mesh _hexMesh;
        private static List<Vector3> s_vertices;
        private static List<int> s_triangles;
        private static List<Vector2> s_uvs;

        /// <summary>
        /// Create the mesh data containers and get the mesh filter.
        /// </summary>
        private void Awake()
        {
            _hexMesh = new Mesh();
            _hexMesh.name = "Hex Territory Mesh";
            s_vertices = new List<Vector3>();
            s_triangles = new List<int>();
            s_uvs = new List<Vector2>();

            _renderer = GetComponent<MeshRenderer>();
            _meshFilter = GetComponent<MeshFilter>();
        }

        /// <summary>
        /// Generate a new territorial border mesh given the
        /// border cells of a HexTerritory.
        /// </summary>
        /// <param name="borderCells">The border cells of this HexTerritory.</param>
        /// <param name="playerId"> The HexTerritory's owner player id.</param>
        /// <param name="incrementUpdatePhase">Denotes whether or not neighboring
        /// HexTerritory's meshes should also be updated.</param>
        public void Create(List<HexCell> borderCells, int playerId)
        {
            ClearMeshData();

            // Use a list of "hexBorder" boolean arrays, each of which
            // has 6 elements. Each element denotes whether or not the 
            // HexCell needs a border quad drawn in the indexed hex direction.
            List<bool[]> hexBorders = new List<bool[]>();
            for (int i = 0; i < borderCells.Count; i++)
            {
                bool[] borders = new bool[6];
                GetCellBorders(borderCells[i], borders);
                hexBorders.Add(borders);
            }
            TriangulateCellBorders(borderCells, hexBorders);

            if (NetworkClient.active)
            {
                SetColorForMesh(playerId);
            }

            SetMeshData();
        }

        /// <summary>
        /// Populate the given hexBorders boolean array, setting the indices
        /// at each HexDirection to true if a border needs to be drawn in that direction.
        /// </summary>
        /// <param name="cell">The given HexCell.</param>
        /// <param name="hexBorders">The hex border boolean array.</param>
        /// <param name="neighboringTerritoriesToUpdate"></param>
        private void GetCellBorders(HexCell cell, bool[] hexBorders)
        {
            for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
            {
                HexCell neighbor = cell.GetNeighbor(dir);
                if (neighbor != null && neighbor.Territory.OwnerPlayerId !=
                    cell.Territory.OwnerPlayerId)
                {
                    hexBorders[(int)dir] = true;
                }
            }
        }

        /// <summary>
        /// Update the meshes of neighboring HexTerritories.
        /// </summary>
        /// <param name="borderCells">The border cells of this HexTerritory.</param>
        public void UpdateNeighboringMeshes(List<HexCell> borderCells)
        {
            // Increment the static MeshUpdatePhase. If neighboring territories
            // have a lesser MeshUpdatePhase, this means they must be added to the
            // neighboringTerritoriesToUpdate list so that their meshes can be updated.
            s_MeshUpdatePhase++;

            List<HexTerritory> neighboringTerritoriesToUpdate =
                new List<HexTerritory>();

            for (int i = 0; i < borderCells.Count; i++)
            {
                GetNewNeighborsToUpdate(borderCells[i], neighboringTerritoriesToUpdate);
            }

            for (int i = 0; i < neighboringTerritoriesToUpdate.Count; i++)
            {
                neighboringTerritoriesToUpdate[i].RefreshMesh(false);
            }
        }

        /// <summary>
        /// Search the neighbors of a HexCell to add neighbors not already
        /// present in the neighboringTerritoriesToUpdate list to the list
        /// to be updated.
        /// </summary>
        /// <param name="cell">The given HexCell.</param>
        /// <param name="neighboringTerritoriesToUpdate">
        /// The list of neighboring territories who's meshes will be updated.</param>
        private void GetNewNeighborsToUpdate(HexCell cell,
            List<HexTerritory> neighboringTerritoriesToUpdate)
        {
            for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
            {
                HexCell neighbor = cell.GetNeighbor(dir);
                if (neighbor != null)
                {
                    if (neighbor.Territory.HexTerritoryMesh.MeshUpdatePhase < s_MeshUpdatePhase)
                    {
                        neighbor.Territory.HexTerritoryMesh.MeshUpdatePhase = s_MeshUpdatePhase;
                        neighboringTerritoriesToUpdate.Add(neighbor.Territory);
                    }
                }
            }
        }

        /// <summary>
        /// Get all the quad data necessary for the mesh.
        /// </summary>
        /// <param name="borderCells">The border cells of a HexTerritory.A</param>
        /// <param name="hexBorders">A list of boolean arrays whichs denote
        /// where to draw border quads for HexCells.</param>
        private void TriangulateCellBorders(List<HexCell> borderCells,
            List<bool[]> hexBorders)
        {
            for (int i = 0; i < borderCells.Count; i++)
            {
                HexCell cell = borderCells[i];
                for (int dir = 0; dir <= (int)HexDirection.NW; dir++)
                {
                    if (hexBorders[i][dir])
                    {
                        Vector3 v1 = cell.Position + Vertices[dir] + YOffset;
                        Vector3 v2 = cell.Position + Vertices[dir + 1] + YOffset;
                        Vector3 dirVector = v2 - v1;

                        Vector3 v3;
                        if (!hexBorders[i][GetWrappedDirection(dir - 1)])
                        {
                            // Did not have a quad in previous direction, get outer trapezoid vertex.
                            v3 = v1 + Vector3.Normalize(
                                Quaternion.Euler(0f, 120f, 0f) * dirVector) * BorderThickness;
                        }
                        else
                        {
                            // Has a quad in previous direction, get inner trapezoid vertex.
                            v3 = v1 + Vector3.Normalize(
                                Quaternion.Euler(0f, 60f, 0f) * dirVector) * BorderThickness;

                        }


                        Vector3 v4;
                        if (!hexBorders[i][GetWrappedDirection(dir + 1)])
                        {
                            // Did not have a quad in previous direction, get outer trapezoid vertex.
                            v4 = v2 + Vector3.Normalize(
                                Quaternion.Euler(0f, 60f, 0f) * dirVector) * BorderThickness;
                        }
                        else
                        {
                            // Has a quad in previous direction, get inner trapezoid vertex.
                            v4 = v2 + Vector3.Normalize(
                                Quaternion.Euler(0f, 120f, 0f) * dirVector) * BorderThickness;
                        }

                        AddQuad(v3, v4, v1, v2);
                        AddUvsForQuad();
                    }
                }
            }
        }

        /// <summary>
        /// A helper method to get the index of a HexDirection
        /// when it has been incremented or decremented past its
        /// range.
        /// </summary>
        /// <param name="direction">The direction index.</param>
        /// <returns>The wrapped hex direction index.</returns>
        private int GetWrappedDirection(int direction)
        {
            if (direction == 6)
            {
                return 0;
            }
            else if (direction == -1)
            {
                return 5;
            }
            else
            {
                return direction;
            }
        }

        /// <summary>
        /// Create the necessary mesh data for a quad given four vertices.
        /// </summary>
        /// <param name="v1">The first vertex.</param>
        /// <param name="v2">The second vertex.</param>
        /// <param name="v3">The thrid vertex.</param>
        /// <param name="v4">The fourth vertex.</param>
        private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            // Record the vertex count before adding vertices in order
            // to offset the vertex index for the new vertices in the
            // triangles list.
            int vertexIndex = s_vertices.Count;

            s_vertices.Add(v1);
            s_vertices.Add(v2);
            s_vertices.Add(v3);
            s_vertices.Add(v4);

            s_triangles.Add(vertexIndex);
            s_triangles.Add(vertexIndex + 2);
            s_triangles.Add(vertexIndex + 1);
            s_triangles.Add(vertexIndex + 1);
            s_triangles.Add(vertexIndex + 2);
            s_triangles.Add(vertexIndex + 3);
        }

        /// <summary>
        /// Add the Uvs for the four vertices of a quad.
        /// </summary>
        private void AddUvsForQuad()
        {
            s_uvs.Add(new Vector2(0.01f, 0.01f));
            s_uvs.Add(new Vector2(0.99f, 0.01f));
            s_uvs.Add(new Vector2(0.01f, 0.99f));
            s_uvs.Add(new Vector2(0.99f, 0.99f));
        }

        /// <summary>
        /// Get the HexTerritory border color for rendering.
        /// </summary>
        /// <param name="OwnerPlayerId">The id for the player that owns
        /// the HexTerritory this mesh is rendering.</param>
        private void SetColorForMesh(int OwnerPlayerId)
        {
            Color borderColor = UnownedColor;
            if (OwnerPlayerId != -1)
            {
                borderColor = Networking.NetworkPlayer.GetColorForId(OwnerPlayerId);
            }
            _renderer.material.SetColor("_Color", borderColor);
        }

        /// <summary>
        /// Clear the currently stored mesh data.
        /// </summary>
        private void ClearMeshData()
        {
            _hexMesh.Clear();
            s_vertices.Clear();
            s_triangles.Clear();
            s_uvs.Clear();
        }

        /// <summary>
        /// Set the generated mesh data.
        /// </summary>
        private void SetMeshData()
        {
            _hexMesh.vertices = s_vertices.ToArray();
            _hexMesh.triangles = s_triangles.ToArray();
            _hexMesh.uv = s_uvs.ToArray();
            _hexMesh.RecalculateNormals();

            _meshFilter.mesh = _hexMesh;
        }
    }
}