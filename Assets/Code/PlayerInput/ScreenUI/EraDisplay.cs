using Assets.Code.GameTime;
using UnityEngine;
using TMPro;
using DG.Tweening;

namespace Assets.Code.PlayerInput.ScreenUI
{
    /// <summary>
    ///  A class to manage displaying information about the current
    ///  game era.
    /// </summary>
    public class EraDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text _eraTimeText;
        [SerializeField] private TMP_Text _eraText;
        [SerializeField] private RectTransform _newEraNotification;
        [SerializeField] private TMP_Text _notificationSubtext;

        private void Start()
        {
            EraManager.ClientReceivedEraTicks += UpdateEraTimer;
            EraManager.ClientUpdatedEra += UpdateEraText;
            EraManager.ClientUpdatedEra += ShowNewEraNotification;
            // to remove
            Hex.HexWorld.ClientGameEnded += delegate (int winningPlayerId, int numTerritories)
            {
                _eraText.color = Networking.NetworkPlayer.GetColorForId(winningPlayerId);
                _eraText.text = Networking.NetworkPlayer.GetColorNameForId(winningPlayerId);
                _eraTimeText.text = " won with " + numTerritories.ToString();
            };
        }

        private void OnDestroy()
        {
            EraManager.ClientReceivedEraTicks -= UpdateEraTimer;
            EraManager.ClientUpdatedEra -= UpdateEraText;
            EraManager.ClientUpdatedEra -= ShowNewEraNotification;
        }

        public void UpdateEraTimer(int eraTicksElapsed)
        {
            int rawSecondsLeftInEra = 
                (EraManager.Instance.CurrentTickTimeToAdvance - eraTicksElapsed) / 10;
            int displayEraIndex = EraManager.Instance.CurrentEraIndex;
            if (rawSecondsLeftInEra == 0)
            {
                // Just completed an era, reset time to next era's full duration.
                displayEraIndex = Mathf.Clamp(EraManager.Instance.CurrentEraIndex + 1,
                    0, EraManager.NumEras - 1);
                rawSecondsLeftInEra = TickSystem.TicksPerMinute * 
                    EraManager.EraTimesToAdvance[displayEraIndex] / 10;
            }
            int displayMinutes = rawSecondsLeftInEra / 60;
            int displaySeconds = rawSecondsLeftInEra % 60;

            string prefix = displayEraIndex < EraManager.NumEras - 1 ?
                "Next era in: " : "Game ends in: ";
            _eraTimeText.text = prefix + displayMinutes + 
                ":" + (displaySeconds < 10 ? "0" +
                displaySeconds : displaySeconds.ToString());
        }

        public void UpdateEraText(int eraIndex, string eraName, bool firstStateUpdate)
        {
            _eraText.text = "Era " + (eraIndex + 1) + ": " + eraName;
        }

        public void ShowNewEraNotification(int eraIndex, string eraName, bool firstStateUpdate)
        {
            if (!firstStateUpdate)
            {
                if (eraIndex < EraManager.NumEras - 1)
                {
                    _notificationSubtext.text = 
                        "Territory upgrades and new units available";
                }
                else
                {
                    _notificationSubtext.text = 
                        "The beginning of the end";
                }

                _newEraNotification.gameObject.SetActive(true);
                DOTween.Sequence()
                    .Append(_newEraNotification.DOLocalMoveY(520f, 0.5f))
                    .AppendInterval(5f)
                    .Append(_newEraNotification.DOLocalMoveY(650f, 0.5f))
                    .AppendCallback(delegate () { _newEraNotification.gameObject.SetActive(false); });
            }
        }
    }
}