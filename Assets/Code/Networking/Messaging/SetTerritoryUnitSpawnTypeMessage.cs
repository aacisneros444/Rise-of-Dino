using Mirror;

namespace Assets.Code.Networking.Messaging
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --SERVER-TO-CLIENT--
    /// </para>
    /// Tell the client to set a territory's unit spawn type.
    /// </summary>
    public struct SetTerritoryUnitSpawnTypeMessage : NetworkMessage
    {
        public int TerritoryId;
        public int UnitSpawnTypeId;
    }
}
