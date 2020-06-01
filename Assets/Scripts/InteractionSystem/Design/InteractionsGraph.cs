﻿
using UnityEngine;
using BlueGraph;
using System.Collections.Generic;

namespace Interactions
{
    using Dialogue;
    using Data;
    public enum InteractionsVariableType {
        Integer,
        Boolean,
        String,
        Float,
        Vector2,
        Vector3,
        InteractionsGraph,
        Animator
    }
    public static class InteractionsVariableTypeExtensions {
        public static InteractionsVariableType[] TypesByIndex = {
            InteractionsVariableType.Integer,
            InteractionsVariableType.Boolean,
            InteractionsVariableType.String,
            InteractionsVariableType.Float,
            InteractionsVariableType.Vector2,
            InteractionsVariableType.Vector3,
            InteractionsVariableType.InteractionsGraph,
            InteractionsVariableType.Animator
        };
    }
    [System.Serializable]
    public struct InteractionsVariable {
        public string name;
        public InteractionsVariableType var_type;
        public int int_val;
        public bool bool_val;
        public string string_val;
        public float float_val;
        public Vector2 vec2_val;
        public Vector3 vec3_val;
        public InteractionsGraph graph_val;
        public Animator animator_val;
        public object value {
            get {
                switch (var_type) {
                    case InteractionsVariableType.Integer:
                        return int_val;
                    case InteractionsVariableType.Boolean:
                        return bool_val;
                    case InteractionsVariableType.String:
                        return string_val;
                    case InteractionsVariableType.Float:
                        return float_val;
                    case InteractionsVariableType.Vector2:
                        return vec2_val;
                    case InteractionsVariableType.Vector3:
                        return vec3_val;
                    case InteractionsVariableType.InteractionsGraph:
                        return graph_val;
                    case InteractionsVariableType.Animator:
                        return animator_val;
                    default:
                        return null;
                }
            }
            set {
                switch (var_type) {
                    case InteractionsVariableType.Integer:
                        if (value is int i) {
                            int_val = i;
                        }
                        return;
                    case InteractionsVariableType.Boolean:
                        if (value is bool b) {
                            Debug.Log("SUCCESSFUL BOOL SET");
                            bool_val = b;
                        }
                        return;
                    case InteractionsVariableType.String:
                        if (value is string s) {
                            string_val = s;
                        }
                        return;
                    case InteractionsVariableType.Float:
                        if (value is float f) {
                            float_val = f;
                        }
                        return;
                    case InteractionsVariableType.Vector2:
                        if (value is Vector2 v2) {
                            vec2_val = v2;
                        }
                        return;
                    case InteractionsVariableType.Vector3:
                        if (value is Vector3 v3) {
                            vec3_val = v3;
                        }
                        return;
                    case InteractionsVariableType.InteractionsGraph:
                        if (value is InteractionsGraph g) {
                          graph_val = g;
                        }
                        return;
                    case InteractionsVariableType.Animator:
                        if (value is Animator a) {
                            animator_val = a;
                        }
                        return;
                }
            }
        }
    }

    public struct InteractionContext {
        public bool complete;
        public InteractionNode interaction_at;
        public Inventory inventory;
        public FlagSet flags;
        public UIController ui_controller;
    }

    [IncludeModules("Interactions", "Items", "Helpers", "Variables", "Gameobject", "Operations/Boolean", "Operations/Animator")]
    public class InteractionsGraph : Graph
    {
        public InteractionsVariable[] variables;
        [System.NonSerialized] public InteractionContext active_context;

        public bool GetVariable(string var_name, out InteractionsVariable var) {
            foreach (InteractionsVariable v in variables) {
                if (v.name.Equals(var_name)) {
                    var = v;
                    return true;
                }
            }
            var = new InteractionsVariable{};
            return false;
        }
        public bool SetVariable(string var_name, object value) {
            for (int i = 0; i < variables.Length; i++) {
                if (variables[i].name.Equals(var_name)) {
                    variables[i].value = value;
                    return true;
                }
            }
            return false;
        }
        public List<InteractionEntryNode> GetEntryPoints() {
            return this.FindNodes<InteractionEntryNode>();
        }
        public InteractionEntryNode GetEntryPoint(string name) {
            foreach (InteractionEntryNode node in GetEntryPoints()) {
                if (node.Action == name) {
                    return node;
                }
            }
            return null;
        }

