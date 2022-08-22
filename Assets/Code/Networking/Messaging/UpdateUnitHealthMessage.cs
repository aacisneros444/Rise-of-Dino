using Mirror;

namespace Assets.Code.Networking.Messaging
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --SERVER-TO-CLIENT--
    /// </para>
    /// WRITE DESCRIPTION HERE
    /// Inform a client that a unit's health value has changed.
    public struct UpdateUnitHealthMessage : NetworkMessage
    {
        public int UnitCellIndex;
        public int Value;
    }
}
