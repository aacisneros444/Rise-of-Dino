using System.Collections.Generic;
using Mirror;
using Assets.Code.Networking;
using Assets.Code.Units;
using Assets.Code.GameTime;
using Assets.Code.Networking.Messaging;
using System;

namespace Assets.Code.Hex
{
    /// <summary>
    /// A class to model a chunk of HexCells that can be owned
    /// by a player.
    /// </summary>
    public class HexTerritory
    {
        /// <summary>
        /// The unique identifier for the territory.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// The capital HexCell.
        /// </summary>
        public HexCell Capital { get; private set; }

        /// <summary>
        /// The HexCells that belong to this territory.
        /// </summary>
        public List<HexCell> _memberCells;

        /// <summary>
        /// The HexCells along a territorial border.
        /// </summary>
        private List<HexCell> _borderCells;

        /// <summary>
        /// If using mesh, the associated HexTerritoryMesh
        /// for this HexTerritory.
        /// </summary>
        public HexTerritoryMesh HexTerritoryMesh { get; private set; }

        /// <summary>
        /// An int denoting that this HexTerritory is unowned.
        /// </summary>
        public const int Unowned = -1;

        /// <summary>
        /// The player id of the player this HexTerritory belongs to.
        /// If -1, the territory is unowned.
        /// </summary>
        public int OwnerPlayerId { get; private set; }


        /// <summary>
        /// The backing field for Level.
        /// </summary>
        private int _level;

        /// <summary>
        /// The upgrade level of this HexTerritory.
        /// </summary>
        public int Level 
        {
            get { return _level; }
            set
            {
                _level = value;
                if (NetworkClient.active) 
                {
                    ClientLevelChanged?.Invoke(this);
                }
            }
        }

        /// <summary>
        /// The max upgrade level for HexTerritories.
        /// </summary>
        public const int MaxLevel = 3;

        /// <summary>
        /// The cost in upgrade points to upgrade once and twice.
        /// </summary>
        public const int FirstUpgradeCost = 10, SecondUpgradeCost = 25;

        /// <summary>
        /// The current cost to upgrade the HexTerritory.
        /// </summary>
        public int UpgradeCost 
        { 
            get 
            { 
                if (Level == 1)
                {
                    return FirstUpgradeCost;
                }
                else
                {
                    return SecondUpgradeCost;
                }
            } 
        }

        /// <summary>
        /// The cost in upgrade points to "reroll" for a different unit
        /// of the same level as the territory's level.
        /// </summary>
        public const int RerollCost = 5;

        /// <summary>
        /// Denotes whether or not this territory will spawn land or water units.
        /// True for land units, false for aquatic units. Note: in both cases, amphibious
        /// units may still spawn.
        /// </summary>
        public bool SpawnsLandUnits { get; private set; }

        /// <summary>
        /// The capital HexCell unit.
        /// </summary>
        private Unit _capitalUnit;

        /// <summary>
        /// The backing field for UnitSpawnType.
        /// </summary>
        private UnitData _unitSpawnType;

        /// <summary>
        /// The UnitData for the type of unit this territory will spawn.
        /// </summary>
        public UnitData UnitSpawnType 
        {
            get { return _unitSpawnType; }
            set
            {
                _unitSpawnType = value;
                if (NetworkClient.active)
                {
                    ClientSpawnTypeChanged?.Invoke(_unitSpawnType);
                }
            }
        }

        /// <summary>
        /// Denotes if this HexTerritory is subscribe to the Tick event of the
        /// TickSystem.
        /// </summary>
        private bool _isTickListener;

        /// <summary>
        /// The number of ticks elapsed since the last unit was spawned.
        /// </summary>
        private int _ticksSinceLastUnitSpawn;

        /// <summary>
        /// The number of ticks elapsed since the last unit was spawned (for client).
        /// </summary>
        public int ClientTicksSinceLastSpawn 
        {
            get { return _ticksSinceLastUnitSpawn; }
            set { _ticksSinceLastUnitSpawn = value; ClientSpawnTicksChanged?.Invoke(this); }
        }

        /// <summary>
        /// The number of ticks elapsed since the territory last healed
        /// allied units within it.
        /// </summary>
        private int _ticksSinceLastHeal;

        /// <summary>
        /// The number of ticks necessary to elapse in order to heal allied
        /// units within the territory.
        /// </summary>
        private const int HealTicks = 150;

        /// <summary>
        /// An event to notify subscribers when ownership of the HexTerritory
        /// has changed.
        /// </summary>
        public event Action<HexTerritory> OwnershipChanged;

        /// <summary>
        /// An event to notify subscribers when ownership of the HexTerritory
        /// has changed on the client.
        /// </summary>
        public event Action<HexTerritory> ClientOwnershipChanged;

