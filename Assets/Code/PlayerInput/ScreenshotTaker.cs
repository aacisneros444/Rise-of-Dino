using UnityEngine;

namespace Assets.Code.PlayerInput
{
    /// <summary>
    /// A class to capture input to take a screenshot every time the player 
    /// presses a certain key.
    /// </summary>
    public class ScreenshotTaker : MonoBehaviour
    {
        private int randomStart;
        private int scNumber;

        private void Awake()
        {
            randomStart = Random.Range(-512000, 512000);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Slash))
            {
                ScreenCapture.CaptureScreenshot("screenshot " + (randomStart + scNumber) +  ".png");
                Debug.Log("Screenshot taken");
                scNumber++;
            }
        }
    }
}
