using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.UIElements.Runtime;
using UnityEngine.UIElements;
using Interactions;
using Interactions.Dialogue;
using BlueGraph;
using UnityEngine.InputSystem;

using Data;
public class UIController : MonoBehaviour
{
    public PanelRenderer dialogue_renderer;
    public VisualTreeAsset dialogue_item_template;
    private DialogueController dialogue_controller;

    public PanelRenderer inventory_renderer;
    public VisualTreeAsset inventory_item_template;
    private InventoryController inventory_controller;

    public InputAction up;
    public InputAction down;
    public InputAction confirm;

    void Awake() {
        up.started += (context) => {
            this.dialogue_controller.UpChoice();
            this.inventory_controller.UpChoice();
        };
        down.started += (context) => {
            this.dialogue_controller.DownChoice();
            this.inventory_controller.DownChoice();
        };
        confirm.started += (context) => {
            this.dialogue_controller.Confirm();
            this.inventory_controller.Confirm();
        };
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        dialogue_renderer.postUxmlReload = BindDialogueUI;
        inventory_renderer.postUxmlReload = BindInventoryUI;
        if (inputs_enabled) EnableInputs();
    }

    void OnDisable() {
        DisableInputs();
    }
    private bool inputs_enabled = false;
    void EnableInputs() {
        up.Enable();
        down.Enable();
        confirm.Enable();
        inputs_enabled = true;
    }
    void DisableInputs() {
        up.Disable();
        down.Disable();
        confirm.Disable();
        inputs_enabled = false;
    }

    private bool ui_loaded;
    public bool UIReady { get { return ui_loaded; } }

    private IEnumerable<Object> BindDialogueUI() {
        this.dialogue_controller = new DialogueController(dialogue_renderer, dialogue_item_template);
        ui_loaded = true;
        return null;
    }

    public void ShowNode(DialogueNode node, Inventory inventory, FlagSet flag_set) {
        this.dialogue_controller.Show(node, inventory, flag_set);
        EnableInputs();
    }

    private IEnumerable<Object> BindInventoryUI() {
        this.inventory_controller = new InventoryController(inventory_renderer, inventory_item_template);
        return null;
    }

    public void ShowInventory(Inventory inventory) {
        this.inventory_controller.Display(inventory);
        EnableInputs();
    }

    public bool InventorySelected(out ItemInfo info) {
        info = null;
        if (this.inventory_controller.TrySelect(out info)) {
            return true;
        }
        return false;
    }

    public InventoryController InventoryController { get => inventory_controller;  }

    public void HideInventory() {
        this.inventory_controller.Hide();
        DisableInputs();
    }

    public bool TrySelect(out InteractionNode node) {
        if (this.dialogue_controller.TrySelect(out node)) {
            DisableInputs();
            return true;
        }
        return false;
    }
}