        /// <summary>
        /// An event to notify subscribers on the client when the spawn ticks
        /// elapsed has changed.
        /// </summary>
        public event Action<HexTerritory> ClientSpawnTicksChanged;

        /// <summary>
        /// An event to notify subscribers on the client when the unit spawn type
        /// has changed.
        /// </summary>
        public event Action<UnitData> ClientSpawnTypeChanged;

        /// <summary>
        /// An event to notify subscribers on the client when the territory's
        /// level has changed.
        /// </summary>
        public event Action<HexTerritory> ClientLevelChanged;

        /// <summary>
        /// A list of clients currently requesting to be informed
        /// of ticks elapsed since last unit spawn.
        /// </summary>
        public List<NetworkConnection> _spawnTickRequestingClients { get; private set; }

        /// <summary>
        /// Create a HexTerritory, assigning its id, and its capital HexCell.
        /// </summary>
        /// <param name="id">The HexTerritory's unique id.</param>
        /// <param name="capital">The HexTerritory's capital HexCell.</param>
        public HexTerritory(int id, HexCell capital)
        {
            Id = id;
            Capital = capital;
            capital.Territory = this;
            OwnerPlayerId = Unowned;
            Level = 1;

            _memberCells = new List<HexCell>();
            _memberCells.Add(capital);
            _borderCells = new List<HexCell>();

            _capitalUnit = new TerritoryCapitalUnit(
                UnitDataLookup.Instance.GetTerritoryCapitalUnit(),
                capital, Unowned, this);

            _spawnTickRequestingClients = new List<NetworkConnection>();
            // Listen for client disconnects to remove any unnecessary NetworkConnection
            // objects from the __spawnTickRequestingClients list.
            GameNetworkManager.ClientDisconnected += delegate (NetworkConnection conn)
            {
                _spawnTickRequestingClients.Remove(conn);
            };
        }

        /// <summary>
        /// Create a HexTerritory, assinging its id, its capital HexCell, 
        /// and the HexTerritoryMesh prefab instance containing components 
        /// for mesh rendering.
        /// </summary>
        /// <param name="id">The HexTerritory's unique id.</param>
        /// <param name="capital">The HexTerritory's capital HexCell.</param>
        /// <param name="meshPrefabInstance">An instance of the HexTerritoryMesh 
        /// prefab containing components for mesh rendering.</param>
        public HexTerritory(int id, HexCell capital,
            HexTerritoryMesh meshPrefabInstance) : this(id, capital)
        {
            HexTerritoryMesh = meshPrefabInstance;
        }

        /// <summary>
        /// Assign a HexCell to this HexTerritory.
        /// </summary>
        /// <param name="cell">The HexCell to assign to this
        /// HexTerritory.</param>
        public void AddMemberCell(HexCell cell)
        {
            _memberCells.Add(cell);
        }

        /// <summary>
        /// Assign a HexCell as a border cell to this HexTerritory.
        /// </summary>
        /// <param name="cell">The HexCell to assign as a border 
        /// cell to this HexTerritory.</param>
        public void AddBorderCell(HexCell cell)
        {
            _borderCells.Add(cell);
        }

        /// <summary>
        /// Refresh the HexTerritory border mesh.
        /// </summary>
        /// <param name="updateNeighbors">Denotes whether or not neighboring
        /// HexTerritory's meshes should also be updated.</param>
        public void RefreshMesh(bool updateNeighbors)
        {
            if (HexTerritoryMesh == null)
            {
                throw new InvalidOperationException("Must be using meshes " +
                    "to call RefreshMesh().");
            }
            HexTerritoryMesh.Create(_borderCells, OwnerPlayerId);
            if (updateNeighbors)
            {
                HexTerritoryMesh.UpdateNeighboringMeshes(_borderCells);
            }
        }

