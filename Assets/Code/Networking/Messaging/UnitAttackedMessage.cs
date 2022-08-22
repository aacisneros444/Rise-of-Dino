using Mirror;

namespace Assets.Code.Networking.Messaging
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --SERVER-TO-CLIENT--
    /// </para>
    /// Inform the client that a unit at a certain HexCell attacked
    /// another unit.
    /// </summary>
    public struct UnitAttackedMessage : NetworkMessage
    {
        public int UnitCellIndex;
        public int TargetCellIndex;
    }
}
