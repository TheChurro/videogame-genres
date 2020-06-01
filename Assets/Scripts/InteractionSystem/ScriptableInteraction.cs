using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Interactions;
using Data;
namespace Interactions {
    public class ScriptableInteraction : InteractionsGraph, Interactable
    {
        InteractionEntryNode entry;
        public void Start() {
            var entries = this.GetEntryPoints();
            if (entries != null && entries.Count > 0) {
                entry = entries[0];
            }
        }
        public IEnumerable<bool> run_interaction(Inventory inventory, FlagSet flags, UIController controller) {
            if (entry == null) yield break;
            if (!this.StartInteraction(entry, inventory, flags, controller, out var iteration)) {
                yield return true;
                while (!this.FollowInteraction(ref iteration)) {
                    yield return true;
                }
            }
            yield break;
        }
    }
}