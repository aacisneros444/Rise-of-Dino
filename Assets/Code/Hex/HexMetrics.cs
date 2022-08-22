using UnityEngine;

namespace Assets.Code.Hex
{
    /// <summary>
    /// A static class holding metric data for hexagons.
    /// </summary>
    public static class HexMetrics
    {
        /// <summary>
        /// The length from the center of a hexagon to any vertex.
        /// Also is the length of each hexagon edge (since a hexagon consists)
        /// of six equilateral triangles.
        /// </summary>
        public const float OuterRadius = 1f;

        /// <summary>
        /// The length from the center of a hexagon to any edge.
        /// Derived by using pythagorean theorem.
        /// InnerRadius = sqrt(OuterRadius^2-(OuterRadius/2)^2)
        /// </summary>
        public const float InnerRadius = OuterRadius * 0.866025404f;

        /// <summary>
        /// The horizontal distance to a neighboring hexagon.
        /// </summary>
        public const float HorizontalDistanceToNeighbor = InnerRadius * 2f;

        /// <summary>
        /// The vertical distance to a neighboring hexagon.
        /// </summary>
        public const float VerticalDistanceToNeighbor = OuterRadius * 1.5f;
    }
}
