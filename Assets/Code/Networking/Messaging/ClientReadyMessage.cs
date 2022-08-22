using Mirror;

/// <summary>
/// A network message struct to be sent across the network.
/// <para>
/// --CLIENT-TO-SERVER--
/// </para>
/// Inform the server that the client is ready to play and
/// be sent the game state.
/// </summary>
public struct ClientReadyMessage : NetworkMessage
{

}
