using UnityEngine;
using Assets.Code.Hex;
using System.Collections.Generic;
using Assets.Code.Units;

namespace Assets.Code.PlayerInput
{
    /// <summary>
    /// A component to create a visual representation of the path
    /// a unit could undertake / is following.
    /// </summary>
    public class UnitPathVisualizer : MonoBehaviour
    {
        /// <summary>
        /// The LineRenderer component to render unit paths.
        /// </summary>
        [SerializeField] private LineRenderer _lineRenderer;

        /// <summary>
        /// A SpriteRenderer to serve as the visualizer for a copy
        /// of the currently selected unit's sprite in a holographic-like
        /// form. Placed at the end of the path.
        /// </summary>
        [SerializeField] private SpriteRenderer _endPathUnitSpriteRenderer;

        /// <summary>
        /// A SpriteRenderer placed at the end of the path to show the
        /// desired travel destination when multiple units are selected.
        /// </summary>
        [SerializeField] private SpriteRenderer _endPathDestinationSpriteRenderer;

        /// <summary>
        /// An icon to place on the hovered cell to indicate the selected
        /// unit will attack the unit at the hovered cell once it has
        /// traveled the visualized path.
        /// </summary>
        [SerializeField] private SpriteRenderer _attackIndicator;

        /// <summary>
        /// The HexGrid to get HexCells from.
        /// </summary>
        [SerializeField] private HexGrid _hexGrid;

        /// <summary>
        /// The class holding the currently selected unit.
        /// </summary>
        [SerializeField] private UnitSelector _unitSelector;

        /// <summary>
        /// The last hovered cell. Only render a new path if the cell
        /// hovered over on a new frame is different than this one.
        /// </summary>
        private HexCell _lastHoveredCell;

        /// <summary>
        /// A y offset to be applied to the final drawn line visualization.
        /// </summary>
        private static readonly Vector3 YOffset = new Vector3(0f, 0.1f, 0f);

        /// <summary>
        /// The amount of time to elapse to update the path visualization.
        /// </summary>
        private const float VisualizationUpdateTime = 0.2f;

        /// <summary>
        /// The amount of time elapsed since the last path visualization update.
        /// </summary>
        private float _timeSinceLastVisualizationUpdate;


        /// <summary>
        /// Subscribe to relevant events from the UnitSelector.
        /// </summary>
        private void Awake()
        {
            _unitSelector.UnitSelected += UpdateForNewUnitSelected;
            _unitSelector.UnitDeselected += HandleUnitDeselection;
        }

        /// <summary>
        /// Unsubscribe to relevant events from the UnitSelector.
        /// </summary>
        private void OnDestroy()
        {
            _unitSelector.UnitSelected -= UpdateForNewUnitSelected;
            _unitSelector.UnitDeselected -= HandleUnitDeselection;
        }

        /// <summary>
        /// If a unit is selected and the player is hovering the mouse over
        /// a new cell, render the projected path the unit will take. Hide path 
        /// visual if target/hovered cell is invalid to move onto. If multiple
        /// units are selected, only show a destination marker on the intended
        /// destination cell. This is all run in LateUpdate to reflect the current
        /// state of the UnitSelector which runs in Update.
        /// </summary>
        private void LateUpdate()
        {
            if (_unitSelector.SelectedUnits.Count > 0 && Mirror.NetworkClient.active &&
                _hexGrid.HasGenerated && !Input.GetMouseButton(0))
            {
                HexCell hoveringCell = _hexGrid.GetCellAtPosition(InputUtils.GetMousePoint());
                if (hoveringCell != null)
                {
                    if (hoveringCell != _lastHoveredCell)
                    {
                        UpdateVisualization(hoveringCell);
                    }
                    else if (_timeSinceLastVisualizationUpdate >= VisualizationUpdateTime)
                    {
                        // Periodically update the path visualization if enough time has elapsed.
                        _timeSinceLastVisualizationUpdate = 0f;
                        UpdateVisualization(hoveringCell);
                    }
                }
                _timeSinceLastVisualizationUpdate += Time.deltaTime;
            }

            if (Input.GetMouseButtonDown(0))
            {
                // Hide when first left-clicked.
                HidePathVisual();
                UpdateDestinationIndicator(null, false);
                UpdateAttackIndicator(null, false);
            }
        }

