using UnityEngine;
using Assets.Code.Hex;
using Mirror;
using Assets.Code.Networking;
using Assets.Code.Networking.Messaging;
using System;
using DG.Tweening;
using System.Collections.Generic;

namespace Assets.Code.Units
{
    /// <summary>
    /// A class to model a unit, an entity which the player
    /// can control.
    /// </summary>
    public class Unit
    {
        /// <summary>
        /// The scriptable object containing data about
        /// this unit.
        /// </summary>
        public UnitData Data { get; private set; }

        /// <summary>
        /// The backing field for OccupiedCell. The cell
        /// currently occupied by this unit.
        /// </summary>
        private HexCell _occupiedCell;

        /// <summary>
        /// The cell currently occupied by this unit.
        /// </summary>
        public HexCell OccupiedCell
        {
            get { return _occupiedCell; }
            set
            {
                if (_occupiedCell != null)
                {
                    _occupiedCell.Unit = null;
                }

                if (NetworkClient.active)
                {
                    // Update fog of war on client.
                    if (OwnerPlayerId == Networking.NetworkPlayer.AuthorityInstance.PlayerId)
                    {
                        // This is an owned unit, update the group of cells around the unit's
                        // old and new cells.
                        if (_occupiedCell != null)
                        {
                            HexCellVisibilityManager.UpdateVisibilityAroundCell(_occupiedCell, false);
                        }
                        HexCellVisibilityManager.UpdateVisibilityAroundCell(value, true);
                    }
                    else if (_occupiedCell != null)
                    {
                        if (value.IsVisible && !_occupiedCell.IsVisible)
                        {
                            // This is an enemy unit which just entered a visible cell from an invisible cell,
                            // make it visible.
                            ChangeVisualPrefabVisibility(true, true);
                        }
                        else if (!value.IsVisible && _occupiedCell.IsVisible)
                        {
                            // This is an enemy unit which just entered an invisible cell from a visible cell,
                            // make it invisible.
                            ChangeVisualPrefabVisibility(false, true);
                        }
                    }
                }

                _occupiedCell = value;

                if (value != null)
                {
                    _occupiedCell.Unit = this;
                }
            }
        }

        /// <summary>
        /// The backing field for the OwnerPlayerId.
        /// </summary>
        private int _ownerPlayerId;

        /// <summary>
        /// The unique id of the NetworkPlayer who owns this unit.
        /// If setting on the client, the unit visal prefab instance's 
        /// color will also be updated.
        /// </summary>
        public int OwnerPlayerId 
        {
            get { return _ownerPlayerId; } 
            set
            {
                _ownerPlayerId = value;
                if (VisualPrefabInstance != null)
                {
                    SetVisualPrefabInstanceColor();
                }
            } 
        }

        /// <summary>
        /// Denotes whether or not the Unit is selected on the client.
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// If IsSelected, denotes an index used to associate this
        /// unit with other visual objects relating to selection.
        /// </summary>
        public int SelectionIndex { get; set; }

        /// <summary>
        /// A value which denotes a tick a unit began a component operation,
        /// like moving or attacking. As such, units with the same OperationTick
        /// owned by the same player are accomplishing a task together. Useful for
        /// grouping such units.
        /// </summary>
        public int OperationTick { get; set; }

        /// <summary>
        /// The unit component that allows for and handles movement.
        /// </summary>
        public Mobile Mobile { get; private set; }

        /// <summary>
        /// The unit component that allows for and handles attacking.
        /// </summary>
        public Attacker Attacker { get; private set; }

        /// <summary>
        /// The unit component that provides health functionality.
        /// </summary>
        public Health Health { get; protected set; }

        /// <summary>
        /// The unit component that allows for calling friendly units
        /// to attack an attacking unit.
        /// </summary>
        public ReinforcementSeeker ReinforcementSeeker { get; protected set; }

        /// <summary>
        /// The instantiated prefab containing a SpriteRenderer for visualization on
        /// the client.
        /// </summary>
        public SpriteRenderer VisualPrefabInstance { get; private set; }

