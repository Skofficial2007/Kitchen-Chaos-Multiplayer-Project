using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Player implements IKitchenObjectParent because players can pick up and carry KitchenObjects.
// This class handles both local input and multiplayer synchronization (via NetworkBehaviour).
public class Player : NetworkBehaviour, IKitchenObjectParent
{
    // Event triggered whenever any player spawns in the game.
    public static event EventHandler OnAnyPlayerSpawned;

    // Event triggered when any player picks up an object (used for sound effects or other feedback).
    public static event EventHandler OnAnyPickedSomething;

    // Singleton Pattern - Ensures there's a quick way to reference the local player's instance.
    public static Player LocalInstance { get; private set; }

    // Clears static events to avoid memory leaks (especially important during scene changes).
    public static void ResetStaticData()
    {
        OnAnyPlayerSpawned = null;
        OnAnyPickedSomething = null;
    }

    // Event for when this specific player picks up something.
    public event EventHandler OnPickedSomething;

    // Event triggered when the selected counter (e.g., work station) changes.
    public event EventHandler<OnSelectedCounterChangedEventArgs> OnSelectedCounterChanged;

    // Custom event arguments that include the currently selected counter.
    public class OnSelectedCounterChangedEventArgs : EventArgs
    {
        public BaseCounter selectedCounter; // The currently selected counter.
    }

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 7f;

    [Header("LayerMask Settings")]
    [SerializeField] private LayerMask countersLayerMask; // Defines which objects the player can interact with.
    [SerializeField] private LayerMask collisionsLayerMask; // Defines which objects the player can collide with.

    [Header("Position")]
    [SerializeField] private Transform kitchenObjectHoldPoint; // The location where the KitchenObject is held when picked up.
    [SerializeField] private List<Vector3> spawnPositionList; // List of possible spawn positions where we can spawn players in multiplayer

    [Header("Visual")]
    [SerializeField] private PlayerVisual playerVisual;

    // Reference to the KitchenObject the player is currently holding.
    private KitchenObject kitchenObject;

    private bool isWalking; // Tracks if the player is currently moving.
    private Vector3 lastInteractDir; // Stores the last direction the player moved/interacted.
    private BaseCounter selectedCounter; // The current counter the player is interacting with.

    private void Start()
    {
        // Subscribe to input events from GameInput.
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
        GameInput.Instance.OnInteractAlternateAction += GameInput_OnInteractAlternateAction;

        // Set player color
        PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromClientId(OwnerClientId);
        playerVisual.SetPlayerColor(KitchenGameMultiplayer.Instance.GetPlayerColor(playerData.colorId));
    }

