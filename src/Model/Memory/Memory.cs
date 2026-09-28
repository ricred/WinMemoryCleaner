using System;

namespace WinMemoryCleaner
{
    /// <summary>
    /// Memory (RAM)
    /// </summary>
    public class Memory : ObservableObject
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Memory" /> class.
        /// </summary>
        public Memory()
        {
            Physical = new MemoryStats(0, 0, 0);
            Virtual = new MemoryStats(0, 0, 0);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Memory" /> class.
        /// </summary>
        /// <param name="memoryStatusEx">The memory status ex.</param>
        public Memory(Structs.Windows.MemoryStatusEx memoryStatusEx)
        {
            if (memoryStatusEx == null)
                throw new ArgumentNullException("memoryStatusEx");

            Physical = new MemoryStats(memoryStatusEx.AvailPhys, memoryStatusEx.TotalPhys, memoryStatusEx.MemoryLoad);
            Virtual = new MemoryStats(memoryStatusEx.AvailPageFile, memoryStatusEx.TotalPageFile);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Physical
        /// </summary>
        public MemoryStats Physical { get; private set; }

        /// <summary>
        /// Virtual
        /// </summary>
        public MemoryStats Virtual { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Updates the memory in place, raising change notifications only when a value actually changed.
        /// This avoids per-tick allocations when monitoring memory.
        /// </summary>
        /// <param name="memoryStatusEx">The memory status ex.</param>
        internal void Update(Structs.Windows.MemoryStatusEx memoryStatusEx)
        {
            if (memoryStatusEx == null)
                throw new ArgumentNullException("memoryStatusEx");

            Physical.Update(memoryStatusEx.AvailPhys, memoryStatusEx.TotalPhys, memoryStatusEx.MemoryLoad);
            Virtual.Update(memoryStatusEx.AvailPageFile, memoryStatusEx.TotalPageFile);
        }

        #endregion
    }
}
