using Mirror;

namespace Assets.Code.Networking.Messaging
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --CLIENT-TO-SERVER--
    /// </para>
    /// Ask the server to begin moving units to a certain HexCell.
    /// </summary>
    public struct MoveOrderMessage : NetworkMessage
    {
        public int[] UnitCellIndices;
        public int ToCellIndex;
    }
}
