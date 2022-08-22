using Mirror;

namespace Assets.Code.Networking.Messaging
{
    /// <summary>
    /// A network message struct to be sent across the network.
    /// <para>
    /// --SERVER-TO-CLIENT--
    /// </para>
    /// Tell clients to move a Unit to a certain HexCell.
    /// </summary>
    public struct MoveUnitMessage : NetworkMessage
    {
        public int UnitCellIndex;
        public int ToCellIndex;
    }
}
