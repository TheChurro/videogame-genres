using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.UIElements.Runtime;

using Interactions;
using Interactions.Dialogue;

using UnityEngine.InputSystem;

using Data;


[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public InputAction move;
    public InputAction interact;
    public InputAction inventory_action;
    private bool viewing_inventory = false;
    public InputAction quit;

    public float speed;
    private InteractionsGraph activeInteraction;
    public UIController UIController;

    private Rigidbody2D body;
    private FlagSet flags;
    private List<Interactable> interactables;
    private IEnumerator<bool> current_interaction;
    public List<FlagUpdateReciever> onFlagChangeHandlers;
    private Vector2 start_pos;

    public Inventory inventory;

    void Awake() {
        body = this.GetComponent<Rigidbody2D>();
        my_animator = this.GetComponent<Animator>();
        
        start_pos = this.transform.position;
        interact.started += OnInteract;
        interact.performed += OnEndInteracting;
        inventory_action.started += InventoryAction;
        quit.performed += (ctx) => Application.Quit();

        inventory = new Inventory(this);
    }

    public void AddFlagChangeHandler(FlagUpdateReciever graph) {
        if (onFlagChangeHandlers == null) {
            onFlagChangeHandlers = new List<FlagUpdateReciever>();
        }
        onFlagChangeHandlers.Add(graph);
    }

    private List<FlagUpdateReciever> toRemoveHandlers;
    public void RemoveFlagChangeHandler(FlagUpdateReciever graph) {
        if (toRemoveHandlers == null) {
            toRemoveHandlers = new List<FlagUpdateReciever>();
        }
        toRemoveHandlers.Add(graph);
    }

    void OnEnable() {
        move.Enable();
        interact.Enable();
        quit.Enable();
        inventory_action.Enable();
    }
    void OnDisable() {
        move.Disable();
        interact.Disable();
        quit.Disable();
        inventory_action.Disable();
    }
    // Start is called before the first frame update
    void Start()
    {
        flags = new FlagSet();
        interactables = new List<Interactable>();
        queue = new List<Interactable>();
        inventory = new Inventory(this);
    }
    void UpdateFlagChangeListeners() {
        if (onFlagChangeHandlers == null) {
            onFlagChangeHandlers = new List<FlagUpdateReciever>();
        }
        foreach (Flag f in flags.change_log) {
            foreach (FlagUpdateReciever updater in onFlagChangeHandlers) {
                if (f.Instruction == FlagInstruction.Set) {
                    updater.flag_set(f.Value);
                } else {
                    updater.flag_unset(f.Value);
                }
            }
        }
    }

    private bool swung_sword;
    void OnInteract(InputAction.CallbackContext action_context) {
        if (current_interaction != null) return;
        foreach (var interactable in interactables) {
            if (interactable == null) continue;
            if (interactable is MonoBehaviour b && b.gameObject == null) continue;
            if (current_interaction == null) {
                current_interaction = interactable.run_interaction(inventory, flags, UIController);
            } else {
                QueueInteraction(interactable);
            }
            break;
        }
    }

    void OnEndInteracting(InputAction.CallbackContext action_context) {
    }

    void InventoryAction(InputAction.CallbackContext action_context) {
        if (!this.viewing_inventory) {
            if (current_interaction == null) {
                current_interaction = this.display_inventory();
            }
        }
        this.viewing_inventory = !this.viewing_inventory;
    }

    private class OpenInventory : Interactable {
        private PlayerController player;
        public OpenInventory(PlayerController player) { this.player = player; }
        public IEnumerator<bool> run_interaction(Inventory inventory, FlagSet flags, UIController controller) {
            return player.display_inventory();
        }
    }

    protected IEnumerator<bool> display_inventory() {
        this.UIController.ShowInventory(this.inventory);
        while (this.viewing_inventory) {
            yield return true;
            if (this.UIController.InventorySelected(out FoodInfo info)) {
                if (this.inventory.try_remove(info, 1, out var new_amount)) {
                    if (new_amount > 0) {
                        this.UIController.InventoryController.UpdateText(info);
                    } else {
                        this.UIController.InventoryController.Remove(info);
                    }
                    // Apply buffs
                }
            }
        }
        this.UIController.HideInventory();
        yield break;
    }

    Animator my_animator;

    // Update is called once per frame
    void Update()
    {
        if (current_interaction != null) {
            if (!current_interaction.MoveNext()) {
                Debug.Log("Ended Iteration!");
                if (queue.Count > 0) {
                    Debug.Log("Queue is non-empty!");
                    current_interaction = queue[0].run_interaction(inventory, flags, UIController);
                    queue.RemoveAt(0);
                } else {
                    Debug.Log("Queue is empty");
                    current_interaction = null;
                }
            }
            body.velocity = Vector2.zero;
            if (my_animator != null) {
                my_animator.SetFloat("Horizontal", 0);
                my_animator.SetFloat("Vertical", 0);
            }
            this.UpdateFlagChangeListeners();
        } else {
            body.velocity = speed * move.ReadValue<Vector2>();
            float currentSpeed = body.velocity.magnitude;
            if (currentSpeed > speed) {
                body.velocity *= speed / currentSpeed;
            }

            if (my_animator != null) {
                my_animator.SetFloat("Horizontal", body.velocity.x);
                my_animator.SetFloat("Vertical", body.velocity.y);
                if (body.velocity.magnitude > 0.001) {
                    my_animator.SetFloat("Facing Horizontal", body.velocity.x < -0.5 ? -1 : body.velocity.x > 0.5 ? 1 : 0);
                    my_animator.SetFloat("Facing Vertical", body.velocity.y < -0.5 ? -1 : body.velocity.y > 0.5 ? 1 : 0);
                }
            }
        }

        if (toRemoveHandlers != null) {
            foreach (FlagUpdateReciever g in toRemoveHandlers) {
                onFlagChangeHandlers.Remove(g);
            }
            toRemoveHandlers = null;
        }
    }

    private List<Interactable> queue;
    void QueueInteraction(Interactable interactable) {
        queue.Add(interactable);
    }

    void OnTriggerEnter2D(Collider2D collider) {
        Interactable interactable = collider.GetComponent<Interactable>();
        if (interactable != null) {
            interactables.Add(interactable);
        }
    }

    void OnTriggerExit2D(Collider2D collider) {
        Interactable interactable = collider.GetComponent<Interactable>();
        if (interactable != null) {
            interactables.Remove(interactable);
        }
    }
}
