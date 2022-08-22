using Mirror;

namespace Assets.Code.Networking.Messaging
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --SERVER-TO-CLIENT--
    /// </para>
    /// Tell the client the current era and elapsed ticks
    /// within the era.
    /// </summary>
    public struct SetInitialEraStateMessage : NetworkMessage
    {
        public int EraIndex;
        public int TicksElapsed;
    }
}
