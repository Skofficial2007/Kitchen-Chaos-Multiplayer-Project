using Unity.Netcode.Components;
using UnityEngine;

// This script syncs player movements across all instances in a multiplayer game using client authority.
// It allows each player to control their own movement while still syncing it with the server.

namespace Unity.Multiplayer.Samples.Utilities.ClientAuthority
{
    /// <summary>
    /// A network transform component that allows client-side movement synchronization.
    /// - Used for syncing transform updates made by clients (including the host).
    /// - Does NOT support a pure server-authoritative setup (where only the server dictates movement).
    /// - If you need full server authority over transforms, use NetworkTransform instead.
    /// 
    /// WARNING: This method trusts clients to update their own transforms, which can be a security risk.
    ///   Be cautious when using this for security-sensitive mechanics (e.g., physics-based interactions, cheating prevention).
    /// </summary>
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        /// <summary>
        /// Determines who has authority over transform updates.
        /// - Returns false to indicate that the client (owner) is responsible for updating this transform.
        /// - If true, only the server would have the authority to update movement.
        /// </summary>
        protected override bool OnIsServerAuthoritative()
        {
            return false; // Client has authority over movement, not the server.
        }
    }
}
