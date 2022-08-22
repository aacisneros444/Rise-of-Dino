using Mirror;

namespace Assets.Code.Networking.Messaging
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --CLIENT-TO-SERVER--
    /// </para>
    /// Ask the server to upgrade an owned HexTerritory.
    /// </summary>
    public struct RequestTerritoryUpgradeMessage : NetworkMessage
    {
        public int TerritoryId;
    }
}
