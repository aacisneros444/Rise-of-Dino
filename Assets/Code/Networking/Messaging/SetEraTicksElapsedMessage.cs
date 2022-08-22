using Mirror;

namespace Assets.Code.Networking.Messaging
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --SERVER-TO-CLIENT--
    /// </para>
    /// Tell the client the number of ticks elapsed for the current era.
    /// </summary>
    public struct SetEraTicksElapsedMessage : NetworkMessage
    {
        public int TicksElapsed;
    }
}
