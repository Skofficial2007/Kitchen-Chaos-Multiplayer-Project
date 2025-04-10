using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class KitchenGameManager : NetworkBehaviour
{
    public static KitchenGameManager Instance { get; private set; }

    // Event triggered when the game state changes (e.g., from WaitingToStart to CountdownToStart, etc.)
    public event EventHandler OnStateChange;
    // Event triggered when the local game is paused
    public event EventHandler OnLocalGamePause;
    // Event triggered when the local game is unpaused
    public event EventHandler OnLocalGameUnpause;
    // Events triggered when the multiplayer game pause state changes (affecting all clients)
    public event EventHandler OnMultiplayerGamePaused;
    public event EventHandler OnMultiplayerGameUnpaused;
    // Event triggered when the local player's ready state changes (used for game joining)
    public event EventHandler OnLocalPlayerReadyChanged;

    [SerializeField] private Transform playerPrefab;

    // Enum representing different game states
    private enum State
    {
        WaitingToStart,
        CountdownToStart,
        GamePlaying,
        GameOver
    }

    // NetworkVariables are to keep them synced between each client
    private NetworkVariable<State> state = new NetworkVariable<State>(State.WaitingToStart); // Current game state
    private NetworkVariable<float> countdownToStartTimer = new NetworkVariable<float>(3f); // Countdown before game begins // DEBUG SET TO 1f FOR TESTING
    private NetworkVariable<float> gamePlayingTimer = new NetworkVariable<float>(0f); // Game Playing time
    private NetworkVariable<bool> isGamePaused = new NetworkVariable<bool>(false); // Synchronized game pause state

    private float gamePlayingTimerMax = 90f; // Max game duration before it ends
    private bool isLocalGamePaused = false; // Local pause toggle (may differ from network state)
    private bool isLocalPlayerReady; // Local flag indicating whether the player has signaled they're ready
    private bool autoTestGamePausedState; // When a player disconnects while paused, we need to wait a frame before updating the pause state

    // Dictionary to track each player's "ready" status for joining the game (multiplayer sync)
    private Dictionary<ulong, bool> playerReadyDictionary;
    // Dictionary to track each player's pause state for syncing game pause across clients
    private Dictionary<ulong, bool> playerPausedDictionary; 

    private void Awake()
    {
        // Ensure only one instance of KitchenGameManager exists
        if (Instance != null)
        {
            Debug.LogError("There is more than one KitchenGameManager Instance");
        }
        Instance = this;

        // Initialize dictionaries for tracking player readiness and pause status (used for multiplayer sync)
        playerReadyDictionary = new Dictionary<ulong, bool>();
        playerPausedDictionary = new Dictionary<ulong, bool>();
    }

    private void Start()
    {
        // Subscribe to input events for pausing and interacting (e.g., to join the game)
        GameInput.Instance.OnPauseAction += GameInput_OnPauseAction;
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
    }

    public override void OnNetworkSpawn()
    {
        // Listen for changes to the networked game state and game pause flag
        state.OnValueChanged += State_OnValueChanged;
        isGamePaused.OnValueChanged += IsGamePaused_OnValueChanged;

        if (IsServer)
        {
            // Subscribe to disconnect callback to handle cleanup and update game state when a client disconnects.
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
        }
    }

    private void SceneManager_OnLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Transform playerTransform = Instantiate(playerPrefab);
            playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        // When a client disconnects, remove them from the multiplayer dictionaries to prevent stale data.
        if (playerReadyDictionary.ContainsKey(clientId))
        {
            playerReadyDictionary.Remove(clientId);
        }

        if (playerPausedDictionary.ContainsKey(clientId))
        {
            playerPausedDictionary.Remove(clientId);
        }

        // Set a flag to re-test the overall game pause state after a disconnect,
        // ensuring that if a paused player leaves, the game state is updated correctly.
        autoTestGamePausedState = true;
    }

    private void IsGamePaused_OnValueChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            Time.timeScale = 0f;
            OnMultiplayerGamePaused?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Time.timeScale = 1f;
            // Reset the local pause flag when the game resumes.
            isLocalGamePaused = false;
            OnMultiplayerGameUnpaused?.Invoke(this, EventArgs.Empty);
        }
    }

    private void State_OnValueChanged(State previousValue, State newValue)
    {
        // When the game state changes, notify any subscribers (e.g., UI updates).
        OnStateChange?.Invoke(this, EventArgs.Empty);
    }

    private void GameInput_OnInteractAction(object sender, EventArgs e)
    {
        // When a player presses the interact button during the WaitingToStart state, mark them as ready.
        if (state.Value == State.WaitingToStart)
        {
            isLocalPlayerReady = true;
            OnLocalPlayerReadyChanged?.Invoke(this, EventArgs.Empty);

            // Call server-side logic to update the player's ready status across the network.
            SetPlayerReadyServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // When a player presses interact, mark the corresponding client as ready in the server dictionary.
        playerReadyDictionary[serverRpcParams.Receive.SenderClientId] = true;

        bool allClientsReady = true;

        // Check if every connected client is ready.
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!playerReadyDictionary.ContainsKey(clientId) || !playerReadyDictionary[clientId])
            {
                // The player is not ready.
                allClientsReady = false;
                break;
            }
        }

        // If every player is ready, transition the game state to start the countdown.
        if (allClientsReady)
        {
            state.Value = State.CountdownToStart;
        }
    }

    private void GameInput_OnPauseAction(object sender, EventArgs e)
    {
        // When the pause action is triggered by the local player, toggle the pause state.
        TogglePauseGame();
    }

    private void Update()
    {
        // Only the server should drive the game state progression.
        if (!IsServer)
        {
            return;
        }

        switch (state.Value)
        {
            case State.WaitingToStart:
                // Waiting for all players to signal readiness.
                break;

            case State.CountdownToStart:
                // Countdown before the game starts.
                countdownToStartTimer.Value -= Time.deltaTime;
                if (countdownToStartTimer.Value < 0f)
                {
                    state.Value = State.GamePlaying;
                    gamePlayingTimer.Value = gamePlayingTimerMax;
                }
                break;

            case State.GamePlaying:
                // Decrease game playing time and check for game over.
                gamePlayingTimer.Value -= Time.deltaTime;
                if (gamePlayingTimer.Value < 0f)
                {
                    state.Value = State.GameOver;
                }
                break;

            case State.GameOver:
                // Game is over, no further updates needed.
                break;
        }
    }

    private void LateUpdate()
    {
        // If a disconnect occurred that may affect the pause state, re-test the overall game pause state.
        if (autoTestGamePausedState)
        {
            autoTestGamePausedState = false;
            TestGamePausedState();
        }
    }

    public bool IsLocalPlayerReady()
    {
        return isLocalPlayerReady;
    }

    // Check if the game is currently in the playing state.
    public bool IsGamePlaying()
    {
        return state.Value == State.GamePlaying;
    }

    public bool IsLocalGamePaused()
    {
        return isLocalGamePaused;
    }

    // Check if the countdown to start is active.
    public bool IsCountdownToStartActive()
    {
        return state.Value == State.CountdownToStart;
    }

    // Return the remaining countdown time before the game starts.
    public float GetCountdownToStartTimer()
    {
        return countdownToStartTimer.Value;
    }

    // Check if the game is over.
    public bool IsGameOver()
    {
        return state.Value == State.GameOver;
    }

    // Check if the game is in waiting state
    public bool IsWaitingToStart()
    {
        return state.Value == State.WaitingToStart;
    }

    public float GetGamePlayingTimerNormalized()
    {
        // Returning the reverse of game playing time because in our code we are counting down.
        return 1 - (gamePlayingTimer.Value / gamePlayingTimerMax);
    }

    // Pause Game: toggles the local pause state and synchronizes it across the network.
    public void TogglePauseGame()
    {
        isLocalGamePaused = !isLocalGamePaused;

        if (isLocalGamePaused)
        {
            // Notify the server that this client is pausing the game.
            PauseGameServerRpc();
            OnLocalGamePause?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            // Notify the server that this client is unpausing the game.
            UnpauseGameServerRpc();
            OnLocalGameUnpause?.Invoke(this, EventArgs.Empty);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PauseGameServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // Update the player's pause status to true on the server.
        playerPausedDictionary[serverRpcParams.Receive.SenderClientId] = true;
        // Check if any player is paused; if so, the game should remain paused across the network.
        TestGamePausedState();
    }

    [ServerRpc(RequireOwnership = false)]
    private void UnpauseGameServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // Update the player's pause status to false on the server.
        playerPausedDictionary[serverRpcParams.Receive.SenderClientId] = false;
        // Reevaluate the overall pause state across all clients.
        TestGamePausedState();
    }

    private void TestGamePausedState()
    {
        // Iterate through all connected clients to determine if any are paused.
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (playerPausedDictionary.ContainsKey(clientId) && playerPausedDictionary[clientId])
            {
                // If any player is paused, set the networked pause flag to true and exit.
                isGamePaused.Value = true;
                return;
            }
        }

        // If no players are paused, set the networked pause flag to false.
        isGamePaused.Value = false;
    }
}
