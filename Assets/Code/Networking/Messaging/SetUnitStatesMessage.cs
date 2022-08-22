using Mirror;

namespace Assets.Code.Networking.Messaging
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --SERVER-TO-CLIENT--
    /// </para>
    /// Send all units and their states for a HexWorld to a client.
    /// </summary>
    public struct SetUnitStatesMessage : NetworkMessage
    {
        public int[] UnitTypeIds;
        public int[] UnitCellIndices;
        public int[] UnitOwnerPlayerIds;
        public int[] UnitHealthValues;
    }
}
