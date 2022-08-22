using UnityEngine;
using System.Collections.Generic;

namespace Assets.Code.FX
{
    /// <summary>
    /// A singleton manager class to handle playing sounds.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static SoundManager Instance { get; private set; }

        [Tooltip("The attached audio source to play non-spatial 2D one-shot sounds.")]
        [SerializeField] private AudioSource _audioSource;

        [Tooltip("The number of audio sources to pool.")]
        [SerializeField] private int _audioSourcePoolCount = 32;

        /// <summary>
        /// An object pool for AudioSources.
        /// </summary>
        private Queue<AudioSource> _audioSourcePool;

        /// <summary>
        /// An array containing a SoundHistory for each
        /// sound clip provided.
        /// </summary>
        private Dictionary<string, SoundHistory> _soundHistories;

        /// <summary>
        /// The minimum amount of time that must elapse before the same sound
        /// can be played again.
        /// </summary>
        private const float RepeatSoundTimeTolerance = 0.1f;

        /// <summary>
        /// The minimum distance away from a previous playing a of a sound clip 
        /// to play the clip again.
        /// </summary>
        private const float RepeatSoundDistanceTolerance = 15f;

        /// <summary>
        /// Set the singleton instance and initialize the necessary
        /// data structures
        /// </summary>
        private void Awake()
        {
            Instance = this;

            _audioSourcePool = new Queue<AudioSource>();
            for (int i = 0; i < _audioSourcePoolCount; i++)
            {
                GameObject sourceGameObject = new GameObject("One Shot Audio");
                sourceGameObject.transform.parent = transform;

                AudioSource audioSource = sourceGameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.maxDistance = 125f;
                audioSource.volume = 1f;
                _audioSourcePool.Enqueue(audioSource);
            }

            _soundHistories = new Dictionary<string, SoundHistory>();
        }

        /// <summary>
        /// Play a 2d, one-shot sound.
        /// </summary>
        /// <param name="sound">The audio clip to play.</param>
        public void PlaySound(AudioClip sound)
        {
            _audioSource.PlayOneShot(sound);
        }

        /// <summary>
        /// Play a sound who's volume will be affected by distance to
        /// the camera.
        /// </summary>
        /// <param name="sound">The audio clip to play.</param>
        /// <param name="position">The position to play the sound at.</param>
        public void PlaySoundAtPoint(AudioClip sound, Vector3 position)
        {
            SoundHistory soundHistory;
            if (!_soundHistories.TryGetValue(sound.name, out soundHistory))
            {
                soundHistory = new SoundHistory
                {
                    LastPlayedTime = 0,
                    LastPlayedPosition = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity)
                };
            }

            float distanceToLastPlayed = Vector3.Distance(position, soundHistory.LastPlayedPosition);
            float timeSinceLastPlayed = Time.time - soundHistory.LastPlayedTime;
            if (timeSinceLastPlayed > RepeatSoundTimeTolerance ||
                distanceToLastPlayed > RepeatSoundDistanceTolerance)
            {
                AudioSource audioSource = _audioSourcePool.Dequeue();
                audioSource.gameObject.SetActive(true);
                audioSource.transform.position = position;
                audioSource.clip = sound;
                audioSource.transform.position = position;
                audioSource.pitch = Random.Range(0.9f, 1f);
                audioSource.Play();
                _audioSourcePool.Enqueue(audioSource);
                _soundHistories[sound.name] = new SoundHistory
                {
                    LastPlayedTime = Time.time,
                    LastPlayedPosition = position
                };
            }
        }

        /// <summary>
        /// A class to contain data about the last
        /// time a specific sound clip was played.
        /// </summary>
        private class SoundHistory
        {
            public float LastPlayedTime;
            public Vector3 LastPlayedPosition;
        }
    }
}