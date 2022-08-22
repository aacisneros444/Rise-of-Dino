using UnityEngine;
using Assets.Code.PlayerInput.ScreenUI;
using Assets.Code.Units;
using Assets.Code.Hex;
using Assets.Code.Networking.Messaging;
using Mirror;

namespace Assets.Code.PlayerInput
{
    /// <summary>
    /// A class to handle upgrading owned HexTerritories upon
    /// player request.
    /// </summary>
    public class TerritoryUpgrader : MonoBehaviour
    {
        [SerializeField] UnitInfoDisplayer _unitInfoDisplayer;

        private void Update()
        {
            if (_unitInfoDisplayer.LastHoveredUnit != null &&
                _unitInfoDisplayer.LastHoveredUnit.Data.Id == 2 &&
                _unitInfoDisplayer.LastHoveredUnit.OwnerPlayerId == 
                Networking.NetworkPlayer.AuthorityInstance.PlayerId)
            {
                TerritoryCapitalUnit capitalUnit = (TerritoryCapitalUnit)_unitInfoDisplayer.LastHoveredUnit;
                HexTerritory territory = capitalUnit.OwnerTerritory;

                if (Input.GetKeyDown(KeyCode.E) && territory.Level < HexTerritory.MaxLevel &&
                    Networking.NetworkPlayer.AuthorityInstance.UpgradePoints >= territory.UpgradeCost)
                {
                    // Upgrade request.
                    RequestTerritoryUpgradeMessage msg = new RequestTerritoryUpgradeMessage
                    {
                        TerritoryId = territory.Id
                    };

                    NetworkClient.Send(msg);
                }

                if (Input.GetKeyDown(KeyCode.R) &&
                    Networking.NetworkPlayer.AuthorityInstance.UpgradePoints >= HexTerritory.RerollCost)
                {
                    // Reroll request.
                    TerritorySpawnTypeRerollMessage msg = new TerritorySpawnTypeRerollMessage
                    {
                        TerritoryId = territory.Id
                    };

                    NetworkClient.Send(msg);
                }
            }
        }
    }
}