using System.Collections.Generic;

namespace Assets.Code.Hex
{
    /// <summary>
    /// A class to model a priority queue data structure specifically made
    /// for HexCells. The priority of a HexCell in the HexCellPriorityQueue
    /// is determined by its SearchPriority property.
    /// </summary>
    public class HexCellPriorityQueue
    {
        /// <summary>
        /// The internal data structue for the HexCellPriorityQueue.
        /// This list's indices correspond to search priorities for HexCells.
        /// As there can be HexCells who's search priority is the same, the
        /// list is really a list of linked lists, as each cell holds a reference
        /// to the next cell with same priority if there happens to be one.
        /// If there are no cells possessing a priority of an index that is in the list,
        /// that index will be a null reference.
        /// </summary>
        private List<HexCell> _list = new List<HexCell>();

        /// <summary>
        /// The number of cells in this priority queue.
        /// </summary>
        public int _count;

        /// <summary>
        /// Determine if the priority queue is empty. True if empty, false otherwise.
        /// </summary>
        public bool IsEmpty { get { return _count == 0; } }

        /// <summary>
        /// The minimum search priority present in the priority queue.
        /// Allows for faster indexing of the highest priority cells.
        /// </summary>
        private int _minPriorityInQueue = int.MaxValue;

        /// <summary>
        /// Add a cell to the priority queue.
        /// </summary>
        /// <param name="cell">The cell to add to the priority queue.</param>
        public void Enqueue(HexCell cell)
        {
            _count++;
            int priority = cell.SearchPriority;
            // Save the minimum priority for faster indexing when dequeuing.
            if (priority < _minPriorityInQueue)
            {
                _minPriorityInQueue = priority;
            }
            // Let a cell's priority be its index in the internal list.
            // Create "dummy" elements / null references if not enough capacity
            // is available.
            while (priority >= _list.Count)
            {
                _list.Add(null);
            }
            // Create a linked list at every index of cells having the same priority.
            cell.NextWithSamePriority = _list[priority];
            _list[priority] = cell;
        }

        /// <summary>
        /// Remove the cell of highest priority from the priority queue.
        /// </summary>
        /// <returns>The cell at the front of the queue.</returns>
        public HexCell Dequeue()
        {
            _count--;
            // Traverse the list until the first non null element is found.
            // Start at the minimum priority so as to not iterate until
            // the first non null element.
            for (; _minPriorityInQueue < _list.Count; _minPriorityInQueue++)
            {
                HexCell cell = _list[_minPriorityInQueue];
                if (cell != null)
                {
                    // Maintain the linked list. If no more nodes available,
                    // the element at this index, i, will be set to null and skipped
                    // in the future.
                    _list[_minPriorityInQueue] = cell.NextWithSamePriority;
                    return cell;
                }
            }
            // No cells to dequeue.
            return null;
        }

        /// <summary>
        /// Change the prioirity of a cell in the priority queue.
        /// </summary>
        /// <param name="cell">The cell who's priority to change in the queue.</param>
        /// <param name="oldPriority">The cell's old priority.</param>
        public void Change(HexCell cell, int oldPriority)
        {
            HexCell current = _list[oldPriority];
            HexCell next = current.NextWithSamePriority;
            // If the first cell in the linked list at the index corresponding
            // to priority is the target cell, simply skip over it to "cut it out"
            // of the linked list.
            if (current == cell)
            {
                _list[oldPriority] = next;
            }
            else
            {
                // Traverse the linked list at the index corresponding to priority 
                // until next is the target cell. When found, set current's 
                // NextWithSamePriority property to cell's NextWithSamePriority
                // to "cut out" the target cell from the linked list.
                while (next != cell)
                {
                    current = next;
                    next = current.NextWithSamePriority;
                }
                current.NextWithSamePriority = cell.NextWithSamePriority;
            }
            // Enqueue the cell at its new priority index and decrement count by one since
            // the enqueue method increments count.
            Enqueue(cell);
            _count--;
        }

        /// <summary>
        /// Reset the priority queue.
        /// </summary>
        public void Clear()
        {
            _list.Clear();
            _count = 0;
            _minPriorityInQueue = int.MaxValue;
        }
    }
}