        /// <summary>
        /// The instantiated prefab containing a SpriteRenderer to outline the
        /// VisualPrefabInstance. Note: this is a child transform of VisualPrefabInstance.
        /// </summary>
        public SpriteRenderer VisualOutlineInstance { get; private set; }

        /// <summary>
        /// A list of tweens for fog of war animation.
        /// </summary>
        private List<Tween> _fadeTweens;

        private static readonly Color UnownedVisualColor = Color.black;

        /// <summary>
        /// Denotes the number of units created.
        /// </summary>
        private static int s_UnitsCreated;

        /// <summary>
        /// An event to notify subscribers when a unit was created.
        /// </summary>
        public static event Action<Unit> UnitCreated;

        /// <summary>
        /// An event to notify subscribers when a unit was destroyed.
        /// </summary>
        public static event Action<Unit> UnitDestroyed;

        /// <summary>
        /// Create a unit and specify its type, its starting cell, and owner player.
        /// <para>
        /// If created on the server, clients will be notified to create the unit.
        /// When created on the client, the associated prefab for rendering will be
        /// instantiated.
        /// </para>
        /// </summary>
        /// <param name="data">The UnitData for the desired unit type.</param>
        /// <param name="occupiedCell">The HexCell to spawn the unit on.</param>
        /// <param name="ownerPlayerId">The id of the player who owns this unit.</param>
        /// <param name="useDefaultComponents">Denotes whether or not to create default 
        /// unit components (Mobile, Attacker, Health) for the unit.</param>
        public Unit(UnitData data, HexCell occupiedCell, int ownerPlayerId, 
            bool useDefaultComponents)
        {
            Data = data;
            OwnerPlayerId = ownerPlayerId;
            OccupiedCell = occupiedCell;

            if (useDefaultComponents)
            {
                Mobile = new Mobile(this);
                Health = new Health(this);
                Attacker = new Attacker(this);
                ReinforcementSeeker = new ReinforcementSeeker(this);
            }

            if (NetworkServer.active)
            {
                SpawnUnitMessage msg = new SpawnUnitMessage
                {
                    Id = data.Id,
                    CellIndex = occupiedCell.Index,
                    OwnerPlayerId = ownerPlayerId
                };

                foreach (NetworkConnection conn in GameNetworkManager.NetworkPlayers.Keys)
                {
                    conn.Send(msg);
                }

                UnitCreated?.Invoke(this);
            }
            else
            {
                // Create the visual prefab for client visualization.
                VisualPrefabInstance = UnityEngine.Object.Instantiate(data.VisualPrefab,
                    _occupiedCell.VisualPosition, data.VisualPrefab.transform.rotation);

                // Set the visual prefab's visual data.
                Material visualMaterial = data.VisualMaterials[0];
                if (data.IsAmphibious) 
                {
                    visualMaterial = occupiedCell.TerrainType.IsLand ? 
                        data.VisualMaterials[0] : data.VisualMaterials[1];
                }
                VisualPrefabInstance.sprite = data.Sprite;
                VisualPrefabInstance.material = visualMaterial;

                // Store the outline sprite renderer and set its visual data.
                VisualOutlineInstance = VisualPrefabInstance.transform.
                    GetChild(0).GetComponent<SpriteRenderer>();
                VisualOutlineInstance.sprite = data.SpriteOutline;
                VisualOutlineInstance.material = visualMaterial;

                // Create the necessary data structures for holding animation tweens.
                _fadeTweens = new List<Tween>();

                SetVisualPrefabInstanceColor();

                // Set a different operation tick for every unit on the client, 
                // as the HexPathfinder is dependent on this for deciding whether a unit-occupied 
                // cell should be included or ignored in a path. On the client, a unit-occuiped cell 
                // should never be included, as paths are only visualized when one unit is selected,
                // so different values will make sure of this.
                OperationTick = s_UnitsCreated++;
            }
        }

