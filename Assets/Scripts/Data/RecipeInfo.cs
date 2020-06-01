using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Data {
    [CreateAssetMenu(menuName = "Items/Recipe")]
	public class RecipeInfo : ScriptableObject {
        public ReicpeRequirement output;
        public ReicpeRequirement[] inputs;
        public bool known = true;
	}

    [System.Serializable]
    public class ReicpeRequirement {
        public ItemInfo info;
        public int amount = 1;
    }

    public static class RecipeState {
        static List<RecipeInfo> recipes;
        public static List<RecipeInfo> available_recipies() {
            if (recipes == null) {
                recipes = new List<RecipeInfo>(Resources.LoadAll<RecipeInfo>("Recipes"));
            }
            return recipes.FindAll((recipe) => recipe.known);
        }
    }
}