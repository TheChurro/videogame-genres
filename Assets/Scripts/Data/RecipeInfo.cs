using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Data {
    [CreateAssetMenu(menuName = "Items/Recipe")]
	public class RecipeInfo : ScriptableObject {
        public ReicpeRequirement output;
        public ReicpeRequirement[] inputs;
        public bool known = true;
        [System.NonSerialized] public bool available;
	}

    [System.Serializable]
    public class ReicpeRequirement {
        public ItemInfo info;
        public int amount = 1;
    }

    public static class RecipeState {
        static List<RecipeInfo> recipes;
        public static List<RecipeInfo> all_recipes() {
            if (recipes == null) {
                recipes = new List<RecipeInfo>(Resources.LoadAll<RecipeInfo>("Recipes"));
                recipes.ForEach(x => x.available = x.known);
            }
            return recipes;
        }
        public static List<RecipeInfo> available_recipies() {
            if (recipes == null) {
                recipes = new List<RecipeInfo>(Resources.LoadAll<RecipeInfo>("Recipes"));
                recipes.ForEach(x => x.available = x.known);
            }
            return recipes.FindAll((recipe) => recipe.available);
        }

        public class RecipeStateFlagReciever : FlagUpdateReciever {
            private FlagSet flag_system;
            public RecipeStateFlagReciever(FlagSet flag_system) {
                this.flag_system = flag_system;
            }
            public void flag_set(string flag) {
                if (flag.StartsWith("learn:")) {
                    var to_learn = flag.Substring(6);
                    foreach (var recipe in all_recipes()) {
                        if (recipe.output.info.name == to_learn) {
                            Debug.Log($"Recipe Learned: {to_learn}");
                            recipe.available = true;
                            flag_system[$"learned:{to_learn}"] = true;
                            break;
                        }
                    }
                }
            }

            public void flag_unset(string flag) {}
        }
    }
}