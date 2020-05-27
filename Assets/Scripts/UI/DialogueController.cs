﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.UIElements.Runtime;
using UnityEngine.UIElements;
using Interactions;
using Interactions.Dialogue;
using BlueGraph;
using UnityEngine.InputSystem;

using Data;

public class DialogueController
{
    private PanelRenderer DialogueRender;
    private ListController<int> choice_controller;
    private VisualElement DialoguePanel;
    private Label NameLabel;
    private VisualElement PortraitImage;
    private VisualElement PortraitContainer;
    private Label DialogueLabel;

    private DialogueNode dialogue_node;
    private DialogueChoiceNode choice_node;
    private MakeRecipeNode recipe_node;

    private Inventory inventory;

    public DialogueController(PanelRenderer UIRenderer, VisualTreeAsset ListItemTemplate) {
        this.DialogueRender = UIRenderer;

        DialogueRender.visualTree.style.display = DisplayStyle.Flex;
        DialogueRender.visualTree.style.visibility = Visibility.Visible;
        DialogueRender.enabled = false;

        var ChoicePanel = UIRenderer.visualTree.Q<VisualElement>("choice-panel");
        var ChoiceList = UIRenderer.visualTree.Q<ListView>("choice-list");
        var ChoiceLabel = UIRenderer.visualTree.Q<Label>("choice-preview");
        this.choice_controller = new ListController<int>(
            ChoicePanel,
            ChoiceList,
            ChoiceLabel,
            ListItemTemplate
        );

        DialoguePanel = UIRenderer.visualTree.Q<VisualElement>("dialogue-panel");
        NameLabel = UIRenderer.visualTree.Q<Label>("character-name");
        PortraitContainer = UIRenderer.visualTree.Q<VisualElement>("portrait-container");
        PortraitImage = UIRenderer.visualTree.Q<VisualElement>("character-portrait");
        DialogueLabel = UIRenderer.visualTree.Q<Label>("dialogue");
        
        this.choice_controller.Hide();
        DialoguePanel.visible = false;
        NameLabel.visible = false;
        PortraitContainer.style.display = DisplayStyle.None;
        DialogueLabel.text = "This is some basic dialogue text. It is nice to have, right?";
    }

    public void Hide() {
        DialogueRender.enabled = false;

        // Hide visible information
        DialoguePanel.visible = false;
        PortraitContainer.visible = false;
        NameLabel.visible = false;
        this.choice_controller.Hide();

        // Clear dialogue node state
        dialogue_node = null;
        choice_node = null;
        recipe_node = null;
    }

    public void Show(DialogueNode node, Inventory inventory, FlagSet flag_set) {
        DialogueRender.enabled = true;

        dialogue_node = node;
        this.inventory = inventory;
        ShowDialogue();
        if (node is DialogueChoiceNode) {
            choice_node = node as DialogueChoiceNode;
            ShowChoices(flag_set);
        }
        if (node is MakeRecipeNode) {
            recipe_node = node as MakeRecipeNode;
            ShowRecipes(inventory);
        }
    }

    private void ShowCharacter() {
        if (dialogue_node.Character != null) {
            NameLabel.text = dialogue_node.Character.name;
            NameLabel.style.color = dialogue_node.Character.color;
            NameLabel.visible = true;
            if (dialogue_node.Character.portrait != null) {
                PortraitImage.style.backgroundImage = new StyleBackground(dialogue_node.Character.portrait);
                PortraitContainer.visible = true;
            }
            else
            {
                PortraitContainer.visible = false;
            }
        }
        else
        {
            NameLabel.visible = false;
        }
    }

    private void ShowChoices(FlagSet flag_set) {
        if (choice_node.answers.Count > 0) {
            var choices = new List<ListController<int>.ListElement>();
            for (int i = 0; i < choice_node.answers.Count; i++) {
                Port port = choice_node.GetPort($"answers[{i}]");
                var answer = choice_node.answers[i];
                if (port.IsConnected && port.ConnectedPorts[0].node is InteractionNode) {
                    if ((port.ConnectedPorts[0].node as InteractionNode).FlagsMeetRequirements(flag_set)) {
                        var choice = new ListController<int>.ListElement(){
                            text = answer.list_text,
                            value = i,
                            choosable = true,
                        };
                        if (answer.unique_preview) {
                            choice.description = answer.preview;
                        } else if (answer.show_preview && port.ConnectedPorts[0].node is DialogueNode) {
                            choice.description = (port.ConnectedPorts[0].node as DialogueNode).Dialogue;
                        } else {
                            choice.description = null;
                        }
                        choices.Add(choice);
                    }
                } else {
                    choices.Add(new ListController<int>.ListElement(){
                        text = answer.list_text,
                        description = answer.show_preview || answer.unique_preview ? answer.preview : null,
                        choosable = true,
                        value = i
                    });
                }
            }
            if (choices.Count > 0) {
                this.choice_controller.ShowList(choices);
                return;
            }
        }
        choice_node = null;
    }

    private void ShowRecipes(Inventory inventory) {
        var recipes = RecipeState.available_recipies();
        var choices = new List<ListController<int>.ListElement>();
        for (int i = 0; i < recipes.Count; i++) {
            string requirements_string = "Required:";
            for (int j = 0; j < recipes[i].inputs.Length; j++) {
                if (j != 0) {
                    requirements_string = $"{requirements_string},";
                }
                requirements_string = $"{requirements_string} {recipes[i].inputs[j].amount} {recipes[i].inputs[j].info.name}";
            }
            choices.Add(new ListController<int>.ListElement(){
                text = recipes[i].output.info.name,
                choosable = inventory.can_make(recipes[i]),
                description = requirements_string,
                image = recipes[i].output.info.image
            });
        }
        choices.Add(new ListController<int>.ListElement(){
            text = "Cancel",
            choosable = true,
            description = null,
            value = -1,
        });
        this.choice_controller.ShowList(choices);
    }

    private void ShowDialogue() {
        DialoguePanel.visible = true;
        ShowCharacter();
        DialogueLabel.text = dialogue_node.Dialogue;
    }

    public bool IsChoice() {
        return (choice_node != null || recipe_node != null);
    }

    public bool TrySelect(out InteractionNode node) {
        node = null;
        if (IsChoice()) {
            if (this.choice_controller.GetConfirmed(out int chosen)) {
                if (choice_node != null) {
                    node = choice_node.GetNextNodeForChoice(chosen);
                } else if (recipe_node != null) {
                    node = recipe_node.GetNextNodeForChoiceAndMake(inventory, chosen);
                }
                Hide();
                return true;
            }
        } else if (do_confirm) {
            node = dialogue_node.GetNextNode();
            Hide();
            return true;
        }
        return false;
    }

    public void UpChoice() {
        this.choice_controller.UpChoice();
    }
    public void DownChoice() {
        this.choice_controller.DownChoice();
    }

    private bool do_confirm;
    public void Confirm() {
        if (!IsChoice()) {
            do_confirm = true;
        }
        this.choice_controller.Confirm();
    }
}