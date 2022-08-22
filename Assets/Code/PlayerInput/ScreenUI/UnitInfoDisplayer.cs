using UnityEngine;
using Assets.Code.Units;
using Assets.Code.Hex;
using UnityEngine.UI;
using TMPro;
using Mirror;
using Assets.Code.Networking.Messaging;

namespace Assets.Code.PlayerInput.ScreenUI
{
    /// <summary>
    /// A class to update UI to display unit information to the player.
    /// </summary>
    public class UnitInfoDisplayer : MonoBehaviour
    {
        [SerializeField] private HexGrid _hexGrid;

        [SerializeField] private Canvas _unitInfoCanvas;

        [Header("Basic Info")]
        [SerializeField] private TMP_Text _unitNameText;
        [SerializeField] private Image _unitImage;
        [SerializeField] private Image _unitImageOutline;
        [SerializeField] private TMP_Text _unitOwnerColorText;
        [Header("Territory Info")]
        [SerializeField] private Image _territorySpawnBg;
        [SerializeField] private Image _territorySpawnFill;
        [SerializeField] private TMP_Text _territorySpawnsText;
        [SerializeField] private Image[] _territoryLevelStars;
        [SerializeField] private GameObject _upgradeVisuals;
        [SerializeField] private TMP_Text _upgradeText;
        [Header("Health")]
        [SerializeField] private Image _unitHealthBarFill;
        [SerializeField] private TMP_Text _unitHealthBarText;
        [Header("Unit Stats")]
        [SerializeField] private GameObject _attackDamageVisuals;
        [SerializeField] private GameObject _attackSpeedVisuals;
        [SerializeField] private GameObject _attackRangeVisuals;
        [SerializeField] private GameObject _movementSpeedVisuals;
        [SerializeField] private TMP_Text _unitAttackDamageText;
        [SerializeField] private TMP_Text _unitAttackSpeedText;
        [SerializeField] private TMP_Text _unitAttackRangeText;
        [SerializeField] private TMP_Text _unitMoveSpeedText;

        /// <summary>
        /// The last hovered cell.
        /// </summary>
        private HexCell _lastHoveredCell;
        public Unit LastHoveredUnit;

        private void Update()
        {
            if (NetworkClient.active && _hexGrid.HasGenerated)
            {
                HexCell hoveredCell = _hexGrid.GetCellAtPosition(InputUtils.GetMousePoint());
                if (hoveredCell != null && hoveredCell != _lastHoveredCell)
                {
                    if (hoveredCell.IsVisible)
                    {
                        if (LastHoveredUnit != null)
                        {
                            // Clean up last subscriptions and unit reference.
                            LastHoveredUnit.Health.ClientUnitHealthChanged -= UpdateHealthUIForUnitHealthChange;
                            if (LastHoveredUnit.Data.Id == 2)
                            {
                                TerritoryCapitalUnit capitalUnit = (TerritoryCapitalUnit)LastHoveredUnit;
                                HexTerritory territory = capitalUnit.OwnerTerritory;
                                ToggleGetUpdatesForTerritory(territory, false);
                            }
                            LastHoveredUnit = null;
                        }

                        if (hoveredCell.Unit != null)
                        {
                            Unit unit = hoveredCell.Unit;
                            LastHoveredUnit = unit;
                            unit.Health.ClientUnitHealthChanged += UpdateHealthUIForUnitHealthChange;
                            if (unit.Data.Id == 2)
                            {
                                TerritoryCapitalUnit capitalUnit = (TerritoryCapitalUnit)unit;
                                HexTerritory territory = capitalUnit.OwnerTerritory;
                                ToggleGetUpdatesForTerritory(territory, true);
                            }
                            UpdateInfoUI(unit);
                        }
                        else
                        {
                            _unitInfoCanvas.enabled = false;
                        }
                    }
                    else
                    {
                        _unitInfoCanvas.enabled = false;
                    }
                }
                _lastHoveredCell = hoveredCell;
            }
        }

