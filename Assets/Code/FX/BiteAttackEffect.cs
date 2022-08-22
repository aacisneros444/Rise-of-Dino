using UnityEngine;
using DG.Tweening;

namespace Assets.Code.FX
{
    public class BiteAttackEffect : ProceduralEffect
    {
        [SerializeField] private SpriteRenderer _lowerJaw;
        [SerializeField] private SpriteRenderer _upperJaw;
        [SerializeField] private AudioClip _biteSound;

        private readonly Vector3 UpperJawStart = new Vector3(0f, 0f, 1f);
        private readonly Vector3 LowerJawStart = new Vector3(0f, 0f, -1f);

        public override void Play(Vector3 position)
        {
            // Set the starting jaw positions.
            _upperJaw.transform.position = position + UpperJawStart;
            _lowerJaw.transform.position = position + LowerJawStart;

            // Set the starting color.
            Color setColor = _lowerJaw.color;
            Color startingColor = new Color(setColor.r, setColor.g, setColor.b, 1f);
            _lowerJaw.color = startingColor;
            _upperJaw.color = startingColor;

            // Move jaws to bite.
            _lowerJaw.transform.DOMove(position, 0.4f);
            _upperJaw.transform.DOMove(position, 0.4f);

            // Interpolate to 0 alpha.
            _lowerJaw.DOColor(new Color(setColor.r, setColor.g, setColor.b, 0f),
                0.6f).OnComplete(Disable);
            _upperJaw.DOColor(new Color(setColor.r, setColor.g, setColor.b, 0f),
                0.6f).OnComplete(Disable);

            // Play the biting sound.
            SoundManager.Instance.PlaySoundAtPoint(_biteSound, position);
        }

        public override void SetColor(Color color)
        {
            _lowerJaw.color = color;
            _upperJaw.color = color;
        }
    }
}