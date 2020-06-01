using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.UIElements.Runtime;
using UnityEngine.UIElements;
using Unity.UIElements;
using Interactions;
using Interactions.Dialogue;
using BlueGraph;
using UnityEngine.InputSystem;

using Data;

public class ListController<T> {
    private VisualElement list_panel;
    private ListView list_view;
    private Label description_view;
    private VisualTreeAsset list_item_template;
    private bool confirmed;
    private int max_items;

    public struct ListElement {
        public string text;
        public Texture2D image;
        public string description;
        public T value;
        public bool choosable;
    }
    private List<ListElement> options;
    private int current_choice = 0;

    public ListController(
        VisualElement ListPanel,
        // VisualElement RecipeContainer,
        ListView ListView,
        Label DescriptionView,
        VisualTreeAsset ListItemTemplate,
        int max_items = 3
    ) {
        this.list_panel = ListPanel;
        this.list_view = ListView;
        this.description_view = DescriptionView;
        this.options = new List<ListElement>();
        this.list_item_template = ListItemTemplate;
        this.max_items = max_items;

        this.list_view.selectionType = SelectionType.Single;
        var scrollView = this.list_view.Q<ScrollView>();
        if (scrollView != null) {
            scrollView.verticalScroller.style.display = DisplayStyle.None;
        }
        this.list_view.makeItem = MakeItem;
        this.list_view.bindItem = BindItem;
        this.list_view.onItemChosen += obj => Confirm();
        this.list_view.onSelectionChanged += selection => SelectionChanged();
        this.list_view.itemsSource = this.options;
        this.list_panel.style.display = DisplayStyle.None;
    }

    private VisualElement MakeItem() {
        return this.list_item_template.CloneTree();
    }

    private void BindItem(VisualElement item_view, int index) {
        ListElement element = this.options[index];
        var text_view = item_view.Q<Label>("text");
        if (text_view != null && element.text != null) {
            text_view.text = element.text;
            if (!element.choosable) {
                text_view.style.color = new StyleColor(Color.red);
            }
        }
        var image_view = item_view.Q<VisualElement>("image");
        if (image_view != null && element.image != null) {
            image_view.style.backgroundImage = new StyleBackground(element.image);
        }
    }

    public void ShowList(IEnumerable<ListElement> list) {
        this.options.Clear();
        this.options.AddRange(list);
        this.current_choice = 0;
        if (max_items > 0) {
            float max_height = Mathf.Min(
                this.list_view.itemHeight * max_items,
                this.list_view.itemHeight * this.options.Count
            );
            this.list_view.style.height = max_height;
        }
        this.confirmed = false;
        this.list_view.visible = true;
        this.list_panel.style.display = DisplayStyle.Flex;
        this.list_view.Refresh();
        if (this.options.Count > 0) {
            ScrollAndShowChoice();
        }
    }

    public void Hide() {
        ShowList(new List<ListElement>());
        this.list_panel.style.display = DisplayStyle.None;
        this.list_view.visible = false;
        this.description_view.visible = false;
    }

    public void DownChoice() {
        if (confirmed) return;
        current_choice++;
        if (current_choice >= this.options.Count) {
            current_choice = this.options.Count - 1;
        } else {
            ScrollAndShowChoice();
        }
    }

    public void UpChoice() {
        if (confirmed) return;
        current_choice--;
        if (current_choice < 0) {
            current_choice = 0;
        } else {
            ScrollAndShowChoice();
        }
    }

    public bool GetConfirmed(out T value) {
        value = default;
        if (!this.confirmed) return false;
        value = this.options[this.current_choice].value;
        this.confirmed = false;
        return true;
    }

    private void ScrollAndShowChoice() {
        this.list_view.ScrollToItem(current_choice);
        this.list_view.selectedIndex = current_choice;
    }

    private void SelectionChanged() {
        this.current_choice = this.list_view.selectedIndex;
        if (this.current_choice >= this.options.Count || this.current_choice < 0) {
            this.description_view.visible = false;
            return;
        }
        if (this.options[current_choice].description != null) {
            this.description_view.visible = true;
            this.description_view.text = this.options[current_choice].description;
        } else {
            this.description_view.visible = false;
        }
    }

    public void Confirm() {
        if (this.current_choice >= this.options.Count || this.current_choice < 0) return;
        if (this.options[this.current_choice].choosable) {
            this.confirmed = true;
        }
    }

    public void Remove(T value) {
        this.options.RemoveAll(elem => elem.value.Equals(value));
        Refresh();
        this.list_view.selectedIndex = Mathf.Min(current_choice, this.options.Count - 1);
    }

    public void SetText(T value, string text) {
        for (int i = 0; i < this.options.Count; i++) {
            var option = this.options[i];
            if (option.value.Equals(value)) {
                option.text = text;
                this.options[i] = option;
            }
        }
        Refresh();
    }

    public void SetDescription(T value, string description) {
        for (int i = 0; i < this.options.Count; i++) {
            var option = this.options[i];
            if (option.value.Equals(value)) {
                option.description = description;
                this.options[i] = option;
            }
        }
        Refresh();
    }

    public void Refresh() {
        this.list_view.Refresh();
    }
}