        /// <summary>
        /// Subscribe or unsubscribe to events regarding territory state being updated.
        /// Request or stop requesting spawn tick updates for that territory from the server.
        /// </summary>
        /// <param name="territory">The given territory.</param>
        /// <param name="getUpdates">Denotes whether or not the territory should 
        /// get/listen for updates.</param>
        private void ToggleGetUpdatesForTerritory(HexTerritory territory, bool getUpdates)
        {
            if (getUpdates)
            {
                territory.ClientSpawnTicksChanged += UpdateTerritorySpawnTimerVisuals;
                territory.ClientOwnershipChanged += UpdateForOwnershipChange;
                territory.ClientSpawnTypeChanged += UpdateTerritorySpawnVisuals;
                territory.ClientLevelChanged += UpdateTerritoryLevelVisuals;
            }
            else
            {
                territory.ClientSpawnTicksChanged -= UpdateTerritorySpawnTimerVisuals;
                territory.ClientOwnershipChanged -= UpdateForOwnershipChange;
                territory.ClientSpawnTypeChanged -= UpdateTerritorySpawnVisuals;
                territory.ClientLevelChanged -= UpdateTerritoryLevelVisuals;
            }

            if (territory.OwnerPlayerId != HexTerritory.Unowned)
            {
                // Toggle territory spawn tick updates from the server.
                ToggleTerritorySpawnTicksUpdateMessage msg = new ToggleTerritorySpawnTicksUpdateMessage
                {
                    TerritoryId = territory.Id,
                    SendUpdates = getUpdates
                };

                NetworkClient.Send(msg);
            }
        }

        /// <summary>
        /// Update the unit info UI.
        /// </summary>
        /// <param name="unit">The unit to update the UI for.</param>
        private void UpdateInfoUI(Unit unit)
        {
            UnitData unitData = unit.Data;
            Color ownerPlayerColor = Networking.NetworkPlayer.GetColorForId(unit.OwnerPlayerId);
            _unitOwnerColorText.text = Networking.NetworkPlayer.GetColorNameForId(unit.OwnerPlayerId);
            _unitOwnerColorText.color = ownerPlayerColor;
            _unitNameText.text = unitData.Name;
            _unitNameText.color = ownerPlayerColor;
            _unitImage.sprite = unitData.Sprite;
            _unitImage.color = ownerPlayerColor;
            _unitImageOutline.sprite = unitData.SpriteOutline;
            _unitHealthBarFill.fillAmount = ((float)unit.Health.Value / unit.Data.Health);
            _unitHealthBarText.text = unit.Health.Value.ToString() + "/" + unit.Data.Health.ToString();

            if (unit.Data.Id == 2)
            {
                ToggleUnitStatVisuals(false);
                ToggleTerritoryInfoVisuals(true);

                TerritoryCapitalUnit capitalUnit = (TerritoryCapitalUnit)unit;
                HexTerritory territory = capitalUnit.OwnerTerritory;

                UpdateTerritoryLevelVisuals(territory);
                UpdateTerritorySpawnVisuals(territory.UnitSpawnType);
                _territorySpawnFill.color = ownerPlayerColor;

                if (territory.OwnerPlayerId != HexTerritory.Unowned)
                {
                    UpdateTerritorySpawnTimerVisuals(territory);
                }
                else
                {
                    _territorySpawnsText.text = "Spawns: " + territory.UnitSpawnType.Name;
                    _territorySpawnFill.fillAmount = 0f;
                }
            }
            else
            {
                ToggleTerritoryInfoVisuals(false);
                ToggleUnitStatVisuals(true);
                _unitAttackDamageText.text = unitData.AttackDamage.ToString();
                _unitAttackSpeedText.text = ((float)unitData.AttackTicks / 10).ToString();
                _unitAttackRangeText.text = unitData.AttackRange.ToString();
                _unitMoveSpeedText.text = ((float)unitData.MoveTicks / 10).ToString();
            }
            _unitInfoCanvas.enabled = true;
        }

        private void ToggleUnitStatVisuals(bool enabled)
        {
            _attackDamageVisuals.SetActive(enabled);
            _attackSpeedVisuals.SetActive(enabled);
            _attackRangeVisuals.SetActive(enabled);
            _movementSpeedVisuals.SetActive(enabled);
        } 

        private void ToggleTerritoryInfoVisuals(bool enabled)
        {
            _territorySpawnBg.enabled = enabled;
            _territorySpawnFill.enabled = enabled;
            _territorySpawnsText.enabled = enabled;
            _upgradeVisuals.SetActive(enabled);
            for (int i = 0; i < _territoryLevelStars.Length; i++)
            {
                _territoryLevelStars[i].enabled = enabled;
            }
        }

