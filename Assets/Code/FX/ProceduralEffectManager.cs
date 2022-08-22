using System.Collections.Generic;
using UnityEngine;
using Assets.Code.Hex;

namespace Assets.Code.FX
{
    /// <summary>
    /// A singleton manager class to handle procedural effects.
    /// </summary>
    public class ProceduralEffectManager : MonoBehaviour
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static ProceduralEffectManager Instance { get; private set; }

        [Tooltip("Set the object pooling parameters for the effect prefabs to be instantiated.")]
        [SerializeField] private List<PoolParameters> _effectPoolParameters = new List<PoolParameters>();

        /// <summary>
        /// The object pools of the set effect prefabs defined in effect pool parameters.
        /// </summary>
        private Dictionary<int, Queue<ProceduralEffect>> _effectPools;

        /// <summary>
        /// Set the singleton instance and create the object pools for the 
        /// effect prefabs given the effect pool parameters.
        /// </summary>
        private void Awake()
        {
            Instance = this;

            _effectPools = new Dictionary<int, Queue<ProceduralEffect>>();
            foreach (PoolParameters poolParameters in _effectPoolParameters)
            {
                Queue<ProceduralEffect> pool = new Queue<ProceduralEffect>();
                for (int i = 0; i < poolParameters.size; i++)
                {
                    ProceduralEffect proceduralEffectInstance = Instantiate(poolParameters.effectPrefab, transform);
                    proceduralEffectInstance.gameObject.SetActive(false);
                    pool.Enqueue(proceduralEffectInstance);
                }
                _effectPools.Add(poolParameters.key, pool);
            }
        }

        /// <summary>
        /// Play a procedural effect at a given position.
        /// </summary>
        /// <param name="key">The key associated with the procedural effect.</param>
        /// <param name="position">The position to play the effect at.</param>
        /// <param name="color">A color to apply to the effect.</param>
        public void PlayEffect(int key, Vector3 position, Color color)
        {
            ProceduralEffect proceduralEffect = _effectPools[key].Dequeue();
            _effectPools[key].Enqueue(proceduralEffect);
            proceduralEffect.gameObject.SetActive(true);
            proceduralEffect.SetColor(color);
            proceduralEffect.Play(position);
        }

    }

    /// <summary>
    /// Parameters for a single procedural effect pool.
    /// </summary>
    [System.Serializable]
    public class PoolParameters
    {
        /// <summary>
        /// The dictionary key for the prefab set in this pool.
        /// </summary>
        public int key;
        /// <summary>
        /// The procedural effect prefab.
        /// </summary>
        public ProceduralEffect effectPrefab;
        /// <summary>
        /// The number of prefabs to instantiate.
        /// </summary>
        public int size;
    }
}