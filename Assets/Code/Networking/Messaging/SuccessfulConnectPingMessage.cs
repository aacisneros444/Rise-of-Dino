using Mirror;

namespace Assets.Code.Networking.Messaging
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --SERVER-TO-CLIENT--
    /// </para>
    /// Ping the client. When this message is received, the client
    /// application knows it has connected successfully, and can 
    /// then request the game state.
    /// </summary>
    public struct SuccessfulConnectPingMessage : NetworkMessage
    {

    }
}
