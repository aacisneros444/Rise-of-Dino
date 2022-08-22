using Mirror;

namespace Assets.Code.Networking.Messaging
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --CLIENT-TO-SERVER--
    /// </para>
    /// Ask the server to select a new unit type of the same level 
    /// for a territory.
    /// </summary>
    public struct TerritorySpawnTypeRerollMessage : NetworkMessage
    {
        public int TerritoryId;
    }
}
