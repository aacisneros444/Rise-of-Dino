using UnityEngine;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;
using Assets.Code.Units;

/// <summary>
/// A class to manage health popups when a unit's health changes.
/// </summary>
public class HealthTextPopupManager : MonoBehaviour
{
    [Tooltip("The number of health text popups to pool.")]
    [SerializeField] private int _healthTextPopupPoolCount = 32;
    [SerializeField] private TMP_Text  _healthTextPopupPrefab;
    [SerializeField] private Color _damageColor;
    [SerializeField] private Color _healColor;

    /// <summary>
    /// A pool of health text popups.
    /// </summary>
    private Queue<TMP_Text> _healthTextPopupPool;
    private Queue<TMP_Text> _lastUsedHealthTextPopups;

    /// <summary>
    /// Populate the health text popup pool and create the necessary data structures.
    /// </summary>
    private void Awake()
    {
        _healthTextPopupPool = new Queue<TMP_Text>();
        _lastUsedHealthTextPopups = new Queue<TMP_Text>();
        for (int i = 0; i < _healthTextPopupPoolCount; i++)
        {
            TMP_Text healthText = Instantiate(_healthTextPopupPrefab, transform);
            healthText.gameObject.SetActive(false);
            _healthTextPopupPool.Enqueue(healthText);
        }
        Health.ClientUnitDamaged += CreateDamageText;
        Health.ClientUnitHealed += CreateHealText;
    }

    /// <summary>
    /// Unsubscribe from the Health class events.
    /// </summary>
    private void OnDestroy()
    {
        Health.ClientUnitDamaged -= CreateDamageText;
        Health.ClientUnitHealed -= CreateHealText;
    }

    /// <summary>
    /// Create a damage text popup.
    /// </summary>
    /// <param name="amount">The amount of damage taken.</param>
    /// <param name="position">The position to create the popup.</param>
    public void CreateDamageText(int amount, Vector3 position)
    {
        CreateHealthText(amount, position, _damageColor);
    }

    /// <summary>
    /// Create a health text popup.
    /// </summary>
    /// <param name="amount">The amount of health healed.</param>
    /// <param name="position">The position to create the popup.</param>
    public void CreateHealText(int amount, Vector3 position)
    {
        CreateHealthText(amount, position, _healColor);
    }

    /// <summary>
    /// Create an animated health text popup.
    /// </summary>
    /// <param name="amountChanged">The amount of health changed.</param>
    /// <param name="position">The position for the health text popup.</param>
    /// <param name="textColor">The color to use for the text.</param>
    public void CreateHealthText(int amountChanged, Vector3 position, Color textColor)
    {
        TMP_Text healthText = _healthTextPopupPool.Dequeue();
        healthText.text = amountChanged.ToString();
        healthText.color = textColor;
        Vector3 randomPositionOffset = new Vector3(Random.Range(-0.25f, 0.25f),
            0f, Random.Range(-0.25f, 0.25f));
        Vector3 popupPosition = position + randomPositionOffset;
        healthText.rectTransform.position = popupPosition;
        healthText.gameObject.SetActive(true);
        _lastUsedHealthTextPopups.Enqueue(healthText);
        _healthTextPopupPool.Enqueue(healthText);

        DOTween.ToAlpha(() => healthText.color, x => healthText.color = x, 0, 1);
        DOTween.Sequence().Append(healthText.transform.DOLocalMove(popupPosition + Vector3.forward, 1f)).
            AppendCallback(DisableOnAnimationComplete);
    }

    /// <summary>
    /// Disable the last used health text once its animation is completed.
    /// </summary>
    private void DisableOnAnimationComplete()
    {
        _lastUsedHealthTextPopups.Dequeue().gameObject.SetActive(false);
    }
}
