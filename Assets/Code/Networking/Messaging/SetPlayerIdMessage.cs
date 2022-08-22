using Mirror;

namespace Assets.Code.Networking.Messaging
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --SERVER-TO-CLIENT--
    /// </para>
    /// Tell a client what their given player id has been set to.
    /// This is used for validating certain inputs. For example,
    /// the client should only be able to select units who's owner
    /// player id matches theirs.
    /// </summary>
    public struct SetPlayerIdMessage : NetworkMessage
    {
        public int PlayerId;
    }
}
