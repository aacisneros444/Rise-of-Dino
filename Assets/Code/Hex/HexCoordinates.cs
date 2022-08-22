using UnityEngine;

namespace Assets.Code.Hex
{
    /// <summary>
    /// A struct to model cube coordinates for hexagonal grid cells.
    /// <para>
    /// This makes arithmetic and describing movement within a hexagonal
    /// grid much easier.
    /// </para>
    /// </summary>
    public struct HexCoordinates
    {
        public int X { get; private set; }
        public int Z { get; private set; }
        public int Y { get { return -X - Z; } }

        /// <summary>
        /// Create new HexCoordinates from cube coordinates x and z.
        /// </summary>
        /// <param name="x">The x cube coordinate.</param>
        /// <param name="z">The z cube coordinate.</param>
        public HexCoordinates(int x, int z)
        {
            X = x;
            Z = z;
        }

        /// <summary>
        /// Generate cube coordinates given raw offset coordinates
        /// from grid generation.
        /// </summary>
        /// <param name="x">The x offset coordinate.</param>
        /// <param name="z">The z offset coordinate.</param>
        /// <returns>The new cube coordinates.</returns>
        public static HexCoordinates FromOffsetCoordinates(int x, int z)
        {
            /// Undo the horizontal shift from grid generation by dividing the z offset
            /// coordinate by 2. This shifts the new x cube coordinate one unit left
            /// every two rows.
            int horizontalShift = z / 2;
            return new HexCoordinates(x - horizontalShift, z);
        }

        /// <summary>
        /// Get the cube coordinates for a world space position.
        /// </summary>
        /// <param name="position">The world space position.</param>
        /// <returns>The cube coordinates associated with the world space position.</returns>
        public static HexCoordinates FromPosition(Vector3 position)
        {
            float x = position.x / HexMetrics.HorizontalDistanceToNeighbor;
            // y is a mirror of x
            float y = -x;

            // Take into account horizontal shifting from grid generation by offsetting x and y
            // by one every two rows.
            float horizontalShift = position.z / (HexMetrics.VerticalDistanceToNeighbor * 2);
            x -= horizontalShift;
            y -= horizontalShift;

            // Round to integers to get cube coordinates and derive z.
            int iX = Mathf.RoundToInt(x);
            int iY = Mathf.RoundToInt(y);
            int iZ = Mathf.RoundToInt(-x - y);

            // All components should add to zero, however some rounding errors
            // can occur near the edges of hexagons. To solve for this, discard
            // the component with the largest rounding delta and reconstruct it
            // from the other two.
            if (iX + iY + iZ != 0)
            {
                float dX = Mathf.Abs(x - iX);
                float dY = Mathf.Abs(y - iY);
                float dZ = Mathf.Abs(-x - y - iZ);

                if (dX > dY && dX > dZ)
                {
                    iX = -iY - iZ;
                }
                else if (dZ > dY)
                {
                    iZ = -iX - iY;
                }
            }

            return new HexCoordinates(iX, iZ);
        }

        /// <summary>
        /// Compute the distance from these cube coordinates to another set
        /// of cube coordinates.
        /// </summary>
        /// <param name="other">The other HexCoordinates.</param>
        /// <returns>The distance to the other set of given coordinates.</returns>
        public int DistanceTo(HexCoordinates other)
        {
            // Find the absolute difference of the two sets of coordinates
            // (distances cannot be negative) and divide by two to account
            // for adding the absolute value of each component which
            // doubles the actual distance.
            return ((X < other.X ? other.X - X : X - other.X) +
                (Y < other.Y ? other.Y - Y : Y - other.Y) +
                (Z < other.Z ? other.Z - Z : Z - other.Z)) / 2;
        }

        /// <summary>
        /// Get a String representation of the hex cube coordinates.
        /// </summary>
        /// <returns>A String representation of the hex cube coordinates.</returns>
        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }
}