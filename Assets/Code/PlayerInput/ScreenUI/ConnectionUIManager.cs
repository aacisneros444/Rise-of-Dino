using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Code.Networking;
using Mirror;

namespace Assets.Code.PlayerInput.ScreenUI
{
    /// <summary>
    /// A class to handle connection UI, providing callbacks for buttons
    /// which handle attempting and canceling a connection to a server.
    /// </summary>
    public class ConnectionUIManager : MonoBehaviour
    {
        [SerializeField] private  GameNetworkManager _networkManager;
        [SerializeField] private TMP_InputField _ipInputField;
        [SerializeField] private Button _playButton;
        [SerializeField] private TMP_Text _connectingText;
        [SerializeField] private Button _stopConnectingButton;
            
        private void Start()
        {
            _networkManager = GameNetworkManager.Instance;
        }

        public void TryConnectOnClick()
        {
            if(GameNetworkManager.Instance.TryConnect(_ipInputField.text))
            {
                ChangeUIView(false);
            }
            else
            {
                ChangeUIView(true);
            }
        }

        public void StopConnectOnClick()
        {
            _networkManager.StopClient();
            ChangeUIView(true);
        }

        /// <summary>
        /// Change the UI view, depending on if connecting or not.
        /// </summary>
        /// <param name="useConnectUI">Denotes whether or not to use the deafult ui.</param>
        public void ChangeUIView(bool useConnectUI)
        {
            _ipInputField.gameObject.SetActive(useConnectUI);
            _playButton.gameObject.SetActive(useConnectUI);
            _connectingText.gameObject.SetActive(!useConnectUI);
            _stopConnectingButton.gameObject.SetActive(!useConnectUI);
        }
    }
}
