using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Assets.Code.Networking.Messaging;
using Assets.Code.Hex;
using UnityEngine.SceneManagement;
using System;
using Assets.Code.GameTime;

namespace Assets.Code.Networking
{
    /// <summary>
    /// On the server, a class to handle player connections,
    /// their associated NetworkPlayer objects, connecting and
    /// disconnecting from the server, and the necessary procedures
    /// for when a player joins.
    /// 
    /// On the client, this handles connecting to the server and creating
    /// the owned NetworkPlayer instance.
    /// </summary>
    public class GameNetworkManager : NetworkManager
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static GameNetworkManager Instance { get; private set; }

        /// <summary>
        /// A map which keys are NetworkConnections mapping to NetworkPlayer objects for
        /// each player on the server.
        /// </summary>
        public static Dictionary<NetworkConnection, NetworkPlayer> NetworkPlayers;

        /// <summary>
        /// A map which keys are player id's mapping to NetworkPlayer objects for
        /// each player on the server.
        /// </summary>
        public static Dictionary<int, NetworkPlayer> NetworkPlayersById;

        /// <summary>
        /// The server HexWorld / map.
        /// </summary>
        [SerializeField] private HexWorld _hexWorld;

        /// <summary>
        /// An event to notify subscribers a client has disconnected.
        /// </summary>
        public static Action<NetworkConnection> ClientDisconnected;

