using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatesCounterVisual : MonoBehaviour
{
    [SerializeField] private PlatesCounter platesCounter; // Reference to the PlatesCounter to subscribe to events.
    [SerializeField] private Transform counterTopPoint;     // Parent transform for spawned plate visuals.
    [SerializeField] private Transform plateVisualPrefab;   // Prefab for the visual representation of a plate.

    // List to keep track of spawned plate visual GameObjects.
    private List<GameObject> plateVisualGameObjectList;

    private void Awake()
    {
        // Initialize the list when the script instance awakens.
        plateVisualGameObjectList = new List<GameObject>();
    }

    private void Start()
    {
        // Subscribe to plate spawn and remove events from the PlatesCounter.
        platesCounter.OnPlateSpawned += PlatesCounter_OnPlateSpawned;
        platesCounter.OnPlateRemoved += PlatesCounter_OnPlateRemoved;
    }

    private void PlatesCounter_OnPlateRemoved(object sender, System.EventArgs e)
    {
        // Remove the last spawned plate visual from the list and destroy it.
        GameObject plateGameObject = plateVisualGameObjectList[plateVisualGameObjectList.Count - 1];
        plateVisualGameObjectList.Remove(plateGameObject);
        Destroy(plateGameObject);
    }

    private void PlatesCounter_OnPlateSpawned(object sender, System.EventArgs e)
    {
        // Instantiate a new plate visual as a child of the counter top.
        Transform plateVisualTransform = Instantiate(plateVisualPrefab, counterTopPoint);

        // Offset the new plate visual based on the number already spawned.
        float plateOffsetY = 0.1f;
        plateVisualTransform.localPosition = new Vector3(0, plateOffsetY * plateVisualGameObjectList.Count, 0);

        // Add the new plate visual to the list.
        plateVisualGameObjectList.Add(plateVisualTransform.gameObject);
    }
}
