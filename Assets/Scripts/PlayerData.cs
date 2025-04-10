using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// For Player data as NetworkVariable list which is a network variable that works as a list
// This will be done using this script to create custom data variable
// This will store all the data related to Player
public struct PlayerData : IEquatable<PlayerData>, INetworkSerializable
{
    public ulong clientId; // Unique identifier for the client/player.
    public int colorId;
    public FixedString64Bytes playerName;
    public FixedString64Bytes playerId;

    // Check equality based on the client ID.
    public bool Equals(PlayerData other)
    {
        return 
            clientId == other.clientId && 
            colorId == other.colorId &&
            playerName == other.playerName &&
            playerId == other.playerId;
    }

    // Serialize the player data over the network.
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // Only the clientId is serialized since it uniquely identifies a player.
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref colorId);
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref playerId);
    }
}
