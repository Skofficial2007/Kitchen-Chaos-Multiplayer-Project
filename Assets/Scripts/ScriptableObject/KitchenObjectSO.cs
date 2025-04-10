using UnityEngine;

// Scriptable object to store Kitchen Object data like prefab, icon and name
[CreateAssetMenu()]
public class KitchenObjectSO : ScriptableObject
{
    public Transform prefab;
    public Sprite sprite;
    public string objectName;
}
