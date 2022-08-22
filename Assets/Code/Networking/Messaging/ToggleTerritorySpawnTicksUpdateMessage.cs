using Mirror;

namespace Assets.Code.Networking.Messaging
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --CLIENT-TO-SERVER--
    /// </para>
    /// Ask the server to start or stop sending spawn tick updates 
    /// for a given HexTerritory.
    /// </summary>
    public struct ToggleTerritorySpawnTicksUpdateMessage : NetworkMessage
    {
        public int TerritoryId;
        public bool SendUpdates;
    }
}
