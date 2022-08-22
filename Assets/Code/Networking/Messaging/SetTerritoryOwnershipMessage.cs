using Mirror;

namespace Assets.Code.Networking.Messaging
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --SERVER-TO-CLIENT--
    /// </para>
    /// Tell clients that a territory is now owned by a player.
    /// </summary>
    public struct SetTerritoryOwnershipMessage : NetworkMessage
    {
        public int TerritoryId;
        public int PlayerId;
    }
}
