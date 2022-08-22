using UnityEngine;
using UnityEngine.SceneManagement;
using Assets.Code.Networking;

namespace Assets.Code.PlayerInput.ScreenUI
{
    public class DisconnectButton : MonoBehaviour
    {
        [SerializeField] private GameNetworkManager _networkManager;

        private void Start()
        {
            _networkManager = GameNetworkManager.Instance;
        }

        public void DisconnectOnClick()
        {
            DG.Tweening.DOTween.Clear();
            _networkManager.StopClient();
            SceneManager.LoadScene("MainMenuScene", LoadSceneMode.Single);
        }
    }
}
