using UnityEngine;
using System;

namespace Assets.Code.GameTime
{
    /// <summary>
    /// A class that models a tick system for the  systematic 
    /// keeping of time on the server. Invokes the tick action 
    /// every tick for listening classes.
    /// </summary>
    public class TickSystem : MonoBehaviour
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static TickSystem Instance { get; private set; }

        /// <summary>
        /// The amount of time taken up by one tick.
        /// </summary>
        public const float TickTime = 0.1f;

        /// <summary>
        /// The number of ticks elapsed per minute.
        /// </summary>
        public static int TicksPerMinute { get { return (int)(60 / TickTime); } }

        /// <summary>
        /// The time that has passed since the last tick.
        /// </summary>
        private float _timeSinceLastTick;

        /// <summary>
        /// Invoked every tick by the TickSystem singleton. 
        /// To client classes: subscribe to this to be notified 
        /// of when a tick occurs.
        /// </summary>
        public static event Action Tick;

        /// <summary>
        /// The current tick number.
        /// </summary>
        public static int TickNumber;

        /// <summary>
        /// Keep track of time and invoke the Tick action when enough
        /// time has passed.
        /// </summary>
        private void Update()
        {
            _timeSinceLastTick += UnityEngine.Time.deltaTime;
            if (_timeSinceLastTick >= TickTime)
            {
                _timeSinceLastTick -= TickTime;
                TickNumber++;
                Tick?.Invoke();
            }
        }
    }
}