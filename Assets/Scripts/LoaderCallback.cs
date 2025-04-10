using UnityEngine;

public class LoaderCallback : MonoBehaviour
{
    private bool isFirstUpdate = true; // Ensures LoaderCallback runs only once

    private void Update()
    {
        if (isFirstUpdate)
        {
            isFirstUpdate = false; // Mark the first update as completed

            // Calls the Loader's callback function to transition from the LoadingScene 
            // to the target scene (e.g., GameScene).
            Loader.LoaderCallback();
        }
    }
}
