using UnityEngine;

// NOTE: Static events and static classes persist across scene changes.
// However, when starting a new game, we don't want leftover event listeners from a previous game.
// If not handled properly, they could lead to unexpected behavior or memory leaks.
// To prevent this, we use a dedicated class to reset all static classes and events at the beginning of a new game.
public class ResetStaticDataManager : MonoBehaviour
{
    private void Awake()
    {
        // Reset static data to clear any lingering event listeners from the previous game session.
        CuttingCounter.ResetStaticData();
        BaseCounter.ResetStaticData();
        TrashCounter.ResetStaticData();
        Player.ResetStaticData();
    }
}
