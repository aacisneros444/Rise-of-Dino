using Mirror;

namespace Assets.Code.Networking
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --SERVER-TO-CLIENT--
    /// </para>
    /// Tell the client to set its era to the current era.
    /// </summary>
    public struct SetEraMessage : NetworkMessage
    {
        public int EraIndex;
    }
}