        /// <summary>
        /// Change the visual prefab's sprite's color depending on player id
        /// and visibility.
        /// </summary>
        public void SetVisualPrefabInstanceColor()
        {
            Color newColor;
            if (OwnerPlayerId != -1)
            {
                // Owned by player.
                newColor = Networking.NetworkPlayer.GetColorForId(OwnerPlayerId);
            }
            else
            {
                // Unowned by any player.
                newColor = UnownedVisualColor;
            }

            newColor.a = _occupiedCell.IsVisible ? 1f : 0f;

            VisualPrefabInstance.color = newColor;

            if (!_occupiedCell.IsVisible)
            {
                VisualPrefabInstance.enabled = false;
                VisualOutlineInstance.enabled = false;
            }
        }

        /// <summary>
        /// Change the unit's visual prefab instance's visibility.
        /// </summary>
        /// <param name="visible">Denotes whether or not to make the visual 
        /// prefab instance visible or invisible.</param>
        /// <param name="fadeOutline">Denotes whether or not to vade the visual
        /// outline as well.</param>
        public virtual void ChangeVisualPrefabVisibility(bool visible, bool fadeOutline)
        {
            float alphaValue = visible ? 1f : 0f;
            if (visible && VisualPrefabInstance.color.a < 1f)
            {
                CompleteFadeTweens();
                VisualPrefabInstance.enabled = true;
                _fadeTweens.Add(VisualPrefabInstance.DOFade(alphaValue, 0.5f));
                if (fadeOutline)
                {
                    VisualOutlineInstance.enabled = true;
                    _fadeTweens.Add(VisualOutlineInstance.DOFade(alphaValue, 0.5f));
                }
            }
            else if (!visible && VisualPrefabInstance.color.a > 0f)
            {
                CompleteFadeTweens();
                _fadeTweens.Add(DOTween.Sequence()
                    .Append(VisualPrefabInstance.DOFade(alphaValue, 0.5f))
                    .AppendCallback(delegate() 
                    { 
                        VisualPrefabInstance.enabled = false; 
                    }));

                if (fadeOutline)
                {
                    _fadeTweens.Add(DOTween.Sequence()
                        .Append(VisualOutlineInstance.DOFade(alphaValue, 0.5f))
                        .AppendCallback(delegate ()
                        {
                            VisualOutlineInstance.enabled = false;
                        }));
                }
            }
        }

        /// <summary>
        /// Complete any previous fade tweens and sequences.
        /// </summary>
        private void CompleteFadeTweens()
        {
            for (int i = 0; i < _fadeTweens.Count; i++)
            {
                _fadeTweens[i].Complete();
            }
            _fadeTweens.Clear();
        }

        /// <summary>
        /// Destroy the unit. This includes any visual GameObjects 
        /// created on the client.
        /// </summary>
        public void Destroy()
        {
            // Clear reference to this Unit instance on HexGrid for the
            // garbage collector.
            _occupiedCell.Unit = null;
            if (NetworkServer.active)
            {
                Mobile.PrepareForDisposal();
                Attacker.PrepareForDisposal();
                UnitDestroyed?.Invoke(this);
            }
            else
            {
                if (OwnerPlayerId == Networking.NetworkPlayer.AuthorityInstance.PlayerId)
                {
                    // Update fog of war around dead unit on client if dead unit was owned.
                    HexCellVisibilityManager.UpdateVisibilityAroundCell(_occupiedCell, false);
                }
            }
            if (VisualPrefabInstance != null)
            {
                // Destroy visual instance after damage tween animation (0.2f seconds) 
                // and delete late.
                DOTween.Sequence().AppendInterval(0.2f)
                    .AppendCallback(delegate() 
                    { 
                        VisualPrefabInstance.DOComplete();
                        VisualOutlineInstance.DOComplete();
                        UnityEngine.Object.Destroy(VisualPrefabInstance);
                        UnityEngine.Object.Destroy(VisualOutlineInstance);
                    });
            }
        }
    }
}