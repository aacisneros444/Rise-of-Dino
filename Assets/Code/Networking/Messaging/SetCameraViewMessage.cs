using Mirror;
using UnityEngine;

namespace Assets.Code.Networking.Messaging
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --SERVER-TO-CLIENT--
    /// </para>
    /// Tell a client application to set its camera position to 
    /// a certain position.
    /// </summary>
    public struct SetCameraViewMessage : NetworkMessage
    {
        public Vector3 Position;
    }
}
