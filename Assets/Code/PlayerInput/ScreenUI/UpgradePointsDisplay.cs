using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// A class to update the display which shows
/// the number of upgrade points the player has.
/// </summary>
public class UpgradePointsDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text _upgradePointsText;
    [SerializeField] private TMP_Text _upgradePointsPopup;
    [SerializeField] private RectTransform _popupParent;

    /// <summary>
    /// Subscribe to the ClientUpgradePointsUpdated event in 
    /// NetworkPlayer to display the updated amount of upgrade
    /// points the player has.
    /// </summary>
    private void Awake()
    {
        Assets.Code.Networking.NetworkPlayer.
            ClientUpgradePointsUpdated += delegate (int oldValue, int upgradePoints)
        {
            _upgradePointsText.text = upgradePoints.ToString();
            AnimateUpdate(oldValue, upgradePoints);
        };
    }

    /// <summary>
    /// Animate the upgrade points update with a text popup.
    /// </summary>
    private void AnimateUpdate(int oldValue, int newValue)
    {
        TMP_Text pointsPopup = Instantiate(_upgradePointsPopup, _popupParent);

        char prefix = newValue > oldValue ? '+' : '-';
        pointsPopup.text = prefix +  Mathf.Abs((newValue - oldValue)).ToString();

        DOTween.ToAlpha(() => pointsPopup.color, x => pointsPopup.color = x, 0, 0.95f);
        DOTween.Sequence().Append(pointsPopup.rectTransform.DOLocalMoveY(50, 1f)).
            AppendCallback(delegate () { Destroy(pointsPopup.gameObject); });
    }
}
