using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Data {
    public class Inventory {
        public PlayerController player;
        public Inventory(PlayerController player) {
            this.player = player;
        }
        public Dictionary<ItemInfo, int> item_map = new Dictionary<ItemInfo, int>();

        public Inventory clone() {
            Inventory out_inv = new Inventory(null);
            foreach (var kvp in this.item_map) {
                out_inv.item_map[kvp.Key] = kvp.Value;
            } 
            return out_inv;
        }

        public List<(ItemInfo, int)> items() {
            var item_list = this.item_map.Keys.Select(key => (key, this.item_map[key])).ToList();
            item_list.Sort((a, b) => a.Item1.name.CompareTo(b.Item1.name));
            return item_list;
        }

        public List<(FoodInfo, int)> food() {
            return this.items()
                        .OfType<FoodInfo>()
                        .Select(x => ((FoodInfo)x, this.item_map[x]))
                        .ToList();
        }

        public bool can_make(RecipeInfo recipe) {
            if (recipe.output.info == null) return false;
            foreach (var entry in recipe.inputs) {
                if (this.item_map.TryGetValue(entry.info, out int value)) {
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

        public void add(ItemInfo item, int amount) {
            if (!this.item_map.TryGetValue(item, out int current)) {
                current = 0;
            }
            this.item_map[item] = current + amount;
        }

        public bool try_remove(ItemInfo item, int amount, out int new_amount) {
            new_amount = 0;
            if (this.item_map.TryGetValue(item, out int current)) {
                if (current > amount) {
                    this.item_map[item] = current - amount;
                    new_amount = current - amount;
                    return true;
                } else if (current == amount) {
                    this.item_map.Remove(item);
                    new_amount = 0;
                    return true;
                }
                new_amount = current;
            }
            return false;
        }

        public int this[ItemInfo item] {
            get {
                if (this.item_map.TryGetValue(item, out int val)) {
                    return val;
                }
                return 0;
            }
        }
    }
}
