using Mirror;

namespace Assets.Code.Networking.Messaging
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --SERVER-TO-CLIENT--
    /// </para>
    /// Tell a client to spawn a unit.
    /// </summary>
    public struct SpawnUnitMessage : NetworkMessage
    {
        public int Id;
        public int CellIndex;
        public int OwnerPlayerId;
    }
}
