using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BlueGraph;

using Data;

namespace Interactions.Dialogue
{

    [Node("Take Item", module = "Items", help = "Try to remove an item from the player.")]
    public class TakeItemNode : InteractionNode
    {
        [Editable] public ItemInfo item;
        [Editable] public int amount;
        [Output("failed", multiple = false)] public Interaction failed;
        public InteractionNode TryTakeItem(Inventory inventory) {
            var port_name = "failed";
            if (inventory.try_remove(item, amount, out _)) {
                port_name = "next";
            }
            var port = GetPort(port_name);
			if (port == null) return null;
			if (!port.IsConnected) return null;
			return port.ConnectedPorts[0].node as InteractionNode;
        }
    }
}