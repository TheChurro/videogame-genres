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
public class InventoryController
{
    private PanelRenderer inventory_renderer;
    private ListController<ItemInfo> inventory_controller;
    private Inventory inventory;

    public InventoryController(PanelRenderer inventory_renderer, VisualTreeAsset item_template) {
        this.inventory_renderer = inventory_renderer;
        this.inventory_renderer.visualTree.style.display = DisplayStyle.Flex;
        this.inventory_renderer.visualTree.style.visibility = Visibility.Visible;
        this.inventory_renderer.enabled = false;

        var inventory_panel = this.inventory_renderer.visualTree.Q<VisualElement>("inventory-panel");
        var inventory_list = this.inventory_renderer.visualTree.Q<ListView>("inventory-list");
        var item_preview = this.inventory_renderer.visualTree.Q<Label>("item-preview");
        this.inventory_controller = new ListController<ItemInfo>(
            inventory_panel,
            inventory_list,
            item_preview,
            item_template,
            0
        );

        this.inventory_controller.Hide();
    }

    public void Hide() {
        this.inventory_renderer.enabled = false;
        this.inventory_controller.Hide();
    }

    public void Display(Inventory inventory) {
        this.inventory_renderer.enabled = true;
        this.inventory = inventory;

        var items = new List<ListController<ItemInfo>.ListElement>();
        foreach (var item in this.inventory.items()) {
            items.Add(new ListController<ItemInfo>.ListElement(){
                text = $"{item.Item1.name} ({item.Item2})",
                image = item.Item1.image,
                description = item.Item1.description,
                choosable = true,
                value = item.Item1
            });
        }

        this.inventory_controller.ShowList(items);
    }

    public void UpChoice() {
        this.inventory_controller.UpChoice();
    }

    public void DownChoice() {
        this.inventory_controller.DownChoice();
    }

    public void Confirm() {
        this.inventory_controller.Confirm();
    }

    public bool TrySelect(out ItemInfo info) {
        info = null;
        if (this.inventory_controller.GetConfirmed(out var return_val)) {
            info = return_val;
            return true;
        }
        return false;
    }

    public void Remove(ItemInfo info) {
        this.inventory_controller.Remove(info);
    }

    public void UpdateText(ItemInfo info) {
        this.inventory_controller.SetText(info, $"{info.name} ({inventory[info]})");
    }
}
