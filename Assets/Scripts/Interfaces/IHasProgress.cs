using System;
using UnityEngine;

// Interface to handle progress tracking for UI updates (e.g., Cutting, Frying, Burning).
public interface IHasProgress
{
    // Event triggered whenever progress changes (used to update UI elements like a progress bar).
    public event EventHandler<OnProgressChangedEventArgs> OnProgressChanged;

    // Custom event arguments to store normalized progress values (0 to 1).
    public class OnProgressChangedEventArgs : EventArgs
    {
        public float progressNormalized; // Represents the current progress as a fraction.
    }
}
