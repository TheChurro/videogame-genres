using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Data {
    [CreateAssetMenu(menuName = "Food/FoodInfo")]
	public class FoodInfo : ScriptableObject {
        public string description;
        public Texture2D image;
        public FoodEffect effect;
	}

    public class Inventory {
        public PlayerController player;
        public Inventory(PlayerController player) {
            this.player = player;
        }
        public Dictionary<FoodInfo, int> food = new Dictionary<FoodInfo, int>();

        public List<(FoodInfo, int)> items() {
            var item_list = new List<(FoodInfo, int)>();
            var keys = new List<FoodInfo>(this.food.Keys);
            keys.Sort((a, b) => a.name.CompareTo(b.name));
            foreach (var key in keys) {
                item_list.Add((key, this.food[key]));
            }
            return item_list;
        }

        public bool can_make(RecipeInfo recipe) {
            if (recipe.output.info == null) return false;
            foreach (var entry in recipe.inputs) {
                if (this.food.TryGetValue(entry.info, out int value)) {
                    if (value < entry.amount) {
                        return false;
                    }
                } else if (entry.amount > 0) {
                    return false;
                }
            }
            return true;
        }

        public bool make(RecipeInfo recipe) {
            if (!can_make(recipe)) {
                return false;
            }
            foreach (var entry in recipe.inputs) {
                this.try_remove(entry.info, entry.amount, out _);
            }
            this.add(recipe.output.info, recipe.output.amount);
            return true;
        }

        public void add(FoodInfo food, int amount) {
            if (!this.food.TryGetValue(food, out int current)) {
                current = 0;
            }
            this.food[food] = current + amount;
        }

        public bool try_remove(FoodInfo food, int amount, out int new_amount) {
            new_amount = 0;
            if (this.food.TryGetValue(food, out int current)) {
                if (current > amount) {
                    this.food[food] = current - amount;
                    new_amount = current - amount;
                    return true;
                } else if (current == amount) {
                    this.food.Remove(food);
                    new_amount = 0;
                    return true;
                }
                new_amount = current;
            }
            return false;
        }

        public int this[FoodInfo food] {
            get {
                if (this.food.TryGetValue(food, out int val)) {
                    return val;
                }
                return 0;
            }
        }
    }

    [System.Serializable]
    public enum FoodEffect {
        None,
        Speed,
        Strength,
        Growth
    }
}