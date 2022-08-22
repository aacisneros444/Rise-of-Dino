using UnityEngine;
using Mirror;
using System.Collections.Generic;
using Assets.Code.Networking.Messaging;
using Assets.Code.Networking;
using Assets.Code.Units;
using Assets.Code.GameTime;
using System;

namespace Assets.Code.Hex
{
    /// <summary>
    /// A class to model a hex world, a structure which holds
    /// and manipulates the components necessary to compose and 
    /// operate a hexagonal world.
    /// </summary>
    public class HexWorld : MonoBehaviour
    {
        /// <summary>
        /// The HexGrid which stores world data.
        /// </summary>
        [SerializeField] private HexGrid _hexGrid;

        /// <summary>
        /// The HexMapGenerator used to generate the map.
        /// </summary>
        [SerializeField] private HexMapGenerator _mapGenerator;

        /// <summary>
        /// The seed for the loaded hex world map.
        /// </summary>
        public int Seed { get; set; }

        /// <summary>
        /// A list of all HexTerritories on the map.
        /// </summary>
        private List<HexTerritory> _territories;

        /// <summary>
        /// A dictionary of all the unowned HexTerritories on the map
        /// where each key is the territory id.
        /// </summary>
        private Dictionary<int, HexTerritory> _unownedTerritories;

        /// <summary>
        /// A map holding HexTerritories by their ids. Used for easy referencing when 
        /// messages are received regarding HexTerritories.
        /// </summary>
        private Dictionary<int, HexTerritory> _hexTerritories;

        /// <summary>
        /// A HashSet containing all the units currently present on this HexWorld.
        /// </summary>
        public HashSet<Unit> _units;

        /// <summary>
        /// Register the necessary handler methods for receiving NetworkMessages.
        /// Subscibers to unit create and destroy events to store units for sending
        /// to new clients.
        /// Create the necessary data structures.
        /// </summary>
        private void Awake()
        {
            Unit.UnitCreated += StoreUnitReferenceOnCreation;
            Unit.UnitDestroyed += RemoveUnitReferenceOnDestroy;
            _units = new HashSet<Unit>();
            _unownedTerritories = new Dictionary<int, HexTerritory>();

            NetworkClient.RegisterHandler<SetWorldSeedMessage>(GenerateNewWorldFromServer);
            NetworkClient.RegisterHandler<SetTerritoryOwnershipMessage>(SetTerritoryOwnershipFromServer);
            NetworkClient.RegisterHandler<SetTerritoryStatesMessage>(SetTerritoryStatesFromServer);
            NetworkClient.RegisterHandler<SetUnitStatesMessage>(SetUnitStatesFromServer);
            NetworkClient.RegisterHandler<SetTerritorySpawnTicksElapsedMessage>(SetTerritorySpawnTicksFromServer);
            NetworkClient.RegisterHandler<SetTerritoryUnitSpawnTypeMessage>(SetTerritoryUnitSpawnTypeFromServer);
            NetworkClient.RegisterHandler<SetTerritoryLevelMessage>(SetTerritoryLevelFromServer);
            NetworkClient.ReplaceHandler<SetAllCellsVisibleMessage>(SetAllCellsVisibleFromServer);
            if (!NetworkClient.active)
            {
                NetworkServer.RegisterHandler<ToggleTerritorySpawnTicksUpdateMessage>
                    (HandleSpawnTickUpdatesRequestForTerritoryFromClient);
                NetworkServer.RegisterHandler<RequestTerritoryUpgradeMessage>(TryUpgradeTerritoryFromClient);
                NetworkServer.RegisterHandler<TerritorySpawnTypeRerollMessage>(TryTerritoryRerollUnitSpawnTypeFromClient);
            }
        }

        /// <summary>
        /// Generate a new world map on the server.
        /// </summary>
        /// <param name="useMeshes">If true, will create meshes for 
        /// visualization. If false, will run headless.</param>>
        public void GenerateNewWorld(bool useMeshes)
        {
            Seed = _mapGenerator.GenerateNewMap(_hexGrid, useMeshes);
            _territories = _mapGenerator.CreateTerritories(_hexGrid, useMeshes);
            CreateTerritoryDictionary(_territories);
            if (NetworkServer.active)
            {
                // Store unowned territories to assign them to new players.
                for (int i = 0; i < _territories.Count; i++)
                {
                    HexTerritory territory = _territories[i];
                    _unownedTerritories.Add(territory.Id, territory);
                    // Subscribe to territory ownership changes to remove owned territories
                    // from the _unownedTerritories list.
                    territory.OwnershipChanged += RemoveOwnedTerritoryFromUnownedDict;
                }
            }
        }

