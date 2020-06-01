using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Data;

namespace Task {
    public class RequirementTree {
        Dictionary<ItemInfo, ItemRequirements> requirement_tree;
        ItemInfo base_requirement;
        public RequirementTree(ItemInfo item) {
            this.base_requirement = item;
            this.requirement_tree = new Dictionary<ItemInfo, ItemRequirements>();

            var requirements_queue = new Queue<ItemInfo>();
            requirements_queue.Enqueue(base_requirement);
            while (requirements_queue.Count > 0) {
                var req = requirements_queue.Dequeue();
                if (requirement_tree.ContainsKey(req)) {
                    continue;
                }
                var req_entry = new ItemRequirements(req);
                requirement_tree[req] = req_entry;
                if (req_entry.recipe != null) {
                    foreach (var recipe_input in req_entry.recipe.inputs) {
                        requirements_queue.Enqueue(recipe_input.info);
                    }
                }
            }
        }

        public HashSet<ItemRequirements> possibly_needed(Inventory inventory, int to_make) {
            inventory = inventory.clone();
            var needed_items = new HashSet<ItemRequirements>();
            var queued_items = new Queue<(ItemInfo, int)>();
            queued_items.Enqueue((base_requirement, to_make));
            while (queued_items.Count > 0) {
                var next = queued_items.Dequeue();
                var next_item = next.Item1;
                var next_needed = next.Item2;
                var containing = Mathf.Min(next_needed, inventory[next.Item1]);
                inventory.try_remove(next_item, containing, out _);
                next_needed -= containing;
                if (next_needed > 0) {
                    var requirement = requirement_tree[next_item];
                    needed_items.Add(requirement);
                    if (requirement.recipe != null) {
                        to_make = Mathf.CeilToInt(next_needed / (float)requirement.recipe.output.amount);
                        foreach (var input in requirement.recipe.inputs) {
                            queued_items.Enqueue((input.info, input.amount * to_make));
                        }
                    }
                }
            }
            return needed_items;
        }

        public class ItemRequirements {
            public ItemRequirements(ItemInfo item) {
                this.base_item = item;
                this.providers = ResourceManager.Instance.those_providing(item).ToList();
                this.recipe = RecipeState.available_recipies()
                                            .Find(x => x.output.info == item);
            }
            public ItemInfo base_item;
            public List<ResourceProvider> providers;
            public RecipeInfo recipe;
        }
    }
}