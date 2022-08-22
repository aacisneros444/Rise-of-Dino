using Mirror;

namespace Assets.Code.Networking
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --SERVER-TO-CLIENT--
    /// </para>
    /// Set a HexTerritory's level on the client.
    /// </summary>
    public struct SetTerritoryLevelMessage : NetworkMessage
    {
        public int TerritoryId;
        public int Level;
    }
}
