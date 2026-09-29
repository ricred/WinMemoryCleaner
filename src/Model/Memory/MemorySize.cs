namespace WinMemoryCleaner
{
    /// <summary>  
    /// Memory Size  
    /// </summary>  
    public class MemorySize : ObservableObject
    {
        #region Fields

        private long _bytes;
        private int _percentage;
        private Enums.Memory.Unit _unit;
        private double _value;

        #endregion

        /// <summary>  
        /// Initializes a new instance of the <see cref="MemorySize" /> class.  
        /// </summary>  
        /// <param name="bytes">The amount of memory, in bytes</param>  
        public MemorySize(long bytes)
        {
            Update(bytes);
        }

        #region Properties

        /// <summary>  
        /// Gets the value in bytes.  
        /// </summary>  
        /// <value>  
        /// The value in bytes.  
        /// </value>
        public long Bytes
        {
            get { return _bytes; }
        }

        /// <summary>  
        /// Gets or sets the percentage.  
        /// </summary>  
        /// <value>  
        /// The percentage.  
        /// </value>
        public int Percentage
        {
            get { return _percentage; }
            set
            {
                if (_percentage == value)
                    return;

                _percentage = value;

                RaisePropertyChanged("Percentage");
            }
        }

        /// <summary>  
        /// Gets the unit.  
        /// </summary>  
        /// <value>  
        /// The unit.  
        /// </value>  
        public Enums.Memory.Unit Unit
        {
            get { return _unit; }
        }

        /// <summary>  
        /// Gets the unit value.  
        /// </summary>  
        /// <value>  
        /// The unit value.  
        /// </value>  
        public double Value
        {
            get { return _value; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Updates the size in place, raising change notifications only when a value actually changed.
        /// This avoids per-tick allocations and redundant UI work when monitoring memory.
        /// </summary>
        /// <param name="bytes">The amount of memory, in bytes</param>
        internal void Update(long bytes)
        {
            if (_bytes == bytes)
                return;

            _bytes = bytes;

            var memory = bytes.ToMemoryUnit();

            if (_value != memory.Key)
            {
                _value = memory.Key;
                RaisePropertyChanged("Value");
            }

            if (_unit != memory.Value)
            {
                _unit = memory.Value;
                RaisePropertyChanged("Unit");
            }
        }

        #endregion

        /// <summary>  
        /// Converts to string.  
        /// </summary>  
        /// <returns>  
        /// A <see cref="string" /> that represents this instance.  
        /// </returns>  
        public override string ToString()
        {
            return string.Format(Localizer.Culture, "{0:0.#} {1} ({2}%)", Value, Unit, Percentage);
        }
    }
}
