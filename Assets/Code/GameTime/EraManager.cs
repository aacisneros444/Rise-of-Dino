using Assets.Code.Networking;
using Assets.Code.Networking.Messaging;
using Mirror;
using UnityEngine;
using System;
using Assets.Code.FX;

namespace Assets.Code.GameTime
{
    /// <summary>
    /// A class to manage game "eras", periods of time
    /// in which certain territory upgrades unlock.
    /// </summary>
    public class EraManager : MonoBehaviour
    {
        public static EraManager Instance { get; private set; }

        /// <summary>
        /// The number of eras there are.
        /// </summary>
        public const int NumEras = 4;

        /// <summary>
        /// The names of all eras.
        /// </summary>
        public static readonly string[] EraNames =
        {
            "Triassic", "Jurassic", "Cretaceous", "Extinction"
        };

        /// <summary>
        /// The time in minutes it will take for the next era to begin.
        /// </summary>
        public static readonly int[] EraTimesToAdvance =
        {   // 10, 15, 25 10
            10, 15, 25, 10
        };

        /// <summary>
        /// The number of ticks needed to elapse to advance to the next era.
        /// </summary>
        public int CurrentTickTimeToAdvance 
        { 
            get 
            { 
                return TickSystem.TicksPerMinute * EraTimesToAdvance[CurrentEraIndex]; 
            } 
        }

        /// <summary>
        /// The name of the current era.
        /// </summary>
        public string CurrentEraName { get { return EraNames[CurrentEraIndex]; } }

        /// <summary>
        /// The index for the current era the game is in.
        /// </summary>
        public int CurrentEraIndex { get; private set; }

        /// <summary>
        /// The max territory level for the current game era.
        /// </summary>
        public int MaxTerritoryEraLevel { get { return CurrentEraIndex + 1; } }

        /// <summary>
        /// The number of ticks elapsed since the era started.
        /// </summary>
        private int _ticksElapsedSinceEraStart;

        /// <summary>
        /// An event to notify subscribers on the client when 
        /// a new tick value since era start has been received 
        /// from the server.
        /// </summary>
        public static event Action<int> ClientReceivedEraTicks;

        /// <summary>
        /// An event to notify subscribers on the client
        /// when a new era has started.
        /// int = era index,
        /// string = era name
        /// bool = initial era state update
        /// </summary>
        public static event Action<int, string, bool> ClientUpdatedEra;

        /// <summary>
        /// The sound to play on the client when a new era has been reached.
        /// </summary>
        [SerializeField] private AudioClip _newEraSound;

        /// <summary>
        /// Set the singleton instance.
        /// </summary>
        private void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Subscribe to the TickSystem to know which a tick has occured.
        /// </summary>
        private void Start()
        {
            if (!NetworkClient.active)
            {
                TickSystem.Tick += OnTick;
            }
            else
            {
                NetworkClient.RegisterHandler<SetEraTicksElapsedMessage>(SetEraTicksElapsedFromServer);
                NetworkClient.RegisterHandler<SetEraMessage>(SetEraFromServer);
                NetworkClient.RegisterHandler<SetInitialEraStateMessage>(SetInitialEraStateFromServer);
            }
        }

        /// <summary>
        /// Unsubscibe to the TickSystem to prevent unintentional behavior.
        /// </summary>
        private void OnDestroy()
        {
            TickSystem.Tick -= OnTick;
        }

        /// <summary>
        /// If enough ticks have elapsed, advance to the next era.
        /// </summary>
        private void OnTick()
        {
            if (GameNetworkManager.NetworkPlayers != null)
            {
                _ticksElapsedSinceEraStart++;

                if (_ticksElapsedSinceEraStart % 10 == 0 && GameNetworkManager.NetworkPlayers.Count > 0)
                {
                    SetEraTicksElapsedMessage msg = new SetEraTicksElapsedMessage
                    {
                        TicksElapsed = _ticksElapsedSinceEraStart
                    };

                    foreach (NetworkConnection conn in GameNetworkManager.NetworkPlayers.Keys)
                    {
                        conn.Send(msg);
                    }
                }

                if (_ticksElapsedSinceEraStart == CurrentTickTimeToAdvance)
                {
                    _ticksElapsedSinceEraStart = 0;

                    CurrentEraIndex++;
                    if (CurrentEraIndex == EraNames.Length)
                    {
                        // Temporary clamp.
                        CurrentEraIndex = EraNames.Length - 1;
                        TickSystem.Tick -= OnTick;

                        SetAllCellsVisibleMessage msg1 = new SetAllCellsVisibleMessage
                        {

                        };

                        foreach (NetworkConnection conn in GameNetworkManager.NetworkPlayers.Keys)
                        {
                            conn.Send(msg1);
                        }

                        Debug.Log("Game finished. Closing server.");
                        NetworkServer.Shutdown();
                    }

                    SetEraMessage msg = new SetEraMessage
                    {
                        EraIndex = CurrentEraIndex
                    };

                    foreach (NetworkConnection conn in GameNetworkManager.NetworkPlayers.Keys)
                    {
                        conn.Send(msg);
                    }
                }
            }     
        }

        /// <summary>
        /// Send a client all the game's current era data.
        /// </summary>
        /// <param name="conn">The client's NetworkConnection.</param>
        public void SendClientEraData(NetworkConnection conn)
        {
            SetInitialEraStateMessage msg = new SetInitialEraStateMessage
            {
                EraIndex = CurrentEraIndex,
                TicksElapsed = _ticksElapsedSinceEraStart
            };

            conn.Send(msg);
        }

        /// <summary>
        /// Set the number of ticks elapsed in the current era after receiving a
        /// SetEraTicksElapsedMessage from the server.
        /// </summary>
        /// <param name="msg">The SetEraTicksElapsedMessage from the server.</param>
        private void SetEraTicksElapsedFromServer(SetEraTicksElapsedMessage msg)
        {
            _ticksElapsedSinceEraStart = msg.TicksElapsed;
            ClientReceivedEraTicks?.Invoke(_ticksElapsedSinceEraStart);
        }

        /// <summary>
        /// Set the current era after receiving a SetEraMessage from the server.
        /// </summary>
        /// <param name="msg">The SetEraMessage received from the server.</param>
        private void SetEraFromServer(SetEraMessage msg)
        {
            CurrentEraIndex = msg.EraIndex;
            ClientUpdatedEra?.Invoke(CurrentEraIndex, CurrentEraName, false);
            SoundManager.Instance.PlaySound(_newEraSound);
        }

        /// <summary>
        /// Set the current era state after receiving a SetInitialEraStateMessage
        /// from the server.
        /// </summary>
        /// <param name="msg">The SetInitialEraStateMessage from the server.</param>
        private void SetInitialEraStateFromServer(SetInitialEraStateMessage msg)
        {
            CurrentEraIndex = msg.EraIndex;
            _ticksElapsedSinceEraStart = msg.TicksElapsed;
            ClientReceivedEraTicks?.Invoke(_ticksElapsedSinceEraStart);
            ClientUpdatedEra?.Invoke(CurrentEraIndex, CurrentEraName, true);
        }
    }
}