using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Static class to handle scene transitions while preserving data between scenes
// This is useful because normally, when changing scenes, all objects are destroyed
public static class Loader
{
    // Enum to define the available scenes in the game.
    // Using an enum improves readability and reduces the risk of typos when referring to scene names.
    public enum Scene
    {
        MainMenuScene, // Main menu of the game
        GameScene,     // The actual gameplay scene
        LoadingScene,  // An intermediary loading scene used during transitions
        LobbyScene,
        CharacterSelectScene
    }

    // Stores the scene that should be loaded after the LoadingScene finishes
    public static Scene targetScene;

    /// <summary>
    /// Initiates the scene loading process.
    /// Instead of switching directly between scenes, we first transition to a loading screen.
    /// This helps in creating a smoother user experience and allows us to display animations or progress indicators.
    /// </summary>
    /// <param name="targetScene">The scene we want to load after the loading screen.</param>
    public static void Load(Scene targetScene)
    {
        // Save the target scene to be loaded later
        Loader.targetScene = targetScene;

        // Load the intermediary LoadingScene first
        SceneManager.LoadScene(Scene.LoadingScene.ToString());
    }

    /// <summary>
    /// Initiates a scene load over the network using Netcode for GameObjects' SceneManager.
    /// This ensures all connected clients transition to the specified scene simultaneously.
    /// </summary>
    /// <param name="targetScene">The network scene to load for all clients.</param>
    public static void LoadNetwork(Scene targetScene)
    {
        // Use Netcode's built-in SceneManager to load the scene in Single mode,
        // so it replaces the current scene for every connected client.
        NetworkManager.Singleton.SceneManager.LoadScene(targetScene.ToString(), LoadSceneMode.Single);
    }

    /// <summary>
    /// This function is called by LoaderCallback.cs after the first frame of the LoadingScene.
    /// It ensures that the LoadingScene gets at least one frame to render before transitioning.
    /// Without this, the transition might be too fast to notice, potentially causing a jarring experience.
    /// </summary>
    public static void LoaderCallback()
    {
        // Load the final target scene after the LoadingScene has been displayed for at least one frame
        SceneManager.LoadScene(targetScene.ToString());
    }
}