namespace Assets.Code.Hex
{
    /// <summary>
    /// A class to provide extension methods for the HexDirection enumerated type.
    /// </summary>
    public static class HexDirectionExtensions
    {
        /// <summary>
        /// Get the opposite HexDirection from a given HexDirection.
        /// </summary>
        /// <param name="direction">The given HexDirection.</param>
        /// <returns>The opposite HexDirection.</returns>
        public static HexDirection Opposite (this HexDirection direction )
        {
            return (int)direction < 3 ? (direction + 3) : (direction - 3);
        }
    }
}