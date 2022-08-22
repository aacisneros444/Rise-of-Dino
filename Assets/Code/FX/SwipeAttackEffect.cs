using UnityEngine;
using DG.Tweening;

namespace Assets.Code.FX
{
    /// <summary>
    /// A procedural effect to model a swipe attack.
    /// </summary>
    public class SwipeAttackEffect : ProceduralEffect
    {
        /// <summary>
        /// The swipe sprite renderer to move.
        /// </summary>
        [SerializeField] private SpriteRenderer _swipe;

        /// <summary>
        /// The swip sound to play.
        /// </summary>
        [SerializeField] private AudioClip _swipeSound;
        
        /// <summary>
        /// A value denoting how far away to start the attack animation.
        /// </summary>
        private const float AttackOffset = 0.75f;

        /// <summary>
        /// Offsets from the given position to start the animation.
        /// </summary>
        private readonly Vector3[] InititalAttackOffsets =
        {
            // Top left
            new Vector3(-AttackOffset, 0f, AttackOffset),
            // Top right
            new Vector3(AttackOffset, 0f, AttackOffset),
            // Bottom left
            new Vector3(-AttackOffset, 0f, -AttackOffset),
            // Bottom right
            new Vector3(AttackOffset, 0f , -AttackOffset)
        };

        /// <summary>
        /// Offsets from the given position to end the animation.
        /// </summary>
        private readonly Vector3[] FinalAttackOffsets =
        {
            new Vector3(AttackOffset, 0f , -AttackOffset),
            new Vector3(-AttackOffset, 0f, -AttackOffset),
            new Vector3(AttackOffset, 0f, AttackOffset),
            new Vector3(-AttackOffset, 0f, AttackOffset)
        };

        public override void Play(Vector3 position)
        {
            // Pick a random attack offset from those defined to use.
            int offset = Random.Range(0, InititalAttackOffsets.Length);

            // Set the starting position and rotation.
            _swipe.transform.position = position + InititalAttackOffsets[offset];
            _swipe.transform.rotation = Quaternion.Euler(90, -60, 0);

            // Set the starting color.
            Color setColor = _swipe.color;
            _swipe.color = new Color(setColor.r, setColor.g, setColor.b, 1f);

            // Move linearly from left to right and rotate.
            _swipe.transform.DOMove(position + FinalAttackOffsets[offset], 0.4f);
            _swipe.transform.DORotate(new Vector3(90, 60, 0), 0.4f);

            // Interpolate to 0 alpha.
            _swipe.DOColor(new Color(setColor.r, setColor.g, setColor.b, 0f),
                1f).OnComplete(Disable);

            SoundManager.Instance.PlaySoundAtPoint(_swipeSound, position);
        }

        public override void SetColor(Color color)
        {
            _swipe.color = color;
        }
    }
}