        /// <summary>
        /// Set this territory to be owned by a given player. If ran on server,
        /// notify clients of the ownership change and subscribe to the Tick event
        /// to spawn units periodically. If ran on the client, will refresh
        /// the HexTerritoryMesh to reflect the ownership change.
        /// </summary>
        /// <param name="playerId">The given player id.</param>
        public void SetOwnership(int playerId)
        {
            int oldOwnerId = OwnerPlayerId;
            OwnerPlayerId = playerId;
            _capitalUnit.OwnerPlayerId = playerId;
            if (NetworkServer.active)
            {
                SetTerritoryOwnershipMessage msg = new SetTerritoryOwnershipMessage
                {
                    TerritoryId = Id,
                    PlayerId = playerId
                };

                foreach (NetworkConnection conn in GameNetworkManager.NetworkPlayers.Keys)
                {
                    conn.Send(msg);
                }

                if (!_isTickListener)
                {
                    TickSystem.Tick += OnTick;
                    _isTickListener = true;
                }

                OwnershipChanged?.Invoke(this);
            }
            else
            {
                RefreshMesh(true);

                int localPlayerId = Networking.NetworkPlayer.AuthorityInstance.PlayerId;
                if (oldOwnerId == localPlayerId || OwnerPlayerId == localPlayerId)
                {
                    // Only update cell visibility if this territory was previously owned or
                    // is now owned by the local player.
                    HexCellVisibilityManager.
                        UpdateVisibilityForCellGroup(_memberCells, OwnerPlayerId == localPlayerId);
                }

                ClientOwnershipChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Get the UnitData for the initital unit type this territory will spawn.
        /// </summary>
        public void PickInitialUnitTypeToSpawn()
        {
            int numWaterCells = 0;
            for (int i = 0; i < _memberCells.Count; i++)
            {
                if (!_memberCells[i].TerrainType.IsLand)
                {
                    numWaterCells++;
                }
            }
            float waterPercentage = (float)numWaterCells / _memberCells.Count;

            if (waterPercentage > 0.75f)
            {
                // Territory is mostly water, guaranteed water unit spawn location.
                SpawnsLandUnits = false;
                UnitSpawnType = UnitDataLookup.Instance.GetRandomWaterUnit(1);
            }
            else if (waterPercentage > 0.5f)
            {
                // Territory is about balanced in water and land. May spawn land or water unit.
                if (UnityEngine.Random.value > 0.5f)
                {
                    SpawnsLandUnits = true;
                    UnitSpawnType = UnitDataLookup.Instance.GetRandomLandUnit(1);
                }
                else
                {
                    SpawnsLandUnits = false;
                    UnitSpawnType = UnitDataLookup.Instance.GetRandomWaterUnit(1);
                }
            }
            else
            {
                // Territory is mostly land. Will spawn a land unit.
                SpawnsLandUnits = true;
                UnitSpawnType = UnitDataLookup.Instance.GetRandomLandUnit(1);
            }
        }

        /// <summary>
        /// Get a new, distinct, UnitData for a unit type this territory will spawn.
        /// </summary>
        public void PickNewUnitTypeToSpawn()
        {
            UnitData newSpawnType = UnitSpawnType;
            while(newSpawnType == UnitSpawnType)
            {
                if (!UnitSpawnType.IsAmphibious && UnitSpawnType.IsWalker)
                {
                    newSpawnType = UnitDataLookup.Instance.GetRandomLandUnit(Level);
                }
                else if (!UnitSpawnType.IsAmphibious && UnitSpawnType.IsSwimmer)
                {
                    newSpawnType = UnitDataLookup.Instance.GetRandomWaterUnit(Level);
                }
                else
                {
                    // Spawn type is amphibious.
                    if (SpawnsLandUnits)
                    {
                        newSpawnType = UnitDataLookup.Instance.GetRandomLandUnit(Level);
                    }
                    else
                    {
                        newSpawnType = UnitDataLookup.Instance.GetRandomWaterUnit(Level);
                    }
                }
            }

            UnitSpawnType = newSpawnType;

            SetTerritoryUnitSpawnTypeMessage msg = new SetTerritoryUnitSpawnTypeMessage
            {
                TerritoryId = Id,
                UnitSpawnTypeId = UnitSpawnType.Id
            };

            foreach (NetworkConnection conn in GameNetworkManager.NetworkPlayers.Keys)
            {
                conn.Send(msg);
            }
        }

        /// <summary>
        /// Change unit spawn type to a starter unit. Inform clients
        /// to change the territory's unit spawn type as well.
        /// </summary>
        public void SetStarterUnitSpawnType()
        {
            int protoId = 10;
            UnitSpawnType = UnitDataLookup.Instance.GetUnitData(protoId);

            SetTerritoryUnitSpawnTypeMessage msg = new SetTerritoryUnitSpawnTypeMessage
            {
                TerritoryId = Id,
                UnitSpawnTypeId = protoId
            };

            foreach (NetworkConnection conn in GameNetworkManager.NetworkPlayers.Keys)
            {
                conn.Send(msg);
            }
        }

        /// <summary>
        /// Spawn the HexTerritory's unit type on an unoccupied HexCell if 
        /// any exist.
        /// </summary>
        public void TrySpawnUnit()
        {
            bool spawned = false;
            // Skip the capital cell, so set i to 1.
            int i = 1;
            int numCellsValidButOccupied = 0;
            while (!spawned && i < _memberCells.Count && numCellsValidButOccupied < 6)
            {
                if (UnitSpawnType.IsWalker && _memberCells[i].TerrainType.IsLand ||
                    UnitSpawnType.IsSwimmer && !_memberCells[i].TerrainType.IsLand)
                {
                    if (_memberCells[i].Unit == null)
                    {
                        new Unit(UnitSpawnType, _memberCells[i], OwnerPlayerId, true);
                        spawned = true;
                    }
                    else
                    {
                        numCellsValidButOccupied++;
                    }
                }
                i++;
            }
        }

        /// <summary>
        /// Increment the number of ticks since the last unit spawn
        /// every tick until the number of ticks required to spawn the
        /// selected unit type is met. Spawn the selected unit if enough
        /// ticks have elapsed.
        /// </summary>
        private void OnTick()
        {
            _ticksSinceLastUnitSpawn++;
            _ticksSinceLastHeal++;

            if (_ticksSinceLastUnitSpawn % 10 == 0 && _spawnTickRequestingClients.Count > 0)
            {
                // A second has elapsed, inform clients of new spawn time.
                SetTerritorySpawnTicksElapsedMessage msg = new SetTerritorySpawnTicksElapsedMessage
                {
                    TerritoryId = (ushort)Id,
                    TicksElapsed = (ushort)_ticksSinceLastUnitSpawn
                };

                foreach (NetworkConnection conn in _spawnTickRequestingClients)
                {
                    conn.Send(msg);
                }
            }

            if (_ticksSinceLastUnitSpawn == UnitSpawnType.SpawnTicks)
            {
                TrySpawnUnit();
                _ticksSinceLastUnitSpawn = 0;
            }

            if (_ticksSinceLastHeal == HealTicks)
            {
                // Heal allied units within territory if enough ticks have elapsed.
                HealOwnedUnitsInTerritory();
                _ticksSinceLastHeal = 0;
            }

        }

        /// <summary>
        /// Heal allied units within territory if enough ticks have elapsed.
        /// </summary>
        private void HealOwnedUnitsInTerritory()
        {
            for (int i = 0; i < _memberCells.Count; i++)
            {
                if (_memberCells[i].Unit != null)
                {
                    Unit unit = _memberCells[i].Unit;
                    if (unit.OwnerPlayerId == OwnerPlayerId &&
                        unit.Health.Value < unit.Data.Health)
                    {
                        unit.Health.Heal((int)(0.1f * unit.Data.Health));
                    }
                }
            }
        }

        /// <summary>
        /// Level up the territory and pick a new unit type to spawn.
        /// Inform clients that the HexTerritory has been upgraded.
        /// </summary>
        public void LevelUpTerritory()
        {
            Level++;
            PickNewUnitTypeToSpawn();
            if (_ticksSinceLastUnitSpawn >= _unitSpawnType.SpawnTicks)
            {
                TrySpawnUnit();
                _ticksSinceLastUnitSpawn = 0;
            }

            SetTerritoryLevelMessage msg = new SetTerritoryLevelMessage
            {
                TerritoryId = Id,
                Level = Level
            };

            foreach (NetworkConnection conn in GameNetworkManager.NetworkPlayers.Keys)
            {
                conn.Send(msg);
            }
        }

        /// <summary>
        /// Get a new random unit type of the same level as the HexTerritory.
        /// </summary>
        public void RerollUnitSpawnType()
        {
            PickNewUnitTypeToSpawn();
            if (_ticksSinceLastUnitSpawn >= _unitSpawnType.SpawnTicks)
            {
                TrySpawnUnit();
                _ticksSinceLastUnitSpawn = 0;
            }
        }

        /// <summary>
        /// Start or stop sending a client spawn tick updates.
        /// </summary>
        /// <param name="conn">The NetworkConnection for the client.</param>
        /// <param name="toggle">Denotes whether to start or stop sending the client 
        /// spawn tick updates.</param>
        public void ToggleSendSpawnTickUpdateToClient(NetworkConnection conn, bool toggle)
        {
            if (toggle)
            {
                if (!_spawnTickRequestingClients.Contains(conn))
                {
                    _spawnTickRequestingClients.Add(conn);

                    // Immediately inform the newly listening client of the 
                    // most updated elapsed spawn ticks.
                    // Subtract the ticks value mod 10 (remainder) to get a clean multiple of 10.
                    int spawnTicksElapsed = _ticksSinceLastUnitSpawn - (_ticksSinceLastUnitSpawn % 10);
                    SetTerritorySpawnTicksElapsedMessage msg = new SetTerritorySpawnTicksElapsedMessage
                    {
                        TerritoryId = (ushort)Id,
                        TicksElapsed = (ushort)spawnTicksElapsed
                    };
                    conn.Send(msg);
                }
            }
            else
            {
                _spawnTickRequestingClients.Remove(conn);
            }
        }
    }
}