using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code.Hex
{
    /// <summary>
    /// A GameObject component to render a hexagonal grid.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class HexMesh : MonoBehaviour
    {
        /// <summary>
        /// The position of a hexagons six vertices relative to its center.
        /// Defined in clock-wise order, starting from the top (top pointed hexagons).
        /// </summary>
        public static readonly Vector3[] Vertices =
        {
            new Vector3(0f, 0f, HexMetrics.OuterRadius),
            new Vector3(HexMetrics.InnerRadius, 0f, 0.5f * HexMetrics.OuterRadius),
            new Vector3(HexMetrics.InnerRadius, 0f, -0.5f * HexMetrics.OuterRadius),
            new Vector3(0f, 0f, -HexMetrics.OuterRadius),
            new Vector3(-HexMetrics.InnerRadius, 0f, -0.5f * HexMetrics.OuterRadius),
            new Vector3(-HexMetrics.InnerRadius, 0f, 0.5f * HexMetrics.OuterRadius)
        };

        /// <summary>         
        /// An array of ints to determine how to connect hex vertices
        /// into triangles.
        /// 0 = hex top
        /// 1 = hex top right
        /// 2 = hex bottom right
        /// 3 = hex bottom
        /// 4 = hex bottom left
        /// 5 = hex top left
        /// </summary>
        private static readonly int[] s_triangleVerts =
        {
            5, 0, 1, // triangle 1
            4, 5, 1, // triangle 2
            4, 1, 2, // triangle 3
            4, 2, 3  // triangle 4
        };

        /// <summary>
        /// Number of terrain textures horizontally in the atlas texture
        /// used in this mesh's material.
        /// </summary>
        private const int NumTerrainTexturesX = 4;

        /// <summary>
        /// Number of terrain textures vertically in the atlas texture
        /// used in this mesh's material.
        /// </summary>
        private const int NumTerrainTexturesY = 4;

        /// <summary>
        /// Width of terrain atlas subtexture in terms of UV coordinates.
        /// </summary>
        private const float TerrainSubTextureWidth = 1f / NumTerrainTexturesX;

        /// <summary>
        /// Height of terrain atlas subtexture in terms of UV coordinates.
        /// </summary>
        private const float TerrainSubTextureHeight = 1f / NumTerrainTexturesY;


        private MeshFilter _meshFilter;

        // Mesh Data
        private Mesh _hexMesh;
        private static List<Vector3> s_vertices;
        private static List<int> s_triangles;
        private static List<Vector2> s_uvs;

        /// <summary>
        /// Create the mesh data containers.
        /// </summary>
        static HexMesh()
        {
            s_vertices = new List<Vector3>();
            s_triangles = new List<int>();
            s_uvs = new List<Vector2>();
        }

        /// <summary>
        /// Create the mesh object and get the mesh filter.
        /// </summary>
        private void Awake()
        {
            _hexMesh = new Mesh();
            _hexMesh.name = "Hex Mesh";

            _meshFilter = GetComponent<MeshFilter>();
        }

        /// <summary>
        /// Generate a new HexMesh based on the given cells of a HexGrid.
        /// </summary>
        /// <param name="cells">The cells of a HexGrid.</param>
        public void Create(HexCell[] cells)
        {
            ClearMeshData();
            for (int i = 0; i < cells.Length; i++)
            {
                Triangulate(cells[i]);
            }
            SetMeshData();
        }

        /// <summary>
        /// Triangulate a hexagonal cell.
        /// </summary>
        /// <param name="cell">The HexCell to triangulate.</param>
        private void Triangulate(HexCell cell)
        {
            // Record the vertex count before adding vertices in order
            // to offset the vertex index for the new vertices in the
            // triangles list.
            int vertexCount = s_vertices.Count;

            // Populate vertices list with hex vertex positions
            // offset by the HexCell's center.
            for (int i = 0; i < Vertices.Length; i++)
            {
                s_vertices.Add(Vertices[i] + cell.Position);
            }

            // Populate triangles list with vertex indices offset by the number
            // of vertices present before adding the new ones.
            for (int i = 0; i < s_triangleVerts.Length; i++)
            {
                s_triangles.Add(vertexCount + s_triangleVerts[i]);
            }

            // Set uv's to proper position on the atlas texture used by this mesh
            // based on cell terrain type.
            for (int i = 0; i < Vertices.Length; i++)
            {
                // Multiply x and y uv coordinates of cell TerrainType by
                // terrain subtexture width/height to obtain correct UV coordinates.
                // Add half the terrain subtexture width/height to get to the center
                // of a subtexture. This prevents atlas leaking artifacts.
                Vector2 uvs = new Vector2(
                    cell.TerrainType.UvCoordinates.x * TerrainSubTextureWidth
                    + TerrainSubTextureWidth / 2,
                    cell.TerrainType.UvCoordinates.y * TerrainSubTextureHeight
                    + TerrainSubTextureHeight / 2);
                s_uvs.Add(uvs);
            }
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