        /// <summary>
        /// Create a dictionary of HexTerritory accessed by their id.
        /// </summary>
        private void CreateTerritoryDictionary(List<HexTerritory> generatedTerritories)
        {
            // Store the generated territories in a map for later referencing
            // when messages come in from ther server regarding them.
            Dictionary<int, HexTerritory> territoryMap = new Dictionary<int, HexTerritory>();
            for (int i = 0; i < generatedTerritories.Count; i++)
            {
                HexTerritory territory = generatedTerritories[i];
                territoryMap.Add(territory.Id, territory);
            }
            _hexTerritories = territoryMap;
        }

        /// <summary>
        /// Get a random unowned HexTerritory.
        /// </summary>
        /// <returns>An unowned HexTerritory if one exists, null if none exist.</returns>
        public HexTerritory GetUnownedTerritory()
        {
            if (_unownedTerritories.Count == 0)
            {
                return null;
            }
            List<int> keys = new List<int>(_unownedTerritories.Keys);
            return _unownedTerritories[keys[UnityEngine.Random.Range(0, _unownedTerritories.Count - 1)]];
        }

        /// <summary>
        /// Store a reference to a newly created unit when notified of
        /// unit creation.
        /// </summary>
        /// <param name="unit">The newly created unit.</param>
        private void StoreUnitReferenceOnCreation(Unit unit)
        {
            _units.Add(unit);
        }

        /// <summary>
        /// Remove a reference to a unit that will be destroyed when notified.
        /// </summary>
        /// <param name="unit">The unit that will be destroyed.</param>
        private void RemoveUnitReferenceOnDestroy(Unit unit)
        {
            _units.Remove(unit);
        }

        /// <summary>
        /// Tell a newly joined client to set the current owner player id's
        /// for all HexTerritories.
        /// </summary>
        /// <param name="conn">The new client's connection.</param>
        public void SendNewClientTerritoryOwnershipStates(NetworkConnection conn)
        {
            int[] territoryIds = new int[_territories.Count];
            int[] ownerPlayerIds = new int[_territories.Count];
            int[] unitSpawnTypeIds = new int[_territories.Count];
            int[] territoryLevels = new int[_territories.Count];
            int i = 0;
            foreach (HexTerritory territory in _territories)
            {
                territoryIds[i] = territory.Id;
                ownerPlayerIds[i] = territory.OwnerPlayerId;
                unitSpawnTypeIds[i] = territory.UnitSpawnType.Id;
                territoryLevels[i] = territory.Level;
                i++;
            }

            SetTerritoryStatesMessage msg = new SetTerritoryStatesMessage
            {
                TerritoryIds = territoryIds,
                OwnerPlayerIds = ownerPlayerIds,
                UnitSpawnTypeIds = unitSpawnTypeIds,
                Levels = territoryLevels
            };

            conn.Send(msg);
        }

        /// <summary>
        /// Tell a newly joined client about all units and their states
        /// in the HexWorld.
        /// </summary>
        /// <param name="conn">The new client's connection.</param>
        public void SendNewClientUnitStates(NetworkConnection conn)
        {
            int[] unitTypeIds = new int[_units.Count];
            int[] unitCellIndices = new int[_units.Count];
            int[] unitOwnerPlayerIds = new int[_units.Count];
            int[] unitHealthValues = new int[_units.Count ];

            int i = 0;
            foreach (Unit unit in _units)
            {
                unitTypeIds[i] = unit.Data.Id;
                unitCellIndices[i] = unit.OccupiedCell.Index;
                unitOwnerPlayerIds[i] = unit.OwnerPlayerId;
                unitHealthValues[i] = unit.Health.Value;
                i++;
            }

            SetUnitStatesMessage msg = new SetUnitStatesMessage
            {
                UnitTypeIds = unitTypeIds,
                UnitCellIndices = unitCellIndices,
                UnitOwnerPlayerIds = unitOwnerPlayerIds,
                UnitHealthValues = unitHealthValues
            };

            conn.Send(msg);
        }
        
        /// <summary>
        /// If a territory was previously unowned and it has been captured, remove it from
        /// the unowned dictionary.
        /// </summary>
        /// <param name="hexTerritory">The territory to remove, if applicable.</param>
        private void RemoveOwnedTerritoryFromUnownedDict(HexTerritory hexTerritory)
        {
            if (_unownedTerritories[hexTerritory.Id] != null)
            {
                _unownedTerritories.Remove(hexTerritory.Id);
                hexTerritory.OwnershipChanged -= RemoveOwnedTerritoryFromUnownedDict;
            }
        }

