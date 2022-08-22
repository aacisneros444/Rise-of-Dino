using UnityEngine;
using System.Collections.Generic;

namespace Assets.Code.Units
{
    /// <summary>
    /// A class to handle destroying units and sending final unit health updates to 
    /// the client at the end of the update cycle on the server, since destroying units 
    /// in the middle of Update() (setting health to 0) can cause null reference issues 
    /// on the client. This also allows all qualifying units to attack before
    /// the tick ends.
    /// </summary>
    public class LateUnitDestroyer : MonoBehaviour
    {
        /// <summary>
        /// A queue of units to destroy / send final health updates
        /// for.
        /// </summary>
        private static Queue<Unit> _unitsToDestroy;

        /// <summary>
        /// Initialize the _unitsToDestroy queue.
        /// </summary>
        private void Awake()
        {
            _unitsToDestroy = new Queue<Unit>();
        }

        /// <summary>
        /// Destroy and send health updates for all queued units at the end of 
        /// the update cycle.
        /// </summary>
        private void LateUpdate()
        {
            while (_unitsToDestroy.Count > 0)
            {
                Unit unit = _unitsToDestroy.Dequeue();
                unit.Health.SendHealthValueToClients();
                unit.Destroy();
            }
        }

        /// <summary>
        /// Queue a unit to be destroyed at the end of the update cycle.
        /// </summary>
        /// <param name="unit">The unit to destroy.</param>
        public static void Enqueue(Unit unit)
        {
            _unitsToDestroy.Enqueue(unit);
        }
    }
}