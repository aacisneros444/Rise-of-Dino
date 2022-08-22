using Mirror;

namespace Assets.Code.Networking.Messaging 
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --SERVER-TO-CLIENT--
    /// </para>
    /// Tell the client to update the number of ticks elapsed
    /// since last unit spawn for a territory.
    /// </summary>
    public struct SetTerritorySpawnTicksElapsedMessage : NetworkMessage
    {
        public ushort TerritoryId;
        public ushort TicksElapsed;
    }
}

