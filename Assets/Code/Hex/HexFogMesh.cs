using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code.Hex
{
    /// <summary>
    /// A GameObject component to render a fog mesh for a chunk
    /// of hexagonal cells.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class HexFogMesh : MonoBehaviour
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
        /// A y offset to be applied to the final drawn mesh.
        /// </summary>
        private static readonly Vector3 YOffset = new Vector3(0f, 1f, 0f);

        private const float VisibleAlpha = 0f;
        // same as NotVisibleExploredAlpha, may update in the future to make unexplored land completely dark (1f alpha).
        private const float NotVisibleAlpha = 0.5f;
        private const float NotVisibleExploredAlpha = 0.5f;

        private MeshFilter _meshFilter;

        // Mesh Data
        private Mesh _hexFogMesh;
        private static List<Vector3> s_vertices;
        private static List<int> s_triangles;

        /// <summary>
        /// The TEXCOORD0 (mesh uvs) array. The first value, x,
        /// of each Vector2 denotes the alpha value for the vertex.
        /// </summary>
        private Vector2[] _vertexAlphas;

        /// <summary>
        /// Create the mesh data containers.
        /// </summary>
        static HexFogMesh()
        {
            s_vertices = new List<Vector3>();
            s_triangles = new List<int>();
        }

        /// <summary>
        /// Create the mesh object, get the mesh filter, and create
        /// the uvs list.
        /// </summary>
        private void Awake()
        {
            _hexFogMesh = new Mesh();
            _hexFogMesh.name = "Hex Fog Mesh";
            _vertexAlphas = new Vector2[HexGrid.ChunkSize * HexGrid.ChunkSize * Vertices.Length];

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
            // offset by the HexCell's center and a Y offset.
            for (int i = 0; i < Vertices.Length; i++)
            {
                s_vertices.Add(Vertices[i] + cell.Position + YOffset);
            }

            // Populate triangles list with vertex indices offset by the number
            // of vertices present before adding the new ones.
            for (int i = 0; i < s_triangleVerts.Length; i++)
            {
                s_triangles.Add(vertexCount + s_triangleVerts[i]);
            }

            for (int i = 0; i < Vertices.Length; i++)
            {
                _vertexAlphas[vertexCount + i] = new Vector2(NotVisibleAlpha, 0);
            }
        }

        /// <summary>
        /// Clear the currently stored mesh data.
        /// </summary>
        private void ClearMeshData()
        {
            _hexFogMesh.Clear();
            s_vertices.Clear();
            s_triangles.Clear();
        }

        /// <summary>
        /// Set the generated mesh data.
        /// </summary>
        private void SetMeshData()
        {
            _hexFogMesh.vertices = s_vertices.ToArray();
            _hexFogMesh.triangles = s_triangles.ToArray();
            _hexFogMesh.uv = _vertexAlphas;
            _hexFogMesh.RecalculateNormals();

            _meshFilter.mesh = _hexFogMesh;
        }

        /// <summary>
        /// Update the fog visibility for a member HexCell
        /// based on its visibility property.
        /// </summary>
        /// <param name="cell">The HexCell.</param>
        public void UpdateFogAtCell(HexCell cell)
        {
            int uvStartIndex = cell.LocalChunkIndex * Vertices.Length;
            float alphaValue;
            if (cell.IsVisible)
            {
                alphaValue = VisibleAlpha;
            }
            else
            {
                alphaValue = cell.Explored ? NotVisibleExploredAlpha : NotVisibleAlpha;
            }
            for (int i = uvStartIndex; i < uvStartIndex + Vertices.Length; i++)
            {
                _vertexAlphas[i] = new Vector2(alphaValue, 0f);
            }
        }

        /// <summary>
        /// Only set the generated uv's for the mesh.
        /// </summary>
        public void SetUVData()
        {
            _hexFogMesh.uv = _vertexAlphas;
        }
    }
}