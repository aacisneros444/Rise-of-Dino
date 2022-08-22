using Mirror;

namespace Assets.Code.Networking.Messaging
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --SERVER-TO-CLIENT--
    /// </para>
    /// Send all the ownership ids, spawn types, and levels for HexTerritories
    /// in a HexWorld.
    /// </summary>
    public struct SetTerritoryStatesMessage : NetworkMessage
    {
        public int[] TerritoryIds;
        public int[] OwnerPlayerIds;
        public int[] UnitSpawnTypeIds;
        public int[] Levels;
    }
}
