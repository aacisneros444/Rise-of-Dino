using UnityEngine;

namespace Assets.Code.PlayerInput.ScreenUI
{
    public class ExitButton : MonoBehaviour
    {
        public void QuitOnClicked()
        {
            Application.Quit();
        }
    }
}
