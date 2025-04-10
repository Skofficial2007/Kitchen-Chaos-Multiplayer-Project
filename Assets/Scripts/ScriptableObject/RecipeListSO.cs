using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ScriptableObject to store a list of available recipes.
// [CreateAssetMenu()] is commented out because we only need one instance of RecipeListSO.
public class RecipeListSO : ScriptableObject
{
    public List<RecipeSO> recipeSOList; // List of all recipes in the game.
}
