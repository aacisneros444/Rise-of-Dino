using Mirror;

namespace Assets.Code.Networking.Messaging
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --SERVER-TO-CLIENT--
    /// </para>
    /// Tell the client to make all grid cells visible.
    /// </summary>
    public struct SetAllCellsVisibleMessage : NetworkMessage
    {

    }
}
