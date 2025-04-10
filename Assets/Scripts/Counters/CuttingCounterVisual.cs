using System;
using UnityEngine;

// Plays the cutting animation when the player cuts an object on the counter.
public class CuttingCounterVisual : MonoBehaviour
{
    private const string CUT = "Cut"; // Animation trigger name.

    [SerializeField] private CuttingCounter cuttingCounter;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // Subscribe to the cutting event to trigger the animation.
        cuttingCounter.OnCut += CuttingCounter_OnCut;
    }

    private void CuttingCounter_OnCut(object sender, EventArgs e)
    {
        animator.SetTrigger(CUT); // Play the cutting animation.
    }
}
