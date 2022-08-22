using UnityEngine;
using Assets.Code.Hex;
using Assets.Code.Units;
using System;
using System.Collections.Generic;

namespace Assets.Code.PlayerInput
{
    /// <summary>
    /// A GameObject component to allow players to select units.
    /// </summary>
    public class UnitSelector : MonoBehaviour
    {
        [Tooltip("The maximum number of units that can be selected at once.")]
        [SerializeField] private int _maxUnitSelectionCount;

        [Tooltip("The HexGrid to select on.")]
        [SerializeField] private HexGrid _hexGrid;

        [Tooltip("The selection indicator visual prefab.")]
        [SerializeField] private SpriteRenderer _selectionIndicatorPrefab;

        [Tooltip("A transform to be the parent to all selection indicators for " +
            "a cleaner hierarchy.")]
        [SerializeField] private Transform _selectionIndicatorInstanceParent;

        /// <summary>
        /// A list of selection indicator instances for selection visualization.
        /// </summary>
        private List<SpriteRenderer> _selectionIndicators;

        /// <summary>
        /// The currently selected units. Count == 0 if no units selected.
        /// </summary>
        public List<Unit> SelectedUnits { get; private set; }

        /// <summary>
        /// A queue containing available selection indices, indexes that
        /// correspond with selection indicators.
        /// </summary>
        public Queue<int> _availableSelectionIndices;

        /// <summary>
        /// Denotes if selection functionality is paused or not.
        /// </summary>
        private bool _pauseSelection;

        /// <summary>
        /// The selection box rect used for drag/box selection
        /// </summary>
        [SerializeField] private RectTransform _selectionBox;

        /// <summary>
        /// The mouse position at the start of a selection action.
        /// </summary>
        private Vector2 startPosition;

        /// <summary>
        /// Denotes whether or not a drag select was performed this frame.
        /// </summary>
        public bool JustDragSelected { get; private set; }

        /// <summary>
        /// An event to notify subscribers that a unit was selected.
        /// </summary>
        public event Action UnitSelected;

        /// <summary>
        /// An event to notify subscribers that a unit was deselected.
        /// Note: When invoked, this class will still hold a reference
        /// to the selected unit.
        /// </summary>
        public event Action UnitDeselected;

        /// <summary>
        /// Create the necessary data structures and cache selection indicators.
        /// </summary>
        private void Awake()
        {
            SelectedUnits = new List<Unit>();
            _selectionIndicators = new List<SpriteRenderer>();
            CreateSelectionIndicators();
            _availableSelectionIndices = new Queue<int>();
            for (int i = 0; i < _maxUnitSelectionCount; i++)
            {
                _availableSelectionIndices.Enqueue(i);
            }
        }

        /// <summary>
        /// Create and cache selection indicator visual prefabs for unit selection
        /// visualization.
        /// </summary>
        private void CreateSelectionIndicators()
        {
            for (int i = 0; i < _maxUnitSelectionCount; i++)
            {
                SpriteRenderer selectionIndicator = Instantiate(_selectionIndicatorPrefab,
                    _selectionIndicatorInstanceParent);
                selectionIndicator.enabled = false;
                _selectionIndicators.Add(selectionIndicator);
            }
        }

        /// <summary>
        /// If the player left-clicks, try to select a unit.
        /// If the player right-clicks, deselect the currently selected units.
        /// </summary>
        private void Update()
        {
            if (!_pauseSelection)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    // Record starting mouse positon in case of drag/box select.
                    startPosition = Input.mousePosition;
                    JustDragSelected = false;
                }

                if (Input.GetMouseButton(0))
                {
                    // Redraw the selection box.
                    UpdateSelectionBox(Input.mousePosition);
                }

                if (Input.GetMouseButtonUp(0))
                {
                    if (Vector3.Distance(startPosition, Input.mousePosition) < 20)
                    {
                        // Player tried to single select.
                        TrySelectUnit(_hexGrid.GetCellAtPosition(InputUtils.GetMousePoint()));
                    }
                    else
                    {
                        // Player tried to box select.
                        TryBoxSelect();
                        JustDragSelected = true;
                    }
                    _selectionBox.gameObject.SetActive(false);
                }

