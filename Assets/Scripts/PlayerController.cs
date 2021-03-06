﻿using System.Collections;
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
    public FlagSet flags;
    private List<Interactable> interactables;
    private IEnumerator<bool> current_interaction;
    public List<FlagUpdateReciever> onFlagChangeHandlers;
    private Vector2 start_pos;

    public Inventory inventory;
    private Collider2D _collider;
    public Collider2D Collider {
        get => _collider;
    }

    void Awake() {
        body = this.GetComponent<Rigidbody2D>();
        my_animator = this.GetComponent<Animator>();
        _collider = this.GetComponent<Collider2D>();

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
        AddFlagChangeHandler(new RecipeState.RecipeStateFlagReciever(flags));
        interactables = new List<Interactable>();
        queue = new List<Interactable>();
        inventory = new Inventory(this);
    }
    void UpdateFlagChangeListeners() {
        if (onFlagChangeHandlers == null) {
            onFlagChangeHandlers = new List<FlagUpdateReciever>();
        }
        var changelog = flags.pull_change_log();
        while (changelog.Count > 0) {
            foreach (Flag f in changelog) {
                foreach (FlagUpdateReciever updater in onFlagChangeHandlers) {
                    if (f.Instruction == FlagInstruction.Set) {
                        updater.flag_set(f.Value);
                    } else {
                        updater.flag_unset(f.Value);
                    }
                }
            }
            changelog = flags.pull_change_log();
        }
    }

    private bool swung_sword;
    void OnInteract(InputAction.CallbackContext action_context) {
        if (current_interaction != null) return;
        foreach (var interactable in interactables) {
            if (interactable == null) continue;
            if (interactable is MonoBehaviour b && b.gameObject == null) continue;
            if (current_interaction == null) {
                var enumerable = interactable.run_interaction(inventory, flags, UIController);
                if (enumerable != null) {
                    current_interaction = enumerable.GetEnumerator();
                }
                if (current_interaction == null) {
                    continue;
                }
            }
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

    protected IEnumerator<bool> display_inventory() {
        this.UIController.ShowInventory(this.inventory);
        while (this.viewing_inventory) {
            yield return true;
            if (this.UIController.InventorySelected(out ItemInfo info)) {
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
                current_interaction = null;
                while (queue.Count > 0 && current_interaction == null) {
                    Debug.Log("Queue is non-empty!");
                    var enumerable = queue[0].run_interaction(inventory, flags, UIController);
                    if (enumerable != null) {
                        current_interaction = enumerable.GetEnumerator();
                    }
                    queue.RemoveAt(0);
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
            if (follow.Item1 != null) {
                var follow_offset = (Vector2)(follow.Item1.position - transform.position);
                body.velocity += follow_offset.normalized * follow.Item2;
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

    private (Transform, float) follow;
    public void add_follow(Transform transform, float amount) {
        follow = (transform, amount);
    }

    public void remove_follow() {
        follow = (null, 0);
    }

    private List<Interactable> queue;
    public void QueueInteraction(Interactable interactable) {
        if (interactable == null) return;
        if (current_interaction == null) {
            var enumerable = interactable.run_interaction(inventory, flags, UIController);
            if (enumerable != null) {
                current_interaction = enumerable.GetEnumerator();
            }
        } else {
            queue.Add(interactable);
        }
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
