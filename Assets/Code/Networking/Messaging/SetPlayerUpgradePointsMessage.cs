using Mirror;

namespace Assets.Code.Networking.Messaging
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --SERVER-TO-CLIENT--
    /// </para>
    /// Tell the client the updated upgrade points that the local
    /// player has.
    /// </summary>
    public struct SetPlayerUpgradePointsMessage : NetworkMessage
    {
        public int UpgradePoints;
    }
}
