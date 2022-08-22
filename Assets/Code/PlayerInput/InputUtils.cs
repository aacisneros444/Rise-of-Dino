using UnityEngine;

namespace Assets.Code.PlayerInput
{
    /// <summary>
    /// A utilities calass for player input.
    /// </summary>
    public static class InputUtils
    {
        /// <summary>
        /// Get the current mouse position in world space.
        /// </summary>
        /// <returns>The mouse position in world space.</returns>
        public static Vector3 GetMousePoint()
        {
            Vector2 mousePosition = UnityEngine.Input.mousePosition;
            return Camera.main.ScreenToWorldPoint(
                new Vector3(mousePosition.x, mousePosition.y, Camera.main.farClipPlane));
        }
    }
}
