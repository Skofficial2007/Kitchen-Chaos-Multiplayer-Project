using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This ScriptableObject is used to define a recipe consisting of multiple Kitchen Objects.
[CreateAssetMenu()]
public class RecipeSO : ScriptableObject
{
    public List<KitchenObjectSO> kitchenObjectSOList;
    public string recipeName;
}
