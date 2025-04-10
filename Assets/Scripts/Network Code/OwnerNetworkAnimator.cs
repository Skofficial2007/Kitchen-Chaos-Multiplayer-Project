using Unity.Netcode.Components;

/// <summary>
/// A custom NetworkAnimator that allows clients to control their own animation updates.
/// - Used for syncing player movement animations across all instances.
/// - Gives animation control to the client (owner) instead of the server.
/// - Works best in a client-authoritative setup where each player controls their own character.
/// </summary>
public class OwnerNetworkAnimator : NetworkAnimator
{
    /// <summary>
    /// Determines who has authority over animation updates.
    /// - Returns false, meaning the client (owner) controls the animations instead of the server.
    /// - This ensures that animations are updated based on the player's own movement input.
    /// </summary>
    protected override bool OnIsServerAuthoritative()
    {
        return false; // Client has authority over animations, not the server.
    }
}