        /// <summary>
        /// Update the health bar fill amount and text when a unit's health has changed.
        /// </summary>
        /// <param name="unit">The unit who's health changed.</param>
        private void UpdateHealthUIForUnitHealthChange(Unit unit)
        {
            _unitHealthBarFill.fillAmount = ((float)unit.Health.Value / unit.Data.Health);
            _unitHealthBarText.text = unit.Health.Value.ToString() + "/" + unit.Data.Health.ToString();
        }

        /// <summary>
        /// Update the UI when a capital unit's ownership has changed.
        /// Also, if the the local client is the owner, request unit spawn
        /// tick updates for the territory.
        /// </summary>
        /// <param name="id">The hovered territory who's ownership changed..</param>
        private void UpdateForOwnershipChange(HexTerritory territory)
        {
            Color newColor = Networking.NetworkPlayer.GetColorForId(territory.OwnerPlayerId);
            _unitNameText.color = newColor;
            _unitImage.color = newColor;
            _unitOwnerColorText.text = Networking.NetworkPlayer.GetColorNameForId(territory.OwnerPlayerId);
            _unitOwnerColorText.color = newColor;
            _territorySpawnFill.color = newColor;

            if (territory.OwnerPlayerId == Networking.NetworkPlayer.AuthorityInstance.PlayerId)
            {
                ToggleTerritorySpawnTicksUpdateMessage msg = new ToggleTerritorySpawnTicksUpdateMessage
                {
                    TerritoryId = territory.Id,
                    SendUpdates = true
                };

                NetworkClient.Send(msg);
            }
        }

        /// <summary>
        /// Update the spawn unit time text and progress fill when hovering over 
        /// a HexTerritory capital.
        /// </summary>
        /// <param name="territorySpawnTicksElapsed">The number of spawn ticks elapsed.</param>
        private void UpdateTerritorySpawnTimerVisuals(HexTerritory territory)
        {
            int spawnRawSeconds;
            if (territory.UnitSpawnType.SpawnTicks != territory.ClientTicksSinceLastSpawn)
            {
                _territorySpawnFill.fillAmount = (float)territory.ClientTicksSinceLastSpawn /
                    territory.UnitSpawnType.SpawnTicks;

                spawnRawSeconds = (territory.UnitSpawnType.SpawnTicks -
                    territory.ClientTicksSinceLastSpawn) / 10;
            }
            else
            {
                // Unit just spawned, reset.
                _territorySpawnFill.fillAmount = 0f;

                spawnRawSeconds = territory.UnitSpawnType.SpawnTicks / 10;
            }

            int spawnMinutes = spawnRawSeconds / 60;
            int spawnMinuteSeconds = spawnRawSeconds % 60;

            _territorySpawnsText.text = "Spawning " + territory.UnitSpawnType.Name + " in " +
                spawnMinutes + ":" + (spawnMinuteSeconds < 10 ? "0" +
                spawnMinuteSeconds : spawnMinuteSeconds.ToString());
        }

        /// <summary>
        /// Update the territory spawn visuals.
        /// </summary>
        /// <param name="newSpawnData">The unit type to spawn's data.</param>
        private void UpdateTerritorySpawnVisuals(UnitData unitData)
        {
            _territorySpawnBg.sprite = unitData.SpriteOutline;
            _territorySpawnFill.sprite = unitData.TightMeshSprite;
        }

        /// <summary>
        /// Update the territory level related visuals.
        /// </summary>
        /// <param name="level">The territory to update for.</param>
        private void UpdateTerritoryLevelVisuals(HexTerritory territory)
        {
            for (int i = 0; i < _territoryLevelStars.Length; i++)
            {
                if (i < territory.Level)
                {
                    _territoryLevelStars[i].enabled = true;
                }
                else
                {
                    _territoryLevelStars[i].enabled = false;
                }
            }

            if (territory.Level == HexTerritory.MaxLevel)
            {
                _upgradeText.text = "Max Level";
            }
            else
            {
                _upgradeText.text = "Press e to upgrade (" + territory.UpgradeCost + ")";
            }
        }

    }
}