        public bool StartInteraction(InteractionEntryNode start, Inventory inventory, FlagSet flags, UIController ui_controller, out InteractionContext context) {
            context = new InteractionContext {
                complete = false,
                interaction_at = start,
                inventory = inventory,
                flags = flags,
                ui_controller = ui_controller,
            };
            return FollowInteraction(ref context);
        }

        public bool FollowInteraction(ref InteractionContext context) {
            if (context.complete) return true;
            if (context.interaction_at is DialogueNode dialogue) {
                if (context.ui_controller.TrySelect(out var next_node)) {
                    return MoveNext(ref context, next_node);
                }
            } else if (handle_custom_node_continue(context.interaction_at, context, out var next_node)) {
                return MoveNext(ref context, next_node);
            }
            return false;
        }

        private bool MoveNext(ref InteractionContext context, InteractionNode node) {
            while (true) {
                if (node == null) {
                    context.complete = true;
                    context.interaction_at = null;
                    return true;
                }
                if (!node.FlagsMeetRequirements(context.flags)) {
                    context.complete = true;
                    context.interaction_at = null;
                    return true;
                }
                active_context = context;
                context.interaction_at = node;
                foreach (var flag in node.flagInteractions.flags) {
                    if (flag.Instruction == FlagInstruction.Set) {
                        context.flags[flag.Value] = true;
                    } else if (flag.Instruction == FlagInstruction.Unset) {
                        context.flags[flag.Value] = false;
                    }
                }

                if (node is VarSetNode var_set) {
                    var target = var_set.Target;
                    if (target == null) throw new System.Exception("Trying to set a variable on a null target!");
                    if (target.GetVariable(var_set.var_name, out var var)) {
                        if (var.var_type != var_set.VarType) {
                            throw new System.Exception($"Variable {var_set.var_name} exists but is of type {var.var_type} not {var_set.VarType}.");
                        }
                        target.SetVariable(var_set.var_name, var_set.Input);
                    } else {
                        throw new System.Exception($"Variable {var_set.var_name} does not exist.");
                    }
                } else if (node is SetAnimatorBooleanNode anim_var_set) {
                    var anim = anim_var_set.Animator;
                    if (anim != null)
                        anim.SetBool(anim_var_set.ParameterName, anim_var_set.Value);
                    else
                        throw new System.Exception($"Trying to set animation variable {anim_var_set.ParameterName} on null animator!");
                } else if (node is SetGameobjectEnabled game_obj_enabled) {
                    var gameobject = game_obj_enabled.Gameobject;
                    if (gameobject != null) {
                        gameobject.SetActive(game_obj_enabled.Value);
                    } else {
                        throw new System.Exception("Trying to set active state on null game object!");
                    }
                } else if (node is DestroyGameobjectNode destroy_node) {
                    var gameobject = destroy_node.Gameobject;
                    if (gameobject != null) {
                        Destroy(gameobject);
                    } else {
                        throw new System.Exception("Trying to destroy a null game object!");
                    }
                } else if (node is TakeItemNode take_item_node) {
                    node = take_item_node.TryTakeItem(context.inventory);
                    continue;
                } else if (node is DialogueNode dialogue) {
                    context.ui_controller.ShowNode(dialogue, context.inventory, context.flags);
                    return false;
                } else {
                    if (handle_custom_node(node, context, out var next_node)) {
                        node = next_node;
                        continue;
                    } else {
                        return false;
                    }
                }
                node = node.GetNextNode();
            }
        }

        protected virtual bool handle_custom_node(
            InteractionNode node,
            InteractionContext context,
            out InteractionNode next
        ) {
            next = node.GetNextNode();
            return true;
        }

        protected virtual bool handle_custom_node_continue(
            InteractionNode node,
            InteractionContext context,
            out InteractionNode next
        ) {
            next = node.GetNextNode();
            return true;
        }
    }
}