    // When the network object is spawned, assign the LocalInstance if this player is the owner.
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LocalInstance = this;
        }

        // Notify all listeners that a player has spawned and spawn it at a position from spawnPositionList
        transform.position = spawnPositionList[KitchenGameMultiplayer.Instance.GetPlayerDataIndexFromClientId(OwnerClientId)];
        OnAnyPlayerSpawned?.Invoke(this, EventArgs.Empty);

        // This will run on both server and all the clients. When any client disconnects, the server will handle cleanup.
        if (IsServer)
        {
            // Subscribe to disconnect callback to handle cleanup when a player disconnects.
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
        }
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        // When a player disconnects, if the disconnecting client is this player and they are holding a KitchenObject,
        // destroy it to avoid orphaned objects in the game.
        if (clientId == OwnerClientId && HasKitchenObject())
        {
            KitchenObject.DestroyKitchenObject(GetKitchenObject());
        }
    }

    // Handles alternate interactions (e.g., secondary actions) with counters.
    private void GameInput_OnInteractAlternateAction(object sender, EventArgs e)
    {
        // Check if the game is in a playing state before processing the interaction.
        if (!KitchenGameManager.Instance.IsGamePlaying()) return;

        if (selectedCounter != null)
        {
            selectedCounter.InteractAlternate(this);
        }
    }

    // Handles primary interactions with counters.
    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        if (!KitchenGameManager.Instance.IsGamePlaying()) return;

        if (selectedCounter != null)
        {
            selectedCounter.Interact(this);
        }
    }

    private void Update()
    {
        // Only process movement and interactions for the local (owned) player in multiplayer.
        if (!IsOwner)
        {
            return;
        }

        HandleMovement();
        HandleInteractions();
    }

    public bool IsWalking()
    {
        return isWalking;
    }

    // Handles player movement, including collision detection and smooth rotation.
    private void HandleMovement()
    {
        Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();
        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        float moveDistance = moveSpeed * Time.deltaTime;
        float playerRadius = 0.7f; // Approximate radius for collision detection.
        // Check if the player can move in the intended direction using a box cast.
        bool canMove = !Physics.BoxCast(transform.position, Vector3.one * playerRadius, moveDir, Quaternion.identity, moveDistance, collisionsLayerMask);

        if (!canMove) // Handle cases where direct movement is blocked.
        {
            // Attempt movement only along the X axis.
            Vector3 moveDirX = new Vector3(moveDir.x, 0f, 0f).normalized;
            canMove = moveDir.x != 0 && !Physics.BoxCast(transform.position, Vector3.one * playerRadius, moveDirX, Quaternion.identity, moveDistance, collisionsLayerMask);

            if (canMove)
            {
                moveDir = moveDirX;
            }
            else
            {
                // If X movement isn't possible, attempt only along the Z axis.
                Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
                canMove = moveDir.z != 0 && !Physics.BoxCast(transform.position, Vector3.one * playerRadius, moveDirZ, Quaternion.identity, moveDistance, collisionsLayerMask);

                if (canMove)
                {
                    moveDir = moveDirZ;
                }
                // Otherwise, the player cannot move.
            }
        }

        if (canMove)
        {
            // Apply movement to the player's position.
            transform.position += moveDir * moveDistance;
        }

        // Update walking status for animations or other logic.
        isWalking = moveDir != Vector3.zero;

        // Smoothly rotate the player to face the movement direction.
        float rotateSpeed = 10f;
        transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
    }

    // Handles player interactions with nearby counters using raycasting.
    private void HandleInteractions()
    {
        Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();
        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        // Update the last known interaction direction when the player moves.
        if (moveDir != Vector3.zero)
        {
            lastInteractDir = moveDir;
        }

        float interactDistance = 2f; // Maximum distance for an interaction.

        // Cast a ray from the player's position in the direction they last moved.
        if (Physics.Raycast(transform.position, lastInteractDir, out RaycastHit raycastHit, interactDistance, countersLayerMask))
        {
            // Check if the object hit by the raycast has a BaseCounter component.
            if (raycastHit.transform.TryGetComponent(out BaseCounter baseCounter))
            {
                if (baseCounter != selectedCounter) // Only update if the counter has changed.
                {
                    SetSelectedCounter(baseCounter);
                }
            }
            else
            {
                SetSelectedCounter(null);
            }
        }
        else
        {
            SetSelectedCounter(null);
        }
    }

    // Sets the currently selected counter and notifies any listeners of the change.
    private void SetSelectedCounter(BaseCounter selectedCounter)
    {
        this.selectedCounter = selectedCounter;
        OnSelectedCounterChanged?.Invoke(this, new OnSelectedCounterChangedEventArgs
        {
            selectedCounter = selectedCounter
        });
    }

    // Returns the transform where a KitchenObject should be held.
    public Transform GetKitchenObjectFollowTransform()
    {
        return kitchenObjectHoldPoint;
    }

    // Assigns a KitchenObject to the player and triggers the appropriate events.
    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        this.kitchenObject = kitchenObject;

        // If a KitchenObject is assigned, trigger the pickup events (e.g., for sound or UI feedback).
        if (kitchenObject != null)
        {
            OnPickedSomething?.Invoke(this, EventArgs.Empty);
            OnAnyPickedSomething?.Invoke(this, EventArgs.Empty);
        }
    }

    // Returns the current KitchenObject the player is holding.
    public KitchenObject GetKitchenObject()
    {
        return kitchenObject;
    }

    // Clears the current KitchenObject reference.
    public void ClearKitchenObject()
    {
        kitchenObject = null;
    }

    // Checks if the player is holding a KitchenObject.
    public bool HasKitchenObject()
    {
        return (kitchenObject != null);
    }

    // Returns the NetworkObject component for networking purposes.
    public NetworkObject GetNetworkObject()
    {
        return NetworkObject;
    }
}
