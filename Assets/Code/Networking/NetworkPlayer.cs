using Mirror;
using UnityEngine;
using Assets.Code.Networking.Messaging;
using System;
using Assets.Code.GameTime;

namespace Assets.Code.Networking
{
    /// <summary>
    /// A class to represent a player.
    /// </summary>
    public class NetworkPlayer
    {
        /// <summary>
        /// A static singleton reference for use on the client. The client's
        /// owner NetworkPlayer object.
        /// </summary>
        public static NetworkPlayer AuthorityInstance { get; set; }

        /// <summary>
        /// A static int used for generating unique player id's on the server.
        /// </summary>
        private static int s_idCounter;

        /// <summary>
        /// A means of uniquely identifying a player on both server and client.
        /// </summary>
        public int PlayerId { get; private set; }

        /// <summary>
        /// The network connection associated with the player's client.
        /// </summary>
        private NetworkConnection _connectionToPlayer;

        /// <summary>
        /// A colors array which indices correspond with player id.
        /// </summary>
        public static readonly Color32[] Colors =
        {
            new Color32(171, 0, 18, 255), // red
            new Color32(195, 28, 146, 255), // pink
            new Color32(73, 18, 190, 255), // indigo
            new Color32(151, 0, 226, 255), // violet
            new Color32(5, 73, 241, 255), // blue
            new Color32(7, 163, 233, 255), // cyan
            new Color32(38, 190, 187, 255), // teal
            new Color32(0, 171, 43, 255), // green
            new Color32(140, 171, 0, 255), // lime
            new Color32(188, 156, 9, 255), // gold
            new Color32(204, 89, 31, 255), // orange
            new Color32(123, 65, 0, 255), // brown
        };

        /// <summary>
        /// An array of strings to hold the names of possible player owner colors.
        /// </summary>
        public static readonly string[] ColorNames =
        {
            "red", "pink", "indigo", "violet", "blue", "cyan", "teal", "green",
            "lime", "gold", "orange", "brown"
        };

        /// <summary>
        /// The backing field for UpgradePoints.
        /// </summary>
        private int _upgradePoints;

        /// <summary>
        /// The number of "upgrade points" a player has. Can be used for upgrading
        /// owned territories for better units.
        /// </summary>
        public int UpgradePoints 
        {
            get { return _upgradePoints; }
            set 
            { 
                _upgradePoints = value;
                if (NetworkServer.active)
                {
                    SetPlayerUpgradePointsMessage msg = new SetPlayerUpgradePointsMessage
                    {
                        UpgradePoints = value
                    };

                    _connectionToPlayer.Send(msg);
                }
            }
        }

        /// <summary>
        /// An event to notify subscribers when the client's upgrade
        /// points have been updated.
        /// </summary>
        public static event Action<int, int> ClientUpgradePointsUpdated;

        /// <summary>
        /// For server. On creation, get a unique player id.
        /// </summary>
        /// <param name="conn">The NetworkConnection associated with 
        /// this NetworkPlayer.</param>
        public NetworkPlayer(NetworkConnection conn)
        {
            if (!NetworkServer.active)
            {
                throw new InvalidOperationException("Cannot call default " +
                    "constructor on the client.");
            }
            GetUniqueId();
            _connectionToPlayer = conn;

            // Give passive upgrade points every 30 seconds.
            TickSystem.Tick += delegate ()
            {
                if (TickSystem.TickNumber % 300 == 0)
                {
                    UpgradePoints++;
                }
            };
        }

        /// <summary>
        /// For client. Create the owned NetworkPlayer with a 
        /// server-given id.
        /// </summary>
        /// <param name="id">The id given by the server.</param>
        public NetworkPlayer(int id)
        {
            PlayerId = id;
            AuthorityInstance = this;
            NetworkClient.RegisterHandler<SetPlayerUpgradePointsMessage>(UpdateUpgradePointsFromServer);
        }

        /// <summary>
        /// Get a new unique player id for this NetworkPlayer instance.
        /// </summary>
        private void GetUniqueId()
        {
            PlayerId = s_idCounter++;
        }

        /// <summary>
        /// Get a player color given an id.
        /// </summary>
        /// <param name="id">The given player id.</param>
        /// <returns>The color for the player id.</returns>
        public static Color GetColorForId(int id)
        {
            if (id >= Colors.Length)
            {
                id = Colors.Length - 1;
            }
            else if (id == -1)
            {
                return Color.black;
            }
            return Colors[id];
        }

        /// <summary>
        /// Get a player color name given an id.
        /// </summary>
        /// <param name="id">The given player id.</param>
        /// <returns>The color name for the player id.</returns>
        public static string GetColorNameForId(int id)
        {
            if (id >= ColorNames.Length)
            {
                id = ColorNames.Length - 1;
            }
            else if (id == -1)
            {
                return "Unowned";
            }
            return ColorNames[id];
        }

        /// <summary>
        /// Update the player's upgrade points after receiving a SetPlayerUpgradePointsMessage
        /// from the server.
        /// </summary>
        /// <param name="msg">The SetPlayerUpgradePointsMessage from the server.</param>
        private void UpdateUpgradePointsFromServer(SetPlayerUpgradePointsMessage msg)
        {
            int oldPoints = _upgradePoints;
            _upgradePoints = msg.UpgradePoints;
            ClientUpgradePointsUpdated?.Invoke(oldPoints, _upgradePoints);
        }
    }
}