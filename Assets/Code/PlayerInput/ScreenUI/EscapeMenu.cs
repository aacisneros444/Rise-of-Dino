using UnityEngine;

namespace Assets.Code.PlayerInput.ScreenUI
{
    public class EscapeMenu : MonoBehaviour
    {
        [SerializeField] private Canvas _escapeMenuCanvas;

        /// <summary>
        /// Toggle the escape menu if the escape key is pressed.
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _escapeMenuCanvas.enabled = !_escapeMenuCanvas.isActiveAndEnabled;
            }
        }
    }
}