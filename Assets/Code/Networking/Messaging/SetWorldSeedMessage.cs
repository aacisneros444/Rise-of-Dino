using Mirror;

namespace Assets.Code.Networking.Messaging
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --SERVER-TO-CLIENT--
    /// </para>
    /// Tell a client what world seed the server is using.
    /// </summary>
    public struct SetWorldSeedMessage : NetworkMessage
    {
        public int Seed;
    }
}
