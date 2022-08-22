using Mirror;

namespace Assets.Code.Networking.Messaging
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --CLIENT-TO-SERVER--
    /// </para>
    /// Ask the server to get units to attack another unit 
    /// at a target HexCell.
    /// </summary>
    public struct AttackOrderMessage : NetworkMessage
    {
        public int[] UnitCellIndices;
        public int TargetCellIndex;
    }
}