        /// <summary>
        /// Start or stop sending the client spawn ticks for a HexTerritory after receiving a
        /// ToggleTerritorySpawnTicksUpdateMessage from the client.
        /// </summary>
        /// <param name="conn">The connection for the client.</param>
        /// <param name="msg">The ToggleTerritorySpawnTicksUpdateMessage from the client.</param>
        private void HandleSpawnTickUpdatesRequestForTerritoryFromClient(NetworkConnection conn, 
            ToggleTerritorySpawnTicksUpdateMessage msg)
        {
            _hexTerritories[msg.TerritoryId].ToggleSendSpawnTickUpdateToClient(conn, msg.SendUpdates);
        }

        /// <summary>
        /// Try upgrading a HexTerritory owned by a client after receiving a RequestTerritoryUpgradeMessage
        /// from a client.
        /// </summary>
        /// <param name="conn">The connection for the client.</param>
        /// <param name="msg">The RequestTerritoryUpgradeMessage from the client.</param>
        private void TryUpgradeTerritoryFromClient(NetworkConnection conn, RequestTerritoryUpgradeMessage msg)
        {
            HexTerritory territory = _hexTerritories[msg.TerritoryId];
            Networking.NetworkPlayer networkPlayer = GameNetworkManager.NetworkPlayers[conn];
            if (territory.Level < HexTerritory.MaxLevel && 
                networkPlayer.UpgradePoints >= territory.UpgradeCost && 
                territory.Level < EraManager.Instance.MaxTerritoryEraLevel)
            {
                networkPlayer.UpgradePoints -= territory.UpgradeCost;
                territory.LevelUpTerritory();
            }
        }

        /// <summary>
        /// Try getting a new unit spawn type of the same level for a HexTerritory after
        /// receiving a TerritorySpawnTypeRerollMessage from the client.
        /// </summary>
        /// <param name="conn">The connection for the client.</param>
        /// <param name="msg">The TerritorySpawnTypeRerollMessage from the client.</param>
        private void TryTerritoryRerollUnitSpawnTypeFromClient(NetworkConnection conn, 
            TerritorySpawnTypeRerollMessage msg)
        {
            HexTerritory territory = _hexTerritories[msg.TerritoryId];
            Networking.NetworkPlayer networkPlayer = GameNetworkManager.NetworkPlayers[conn];
            if (networkPlayer.UpgradePoints >= HexTerritory.RerollCost)
            {
                networkPlayer.UpgradePoints -= HexTerritory.RerollCost;
                territory.RerollUnitSpawnType();
            }
        }

        /// <summary>
        /// Generate the world map on the client after receiving a SetWorldSeedMessage 
        /// from the server.
        /// </summary>
        /// <param name="msg">The SetWorldSeedMessage sent by the server.</param>
        public void GenerateNewWorldFromServer(SetWorldSeedMessage msg)
        {
            Debug.Log("Received seed: " + msg.Seed + ", generating.");
            Seed = msg.Seed;
            _mapGenerator.GenerateNewMap(_hexGrid, msg.Seed, true);
            List<HexTerritory> territories = _mapGenerator.CreateTerritories(_hexGrid, true);

            // Store the generated territories in a map for later referencing
            // when messages come in from ther server regarding them.
            CreateTerritoryDictionary(territories);
        }

        /// <summary>
        /// Set ownership of a HexTerritory to a certain player after receiving a 
        /// SetTerritoryOwnershipMessage from the server.
        /// </summary>
        /// <param name="msg">The SetTerritoryOwnershipMessage sent by the server.</param>
        private void SetTerritoryOwnershipFromServer(SetTerritoryOwnershipMessage msg)
        {
            _hexTerritories[msg.TerritoryId].SetOwnership(msg.PlayerId);
            Debug.Log("Set territory: " + msg.TerritoryId + " ownership to " + msg.PlayerId);
        }

        /// <summary>
        /// Set the ownership and unit spawn types of all HexTerritories after receiving a 
        /// SetTerritoryStatesFromServer message from the server.
        /// </summary>
        /// <param name="msg">The SetTerritoryStatesFromServer sent by ther server.</param>
        private void SetTerritoryStatesFromServer(SetTerritoryStatesMessage msg)
        {
            int numTerritories = 0;
            for (int i = 0; i < msg.TerritoryIds.Length; i++)
            {
                HexTerritory territory = _hexTerritories[msg.TerritoryIds[i]];
                territory.SetOwnership(msg.OwnerPlayerIds[i]);
                territory.UnitSpawnType = UnitDataLookup.Instance.GetUnitData(msg.UnitSpawnTypeIds[i]);
                territory.Level = msg.Levels[i];
                numTerritories++;
            }
            Debug.Log("Territory states set for: " + numTerritories + " territories from the server.");
        }

