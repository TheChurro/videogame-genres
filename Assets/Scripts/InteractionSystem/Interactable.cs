using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Data;

public interface Interactable
{
    IEnumerator<bool> run_interaction(Inventory inventory, FlagSet flags, UIController controller);
}
