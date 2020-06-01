using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BlueGraph;

using Data;

namespace Interactions.Dialogue
{
    [System.Serializable]
    public class RecipeChoice : Interactions.Interaction {
        public FoodInfo when_making;
    }

    [Node("Make Recipe", module = "Items", help = "Show dialogue for making a recipe. Will show all learned recipes. Displays error if you don't have the ingredients.")]
    public class MakeRecipeNode : DialogueNode
    {
        [Output(list=true, list_type=typeof(RecipeChoice))] public List<RecipeChoice> on_recipe_chosen = new List<RecipeChoice>();
        [Output("default", multiple = false)] public Interaction default_chosen;
        [Output("cancel", multiple = false)] public Interaction cancel;
        public InteractionNode GetNextNodeForChoiceAndMake(Inventory inventory, RecipeInfo recipe) {
            var port_name = "cancel";
            if (recipe != null) {
                port_name = "default";
                ItemInfo choosen_output = recipe.output.info;
                for (int i = 0; i < on_recipe_chosen.Count; i++) {
                    if (on_recipe_chosen[i].when_making == choosen_output) {
                        port_name = $"on_recipe_chosen[{i}]";
                    }
                }
                inventory.make(recipe);
            }
            var port = GetPort(port_name);
			if (port == null) return null;
			if (!port.IsConnected) return null;
			return port.ConnectedPorts[0].node as InteractionNode;
        }

        public override bool HasOutput() { return false; }
    }
}