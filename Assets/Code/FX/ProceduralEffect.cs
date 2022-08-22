using UnityEngine;

namespace Assets.Code.FX
{
    /// <summary>
    /// An "abstract" class to model a procedural effect object,
    /// an object with a sequence of actions to achieve some sort
    /// of audio/visual effect.
    /// </summary>
    public class ProceduralEffect : MonoBehaviour
    {
        /// <summary>
        /// An enumerated type to define numerical values for effect types.
        /// </summary>
        public enum EffectType
        {
            Scratch, LargeBite, SmallBite, TailSwipe
        }

        /// <summary>
        /// Play the procedural effect.
        /// </summary>
        /// <param name="position">The position to play at.</param>
        public virtual void Play(Vector3 position) { }

        /// <summary>
        /// Set a color for the effect visuals.
        /// </summary>
        /// <param name="color">The color to use for the effect.</param>
        public virtual void SetColor(Color color) { }

        /// <summary>
        /// Disable the procedural effect GameObject.
        /// </summary>
        protected void Disable()
        {
            gameObject.SetActive(false);
        }
    }
}