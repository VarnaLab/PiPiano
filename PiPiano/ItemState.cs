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
        private const int MinPressedValue = 140;
        private const int ValueStackSize = 3;

        public bool IsPressed { get; set; }

        public List<int> LastValues { get; set; }

        public bool UpdateState(int value)
        {
            UpdateStack(value);
            bool newState = CalculatePressed();
            IsPressed = newState;
            return newState;
        }

        private void UpdateStack(int value)
        {
            if (LastValues == null)
                LastValues = new List<int>();

            LastValues.Add(value);

            if (LastValues.Count > ValueStackSize)
                LastValues.RemoveAt(0);
        }

        private bool CalculatePressed()
        {
            foreach (var value in LastValues)
                if (value <= MinPressedValue)
                    return false;
            return LastValues.Count >= ValueStackSize;
        }
    }
}
