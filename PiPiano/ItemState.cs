using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiPiano
{
    /// <summary>
    /// Class to store item state
    /// </summary>
    public class ItemState
    {
        /// <summary>
        /// Min value for pressed state
        /// </summary>
        private const int MinPressedValue = 75;

        /// <summary>
        /// Number of values to consider valid state
        /// </summary>
        private const int ValueStackSize = 5;

        /// <summary>
        /// Is pressed state
        /// </summary>
        public bool IsPressed { get; set; }

        /// <summary>
        /// List of last measured values
        /// </summary>
        public List<int> LastValues { get; set; }

        /// <summary>
        /// Register value and update pressed state
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool UpdateState(int value)
        {
            UpdateStack(value);
            bool newState = CalculatePressed();
            IsPressed = newState;
            return newState;
        }

        /// <summary>
        /// Register measured value in stack
        /// </summary>
        /// <param name="value"></param>
        private void UpdateStack(int value)
        {
            if (LastValues == null)
                LastValues = new List<int>();

            LastValues.Add(value);

            if (LastValues.Count > ValueStackSize)
                LastValues.RemoveAt(0);
        }

        /// <summary>
        /// Calculate pressed state based on set of values
        /// </summary>
        /// <returns></returns>
        private bool CalculatePressed()
        {
            if(IsPressed)
            {
                foreach (var value in LastValues)
                    if (value > MinPressedValue)
                        return true;
                return false;
            }
            else
            {
                foreach (var value in LastValues)
                    if (value <= MinPressedValue)
                        return false;
                return LastValues.Count >= ValueStackSize;
            }
        }
    }
}
