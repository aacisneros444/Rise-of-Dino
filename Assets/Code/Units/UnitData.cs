using UnityEngine;
using Assets.Code.FX;

namespace Assets.Code.Units
{
    /// <summary>
    /// A scriptable object to hold data about a unit type.
    /// </summary>
    [CreateAssetMenu(fileName = "New Unit Data",
        menuName = "Unit/Unit Data")]
    public class UnitData : ScriptableObject
    {
        [Header("Identifiers")]
        [Tooltip("The unique identifier for this unit type. " +
            "The index denoting what unit type this is.")]
        [SerializeField] private int _id;

        /// <summary>
        /// The unique identifier for this unit type. The index 
        /// denoting what unit type this is.
        /// </summary>
        public int Id { get { return _id; } }

        [Tooltip("The name of this unit type.")]
        [SerializeField] private string _name;

        /// <summary>
        /// The name of this unit type.
        /// </summary>
        public string Name { get { return _name; } }


        [Tooltip("The unit's level, with level 3 being the best and level 1 " +
            "being the worst. A general indication of a unit's preferability.")]
        [SerializeField] private int _level;

        /// <summary>
        /// The unit's level, with level 3 being the best and level 1 being
        /// the worst. A general indication of a unit's preferability.
        /// </summary>
        public int Level { get { return _level; } }


        [Header("Player Interaction")]
        [Tooltip("Denotes whether or not this unit can be selected.")]
        [SerializeField] private bool isSelectable;

        /// <summary>
        /// Denotes whether or not this unit can be selected.
        /// </summary>
        public bool IsSelectable { get { return isSelectable; } }


        [Header("Visuals")]
        [Tooltip("The sprite the unit will use for rendering.")]
        [SerializeField] private Sprite _sprite;

        /// <summary>
        /// The sprite the unit will use for rendering.
        /// </summary>
        public Sprite Sprite { get { return _sprite; } }

        [Tooltip("The sprite to outline the main sprite used for rendering.")]
        [SerializeField] private Sprite _spriteOutline;

        /// <summary>
        /// The sprite to outline the main sprite used for rendering.
        /// </summary>
        public Sprite SpriteOutline { get { return _spriteOutline; } }


        [Tooltip("A tight meshed version of the unit sprite. " +
            "Useful for some UI features, like partial fill.")]
        [SerializeField] private Sprite _tightMeshSprite;

        /// <summary>
        /// A tight meshed version of the unit sprite. Useful for some UI features, 
        /// like partial fill.
        /// </summary>
        public Sprite TightMeshSprite { get { return _tightMeshSprite; } }


        [Header("Locomotion")]
        [Tooltip("True if unit can walk on land, false otherwise.")]
        [SerializeField] private bool _isWalker;

        /// <summary>
        /// True if unit can walk on land, false otherwise.
        /// </summary>
        public bool IsWalker { get { return _isWalker; } }

        [Tooltip("True if unit can swim in water, false otherwise.")]
        [SerializeField] private bool _isSwimmer;

        /// <summary>
        /// True if unit can swim in water, false otherwise.
        /// </summary>
        public bool IsSwimmer { get { return _isSwimmer; } }

        /// <summary>
        /// Denotes if the unit can traverse both water and land.
        /// </summary>
        public bool IsAmphibious { get { return _isWalker && _isSwimmer; } }

        [Tooltip("The number of ticks that must elapse before a unit moves.")]
        [SerializeField] private int _moveTicks;

        /// <summary>
        /// The number of ticks that must elapse before a unit moves.
        /// </summary>
        public int MoveTicks { get { return _moveTicks; } }

        [Tooltip("The sound(s) to play when the unit moves. The unit's" +
            " default move sound resides at index 0; however, in amphibious units," +
            " index 0 is assigned a land movement sound while index 1 is assigned" +
            " a water move sound.")]
        [SerializeField] private AudioClip[] _moveSounds;

        /// <summary>
        /// The sound(s) to play when the unit moves. The unit's
        /// default move sound resides at index 0; however, in amphibious units,
        /// index 0 is assigned a land movement sound while index 1 is assigned a water
        /// move sound.
        /// </summary>
        public AudioClip[] MoveSounds{ get { return _moveSounds; } }

        /// <summary>
        /// Denotes whether the unit can move or not.
        /// </summary>
        public bool CanMove { get { return _isWalker || _isSwimmer; } }

        [Header("Attacking")]
        [Tooltip("The maximum distance in HexCells the unit can be from a " +
            "target to attack.")]
        [SerializeField] private int _attackRange;

        /// <summary>
        /// The maximum distance in HexCells the unit can be from a target to attack.
        /// </summary>
        public int AttackRange { get { return _attackRange; } }

        [Tooltip("The number of ticks that must elapse before a unit attacks.")]
        [SerializeField] private int _attackTicks;

        /// <summary>
        /// The number of ticks that must elapse before a unit attacks.
        /// </summary>
        public int AttackTicks { get { return _attackTicks; } }

        [Tooltip("The amount of damage the unit will deal per attack.")]
        [SerializeField] private int _attackDamage;

        /// <summary>
        /// The amount of damage the unit will deal per attack.
        /// </summary>
        public int AttackDamage { get { return _attackDamage; } }

        /// <summary>
        /// Denotes whether or not the unit can attack.
        /// </summary>
        public bool CanAttack { get { return AttackDamage > 0; } }

        [Tooltip("The attack visual effect type to play when the unit attacks.")]
        [SerializeField] private ProceduralEffect.EffectType _attackEffect;

        /// <summary>
        /// The attack visual effect to play when the unit attacks.
        /// </summary>
        public ProceduralEffect.EffectType AttackEffect { get { return _attackEffect; } }


        [Header("Health")]
        [Tooltip("The amount of health for a unit to have.")]
        [SerializeField] private int _health;

        /// <summary>
        /// The amount of health for a unit to have.
        /// </summary>
        public int Health { get { return _health; } }

        [Tooltip("The sound to play when the unit gets hurt.")]
        [SerializeField] private AudioClip _hurtSound;

        /// <summary>
        /// The sound to play when the unit gets hurt.
        /// </summary>
        public AudioClip HurtSound { get { return _hurtSound; } }


        [Header("Visualization")]
        [Tooltip("A prefab to be instantiated containing a sprite renderer " +
            "for visualization.")]
        [SerializeField] private SpriteRenderer _visualPrefab;

        /// <summary>
        /// A prefab to be instantiated containing a sprite renderer for visualization.
        /// </summary>
        public SpriteRenderer VisualPrefab { get { return _visualPrefab; } }

        [Tooltip("The material(s) to use for the unit's visual prefab. " +
            "The unit's default material resides at index 0; however, " +
            "in amphibious units index 0 is assigned a land visual material " +
            "while index 1 is assigned a water visual material.")]
        [SerializeField] private Material[] _visualMaterials;

        /// <summary>
        /// The material(s) to use for the unit's visual prefab. 
        /// The unit's default material resides at index 0; however, 
        /// in amphibious units index 0 is assigned a land visual material 
        /// while index 1 is assigned a water visual material.
        /// </summary>
        public Material[] VisualMaterials { get { return _visualMaterials; } }

        [Header("Spawning")]
        [Tooltip("The number of ticks it takes for a territory to spawn a unit.")]
        [SerializeField] private int _spawnTicks;

        /// <summary>
        /// The number of ticks it takes for a territory to spawn a unit.
        /// </summary>
        public int SpawnTicks { get { return _spawnTicks; } }
    }
}