        /// <summary>
        /// Update the path visualization.
        /// </summary>
        /// <param name="hoveringCell">The currently hovered cell.</param>
        private void UpdateVisualization(HexCell hoveringCell)
        {
            _lastHoveredCell = hoveringCell;
            if ((hoveringCell.Unit == null && hoveringCell.IsVisible) || !hoveringCell.IsVisible)
            {
                UpdatePathForNonOccupiedCell(hoveringCell);
            }
            else if (hoveringCell.Unit != null)
            {
                if (hoveringCell.Unit.OwnerPlayerId ==
                    Networking.NetworkPlayer.AuthorityInstance.PlayerId)
                {
                    // Hovering cell's unit is friendly unit. Hide path visual.
                    HidePathVisual();
                    UpdateAttackIndicator(hoveringCell, false);
                }
                else if (hoveringCell.IsVisible)
                {
                    // Hovering cell's unit is an enemy unit.
                    UpdateAttackIndicator(hoveringCell, true);
                    UpdateDestinationIndicator(hoveringCell, false);

                    if (_unitSelector.SelectedUnits.Count == 1)
                    {
                        // Only one unit selected, try to visual the path to attack the hovered enemy
                        // unit.
                        if (_unitSelector.SelectedUnits[0].OccupiedCell.Coordinates.DistanceTo(
                             hoveringCell.Coordinates) > _unitSelector.SelectedUnits[0].Data.AttackRange)
                        {
                            // Get closest cell in range to target.
                            HexCell attackFromCell = HexPathfinder.Instance.
                                GetClosestCellInRangeToTarget(_unitSelector.SelectedUnits[0].OccupiedCell,
                                hoveringCell, _unitSelector.SelectedUnits[0].Data.AttackRange);

                            if (attackFromCell != null)
                            {
                                VisualizePath(_unitSelector.SelectedUnits[0].OccupiedCell, attackFromCell);
                            }
                            else
                            {
                                // No attack from cell, hide visuals.
                                HidePathVisual();
                                UpdateAttackIndicator(null, false);
                            }
                        }
                        else
                        {
                            // The unit is already the minimum possible distance to the target. Hide
                            // the path visualization.
                            HidePathVisual();
                        }
                    }
                    else
                    {
                        _lineRenderer.positionCount = 0;
                        _endPathUnitSpriteRenderer.enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// Update the path visualization for a non-occupied cell.
        /// </summary>
        /// <param name="hoveringCell">The hovering cell.</param>
        private void UpdatePathForNonOccupiedCell(HexCell hoveringCell)
        {
            if (_unitSelector.SelectedUnits.Count == 1)
            {
                if (hoveringCell.CanUnitTravelOn(_unitSelector.SelectedUnits[0]))
                {
                    // Draw the predicted path for unit movement.
                    VisualizePath(_unitSelector.SelectedUnits[0].OccupiedCell, hoveringCell);
                }
                else
                {
                    // Cannot travel on terrain type, hide path visuals.
                    HidePathVisual();
                }
                UpdateDestinationIndicator(hoveringCell, false);
            }
            else
            {
                // Multiple units selected, simply show the intended end destination.
                UpdateDestinationIndicator(hoveringCell, true);
                _lineRenderer.positionCount = 0;
                _endPathUnitSpriteRenderer.enabled = false;
            }
            UpdateAttackIndicator(hoveringCell, false);
        }

        /// <summary>
        /// Visualize a path given a starting cell and end cell.
        /// If no path was found, nothing shall be rendered.
        /// </summary>
        /// <param name="fromCell">The starting cell.</param>
        /// <param name="toCell">The ending cell.</param>
        /// is to get a unit in range to attack.</param>
        private void VisualizePath(HexCell fromCell, HexCell toCell)
        {
            if (fromCell != toCell)
            {
                List<HexCell> path = HexPathfinder.Instance.FindPath(fromCell, toCell);
                if (path != null)
                {
                    _lineRenderer.positionCount = path.Count + 1;
                    _lineRenderer.SetPosition(0, fromCell.VisualPosition + YOffset);
                    for (int i = 0; i < path.Count; i++)
                    {
                        _lineRenderer.SetPosition(i + 1, path[i].VisualPosition + YOffset);
                    }

                    _endPathUnitSpriteRenderer.color = Networking.NetworkPlayer.
                        GetColorForId(_unitSelector.SelectedUnits[0].OwnerPlayerId);
                    _endPathUnitSpriteRenderer.transform.position = path[path.Count - 1].VisualPosition;
                    _endPathUnitSpriteRenderer.enabled = true;
                }
                else
                {
                    // No valid path found, hide visuals.
                    HidePathVisual();
                    UpdateAttackIndicator(null, false);
                }
            }
        }

        /// <summary>
        /// Reset the line renderer and hide the end path sprites.
        /// </summary>
        private void HidePathVisual()
        {
            _lastHoveredCell = null;
            _lineRenderer.positionCount = 0;
            _endPathUnitSpriteRenderer.enabled = false;
            UpdateDestinationIndicator(null, false);
        }

        /// <summary>
        /// Since the selected unit moved, redraw the path.
        /// </summary>
        /// <param name="unit">An unecessary parameter, as the Mobile.ClientUnitMoved event
        /// returns a unit.</param>
        private void UpdatePathOnUnitMove(Unit unit)
        {
            // Set last hovered cell to null to force a path visual update.
            _lastHoveredCell = null;
            LateUpdate();
        }

        /// <summary>
        /// Subscribe to the ClientUnitMoved event of the selected unit's mobile component
        /// in order to update the drawn path upon the unit moving. Also, update the end path 
        /// sprite renderer's sprite.
        /// </summary>
        private void UpdateForNewUnitSelected()
        {
            Unit selectedUnit = _unitSelector.SelectedUnits[0];
            selectedUnit.Mobile.ClientUnitMoved += UpdatePathOnUnitMove;
            selectedUnit.Health.ClientDying += CleanupOnUnitDestroy;
            _endPathUnitSpriteRenderer.sprite = _unitSelector.SelectedUnits[0].Data.Sprite;
        }

        /// <summary>
        /// Unsubscribe from the selected unit's ClientUnitMoved event and hide the path
        /// and end path attack indicator if necessary.
        /// </summary>
        private void HandleUnitDeselection()
        {
            Unit deselectedUnit = _unitSelector.SelectedUnits[0];
            deselectedUnit.Mobile.ClientUnitMoved -= UpdatePathOnUnitMove;
            deselectedUnit.Health.ClientDying -= CleanupOnUnitDestroy;
            HidePathVisual();
            UpdateAttackIndicator(null, false);
        }

        /// <summary>
        /// Update the attack indicator based on the unit, if any, at the currently
        /// hovered cell.
        /// </summary>
        /// <param name="hoveringCell">The cell currently being hovered over.</param>
        /// <param name="active">The state to set the attack indicator to.</param>
        private void UpdateAttackIndicator(HexCell hoveringCell, bool active)
        {
            _attackIndicator.enabled = active;
            if (active)
            {
                _attackIndicator.transform.position = hoveringCell.VisualPosition + YOffset;
            }
        }

        /// <summary>
        /// Update the end path destination indicator.
        /// </summary>
        /// <param name="hoveringCell">The cell being hovered over.</param>
        /// <param name="active">The state to set the end path destination indicator to.</param>
        private void UpdateDestinationIndicator(HexCell hoveringCell, bool active)
        {
            _endPathDestinationSpriteRenderer.enabled = active;
            if (active)
            {
                _endPathDestinationSpriteRenderer.color = Networking.NetworkPlayer.
                    GetColorForId(_unitSelector.SelectedUnits[0].OwnerPlayerId);
                _endPathDestinationSpriteRenderer.transform.position = hoveringCell.VisualPosition;
            }
        }

        /// <summary>
        /// Hide the path visualization and attack indicator if the selected unit dies.
        /// </summary>
        private void CleanupOnUnitDestroy()
        {
            HidePathVisual();
            UpdateAttackIndicator(null, false);
        }
    }
}