                if (Input.GetMouseButtonDown(1) && SelectedUnits.Count > 0)
                {
                    DeselectUnits();
                }
            } 
            else
            {
                _pauseSelection = false;
            }
        }

        /// <summary>
        /// Update the box select visual.
        /// </summary>
        /// <param name="mousePosition">The current mouse position.</param>
        private void UpdateSelectionBox(Vector2 mousePosition)
        {
            if (!_selectionBox.gameObject.activeInHierarchy)
            {
                _selectionBox.gameObject.SetActive(true);
            }

            float width = mousePosition.x - startPosition.x;
            float height = mousePosition.y - startPosition.y;

            _selectionBox.sizeDelta = 
                new Vector2(Mathf.Abs(width), Mathf.Abs(height));
            _selectionBox.anchoredPosition = 
                startPosition + new Vector2(width / 2, height / 2);
        }

        /// <summary>
        /// Try to box select within the bounds of the drawn box.
        /// </summary>
        private void TryBoxSelect()
        {
            Vector2 mousePosition = Input.mousePosition;
            float width = mousePosition.x - startPosition.x;
            float height = mousePosition.y - startPosition.y;
            Vector2 center = startPosition + new Vector2(width / 2, height / 2);
            Vector3 centerPosWorld = Camera.main.ScreenToWorldPoint(
                new Vector3(center.x, center.y, Camera.main.farClipPlane));
            HexCell centerCell = _hexGrid.GetCellAtPosition(centerPosWorld);

            if (centerCell != null)
            {
                Vector2 screenMin = _selectionBox.anchoredPosition - _selectionBox.sizeDelta / 2;
                Vector2 screenMax = _selectionBox.anchoredPosition + _selectionBox.sizeDelta / 2;

                List<HexCell> cellsInBounds =
                    HexPathfinder.Instance.GetCellsInScreenBounds(centerCell, screenMin, screenMax);

                for (int i = 0; i < cellsInBounds.Count; i++)
                {
                    TrySelectUnit(cellsInBounds[i]);
                }
            }
        }

        /// <summary>
        /// Try to select a unit based on the clicked position.
        /// </summary>
        /// <param name="clickPosition">The clicked position in world space.</param>
        private void TrySelectUnit(HexCell clickedCell)
        {
            if (clickedCell != null && clickedCell.IsVisible)
            {
                Unit unit = clickedCell.Unit;
                if (unit != null &&
                    unit.Data.IsSelectable &&
                    unit.OwnerPlayerId ==
                    Networking.NetworkPlayer.AuthorityInstance.PlayerId &&
                    !unit.IsSelected)
                {
                    SelectedUnits.Add(clickedCell.Unit);
                    unit.IsSelected = true;
                    unit.SelectionIndex = GetAvailableSelectionIndex();
                    unit.Mobile.ClientUnitMoved += UpdateSelectionIndicatorForUnit;
                    unit.Health.ClientUnitDying += DeselectUnitOnDestroy;
                    UpdateSelectionIndicatorForUnit(clickedCell.Unit);
                    UnitSelected?.Invoke();
                }
            }
        }

        /// <summary>
        /// Get, enable, and update a selection indicator for a unit.
        /// </summary>
        /// <param name="unit">The unit to update a selection indicator for.</param>
        private void UpdateSelectionIndicatorForUnit(Unit unit)
        {
            SpriteRenderer selectionIndicator = _selectionIndicators[unit.SelectionIndex];
            selectionIndicator.transform.position = unit.OccupiedCell.VisualPosition;
            selectionIndicator.color = Networking.NetworkPlayer.GetColorForId(unit.OwnerPlayerId);
            selectionIndicator.enabled = true;
        }

        /// <summary>
        /// Deselect the currently selected units.
        /// </summary>
        public void DeselectUnits()
        {
            UnitDeselected?.Invoke();
            for (int i = SelectedUnits.Count - 1; i >= 0; i--)
            {
                Unit unit = SelectedUnits[i];
                DeselectUnit(unit);
            }
        }

        /// <summary>
        /// Deselect a single unit.
        /// </summary>
        /// <param name="unit">The unit to deselect.</param>
        private void DeselectUnit(Unit unit)
        {
            unit.IsSelected = false;
            unit.Mobile.ClientUnitMoved -= UpdateSelectionIndicatorForUnit;
            unit.Health.ClientUnitDying -= DeselectUnitOnDestroy;
            SelectedUnits.Remove(unit);
            _selectionIndicators[unit.SelectionIndex].enabled = false;
            _availableSelectionIndices.Enqueue(unit.SelectionIndex);
        }

        /// <summary>
        /// Get the first available selection index.
        /// </summary>
        /// <returns>The first available selection index.</returns>
        private int GetAvailableSelectionIndex()
        {
            return _availableSelectionIndices.Dequeue();
        }

        /// <summary>
        /// Get an array of all the HexCell indices for the selected units.
        /// </summary>
        /// <returns>An array of all the HexCell indices for the selected units.</returns>
        public int[] GetUnitCellIndicesArray()
        {
            int[] unitCellIndices = new int[SelectedUnits.Count];
            for (int i = 0; i < SelectedUnits.Count; i++)
            {
                unitCellIndices[i] = SelectedUnits[i].OccupiedCell.Index;
            }
            return unitCellIndices;
        }

        /// <summary>
        /// Pause selection until the left mouse button has been lifted so as 
        /// to not select a unit that was just deselected which has moved onto 
        /// the mouse hovered cell.
        /// </summary>
        public void PauseSelectionUntilMouseUp()
        {
            _pauseSelection = true;
        }

        /// <summary>
        /// Deselect the unit when it is destroyed.
        /// </summary>
        /// <param name="unit">The unit being destroyed.</param>
        private void DeselectUnitOnDestroy(Unit unit)
        {
            DeselectUnit(unit);
            if (SelectedUnits.Count > 0)
            {
                UnitSelected?.Invoke();
            }
        }
    }
}