        /// <summary>
        /// Create all the HexWorld units and set their state after receiving a 
        /// SetUnitStatesMessage from the server.
        /// </summary>
        /// <param name="msg">The SetUnitStatesMessage from the server.</param>
        private void SetUnitStatesFromServer(SetUnitStatesMessage msg)
        {
            int numUnits = 0;
            for (int i = 0; i < msg.UnitCellIndices.Length; i++)
            {
                Unit unit;
                if (msg.UnitTypeIds[i] != 2)
                {
                    // If not a HexTerritory capital (which are already deterministically created
                    // on the client), create a new Unit.
                    unit = new Unit(UnitDataLookup.Instance.GetUnitData(msg.UnitTypeIds[i]),
                        _hexGrid.GetCell(msg.UnitCellIndices[i]), msg.UnitOwnerPlayerIds[i], true);
                    numUnits++;
                }
                else
                {
                    // Get the HexTerritory capital to set its health.
                    unit = _hexGrid.GetCell(msg.UnitCellIndices[i]).Unit;
                }
                unit.Health.SetHealthValue(msg.UnitHealthValues[i]);
            }
            Debug.Log("Created " + numUnits + " units on start from server.");
        }

        /// <summary>
        /// Set a territory's unit spawn type after receiving a SetTerritoryUnitSpawnTypeMessage
        /// from ther server.
        /// </summary>
        /// <param name="msg">The SetTerritoryUnitSpawnTypeMessage from the server.</param>
        private void SetTerritoryUnitSpawnTypeFromServer(SetTerritoryUnitSpawnTypeMessage msg)
        {
            UnitData unitSpawnTypeData = UnitDataLookup.Instance.GetUnitData(msg.UnitSpawnTypeId);

            _hexTerritories[msg.TerritoryId].UnitSpawnType = unitSpawnTypeData;

            Debug.Log("Set territory unit spawn type to: " + unitSpawnTypeData.Name);
        }

        /// <summary>
        /// Set the spawn ticks elapsed for a territory after receiving a 
        /// SetTerritorySpawnTicksElapsedMessage from the server.
        /// </summary>
        /// <param name="msg">The SetTerritorySpawnTicksElapsedMessage from the server.</param>
        private void SetTerritorySpawnTicksFromServer(SetTerritorySpawnTicksElapsedMessage msg)
        {
            _hexTerritories[msg.TerritoryId].ClientTicksSinceLastSpawn = msg.TicksElapsed;
        }

        /// <summary>
        /// Set a territory's level after receiving a SetTerritoryLevelMessage
        /// from the server.
        /// </summary>
        /// <param name="msg">The SetTerritoryLevelMessage from the server.</param>
        private void SetTerritoryLevelFromServer(SetTerritoryLevelMessage msg)
        {
            _hexTerritories[msg.TerritoryId].Level = msg.Level;
            Debug.Log("Set territory " + msg.TerritoryId + " to " + msg.Level);
        }

        // TO MOVE-----
        public static event Action<int, int> ClientGameEnded;

        /// <summary>
        /// Make all grid cells visible after receiving a SetAllCellsVisibleMessage
        /// from the server.
        /// </summary>
        /// <param name="msg">The SetAllCellsVisibleMessage from the server.</param>
        private void SetAllCellsVisibleFromServer(SetAllCellsVisibleMessage msg)
        {
            List<HexCell> allCells = new List<HexCell>();
            for (int i = 0; i < _hexGrid.CellCount; i++)
            {
                allCells.Add(_hexGrid.GetCell(i));
            }
            HexCellVisibilityManager.UpdateVisibilityForCellGroup(allCells, true);

            // ----To Move!----
            Dictionary<int, int> playerIdToTerritoryCount = new Dictionary<int, int>();
            foreach (HexTerritory territory in _hexTerritories.Values)
            {
                if (territory.OwnerPlayerId != HexTerritory.Unowned)
                {
                    if (playerIdToTerritoryCount.TryGetValue(territory.OwnerPlayerId, out int numTerritories))
                    {
                        playerIdToTerritoryCount[territory.OwnerPlayerId] = numTerritories + 1;
                    }
                    else
                    {
                        playerIdToTerritoryCount.Add(territory.OwnerPlayerId, 1);
                    }
                }
            }
            int winningPlayer = 0;
            int numPlayerTerritories = 0;
            foreach(int playerId in playerIdToTerritoryCount.Keys)
            {
                if (playerIdToTerritoryCount[playerId] > numPlayerTerritories)
                {
                    winningPlayer = playerId;
                    numPlayerTerritories = playerIdToTerritoryCount[playerId];
                }
            }
            ClientGameEnded?.Invoke(winningPlayer, numPlayerTerritories);
        }
    }
}