        /// <summary>
        /// Register the necessary handler methods for receiving NetworkMessages.
        /// </summary>
        public override void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                // Duplicate, destroy.
                Destroy(gameObject);
                return;
            }

            base.Awake();
            NetworkServer.RegisterHandler<ClientReadyMessage>(SetupNewClient);
        }

        /// <summary>
        /// Initialize the GameNetworkManager.
        /// </summary>
        public override void OnStartServer()
        {
            NetworkPlayers = new Dictionary<NetworkConnection, NetworkPlayer>();
            NetworkPlayersById = new Dictionary<int, NetworkPlayer>();
            Debug.Log("Server started.");

            _hexWorld.GenerateNewWorld(false);
            Debug.Log("Generated server world. Using seed: " + _hexWorld.Seed);
        }

        /// <summary>
        /// Inform the client that they have successfully connected.
        /// </summary>
        /// <param name="conn">The client's NetworkConnection.</param>
        public override void OnServerConnect(NetworkConnection conn)
        {
            SuccessfulConnectPingMessage msg = new SuccessfulConnectPingMessage
            {

            };

            conn.Send(msg);
        }

        /// <summary>
        /// Create a new NetworkPlayer on the server for a newly joined client.
        /// Send a network message to the client to inform them of their assigned
        /// player id and the hex world map seed. Also, set ownership of an unoccupied
        /// territory for them.
        /// </summary>
        /// <param name="conn">The client's NetworkConnection.</param>
        /// <param name="msg">The ready message received by the server.</param>
        public void SetupNewClient(NetworkConnection conn, ClientReadyMessage msg)
        {
            CreateNewNetworkPlayer(conn);
            SendNewPlayerWorldSeed(conn);
            EraManager.Instance.SendClientEraData(conn);
            _hexWorld.SendNewClientTerritoryOwnershipStates(conn);
            _hexWorld.SendNewClientUnitStates(conn);
            HexTerritory newlyOwnedTerritory = GetNewPlayerTerritoryAndUnit(conn);
            if (newlyOwnedTerritory != null)
            {
                SetNewPlayerCameraPosition(conn, newlyOwnedTerritory);
            }
        }

        /// <summary>
        /// Remove the NetworkPlayer associated with the disconnecting client connection
        /// from the NetworkPlayers map.
        /// </summary>
        /// <param name="conn">The leaving client's connection.</param>
        public override void OnServerDisconnect(NetworkConnection conn)
        {
            if (NetworkPlayers.Count > 0)
            {
                ClientDisconnected?.Invoke(conn);
                NetworkPlayersById.Remove(NetworkPlayers[conn].PlayerId);
                NetworkPlayers.Remove(conn);
                Debug.Log("Removed player on disconnect, new player count: " +
                    NetworkPlayers.Count);
            }
        }

        /// <summary>
        /// Clear the NetworkPlayers dictionaries on server stop so mirror's internals
        /// destroy NetworkPlayer objects. This causes errors when OnServerDisconnect is 
        /// called internally.
        /// </summary>
        public override void OnStopServer()
        {
            NetworkPlayers.Clear();
            NetworkPlayersById.Clear();
        }

        /// <summary>
        /// Create a new NetworkPlayer object for a newly connected client.
        /// </summary>
        /// <param name="conn">The new client's connection.</param>
        private void CreateNewNetworkPlayer(NetworkConnection conn)
        {
            NetworkPlayer networkPlayer = new NetworkPlayer(conn);
            NetworkPlayers.Add(conn, networkPlayer);
            NetworkPlayersById.Add(networkPlayer.PlayerId, networkPlayer);

            SetPlayerIdMessage msg = new SetPlayerIdMessage
            {
                PlayerId = networkPlayer.PlayerId
            };
            conn.Send(msg);

            Debug.Log("NetworkPlayer added, id :" + NetworkPlayers[conn].PlayerId);
        }

        /// <summary>
        /// Send the newly joined client the loaded HexWorld seed.
        /// </summary>
        /// <param name="conn">The new client's connection.</param>
        private void SendNewPlayerWorldSeed(NetworkConnection conn)
        {
            SetWorldSeedMessage msg = new SetWorldSeedMessage
            {
                Seed = _hexWorld.Seed
            };
            conn.Send(msg);
            Debug.Log("Sent world seed: " + msg.Seed);
        }

        /// <summary>
        /// Get an available HexTerritory for a newly joined player to own.
        /// Also, spawn them their first unit.
        /// </summary>
        /// <param name="newConn">The new client's connection.</param>
        /// <returns>The territory owned by the new player. Null if none available.</returns>
        private HexTerritory GetNewPlayerTerritoryAndUnit(NetworkConnection newConn)
        {
            HexTerritory territory = _hexWorld.GetUnownedTerritory();
            if (territory != null)
            {
                territory.SetStarterUnitSpawnType();
                territory.SetOwnership(NetworkPlayers[newConn].PlayerId);
                Debug.Log("Set territory: " + territory.Id + " ownership to " + NetworkPlayers[newConn].PlayerId);
            }
            int startingSpawnAmount = 3;
            for (int i = 0; i < startingSpawnAmount; i++)
            {
                territory.TrySpawnUnit();
            }
            return territory;
        }

        /// <summary>
        /// Tell a newly joined client's application to set the camera position
        /// to its new HexTerritory.
        /// </summary>
        /// <param name="conn">The new client's connection.</param>
        private void SetNewPlayerCameraPosition(NetworkConnection conn, HexTerritory territory)
        {
            SetCameraViewMessage msg = new SetCameraViewMessage
            {
                Position = territory.Capital.Position + new Vector3(0f, 10f, 0f)
            };
            conn.Send(msg);
        }


        /// <summary>
        /// Create a new network client if it does not exist and attempt to connect to the
        /// supplied address.
        /// </summary>
        /// <param name="address">The network address to connect to.</param>
        /// <returns>True if connecting to valid address, false otherwise.</returns>
        public bool TryConnect(string address)
        {
            networkAddress = address;
            try
            {
                if (!NetworkClient.active)
                {
                    StartClient();
                    NetworkClient.RegisterHandler<SuccessfulConnectPingMessage>(LoadGameSceneOnConnect);
                    NetworkClient.RegisterHandler<SetPlayerIdMessage>(CreateNetworkPlayerFromServer);
                }
                else
                {
                    NetworkClient.Connect(networkAddress);
                }
                return true;
            }
            catch (System.Net.Sockets.SocketException)
            {
                StopClient();
                return false;
            }
        }

        /// <summary>
        /// Load the client came scene after receiving a SuccessfulConnectPingMessage
        /// from the server.
        /// </summary>
        /// <param name="msg">The SuccessfulConnectPingMessage sent by the server.</param>
        private void LoadGameSceneOnConnect(SuccessfulConnectPingMessage msg)
        {
            SceneManager.sceneLoaded += OnGameSceneLoad;
            SceneManager.LoadScene("ClientScene", LoadSceneMode.Single);
        }

        /// <summary>
        /// When the game scene loads, send the server a ClientReadyMessage.
        /// </summary>
        /// <param name="scene">The ClientScene.</param>
        /// <param name="mode">The LoadSceneMode.</param>
        private void OnGameSceneLoad(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnGameSceneLoad;

            ClientReadyMessage msg = new ClientReadyMessage
            {

            };

            NetworkClient.Send(msg);
        }

        /// <summary>
        /// Create the owned NetworkPlayer object with the id given by the server after
        /// receiving a SetPlayerIdMessage from the server.
        /// </summary>
        /// <param name="msg">The SetPlayerIdMessage sent by the server.</param>
        private void CreateNetworkPlayerFromServer(SetPlayerIdMessage msg)
        {
            new NetworkPlayer(msg.PlayerId);
            Debug.Log("Created player with id: " + NetworkPlayer.AuthorityInstance.PlayerId);
        }
    }
}
