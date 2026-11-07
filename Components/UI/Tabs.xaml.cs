using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace EEE.Components.UI;

public sealed class TabItem
{
    public string Title { get; set; } = string.Empty;
    public View? Content { get; set; }
}

public partial class Tabs : ContentView
{
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(ObservableCollection<TabItem>), typeof(Tabs), propertyChanged: OnItemsPropertyChanged);
    public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(Tabs), 0, BindingMode.TwoWay, propertyChanged: OnSelectedIndexChanged);
    public static readonly BindableProperty SelectedTabProperty = BindableProperty.Create(nameof(SelectedTab), typeof(TabItem), typeof(Tabs));
    public static readonly BindableProperty SelectedKeyProperty = BindableProperty.Create(nameof(SelectedKey), typeof(string), typeof(Tabs), string.Empty, BindingMode.TwoWay);
    public static readonly BindableProperty CustomContentProperty = BindableProperty.Create(nameof(CustomContent), typeof(View), typeof(Tabs), propertyChanged: OnCustomContentChanged);
    public static readonly BindableProperty HasCustomContentProperty = BindableProperty.Create(nameof(HasCustomContent), typeof(bool), typeof(Tabs), false);
    public static readonly BindableProperty HasItemsProperty = BindableProperty.Create(nameof(HasItems), typeof(bool), typeof(Tabs), false);

    public Tabs()
    {
        InitializeComponent();
        Items = new ObservableCollection<TabItem>();
        SelectTabCommand = new Command<TabItem>(SelectTab);
    }

    public ObservableCollection<TabItem> Items { get => (ObservableCollection<TabItem>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public int SelectedIndex { get => (int)GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }
    public TabItem? SelectedTab { get => (TabItem?)GetValue(SelectedTabProperty); private set => SetValue(SelectedTabProperty, value); }
    public string SelectedKey { get => (string)GetValue(SelectedKeyProperty); set => SetValue(SelectedKeyProperty, value); }
    public View? CustomContent { get => (View?)GetValue(CustomContentProperty); set => SetValue(CustomContentProperty, value); }
    public bool HasCustomContent { get => (bool)GetValue(HasCustomContentProperty); private set => SetValue(HasCustomContentProperty, value); }
    public bool HasItems { get => (bool)GetValue(HasItemsProperty); private set => SetValue(HasItemsProperty, value); }
    public ICommand SelectTabCommand { get; }

    private static void OnSelectedIndexChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((Tabs)bindable).UpdateSelectedTab();

    private static void OnItemsPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tabs = (Tabs)bindable;
        if (oldValue is ObservableCollection<TabItem> oldItems)
            oldItems.CollectionChanged -= tabs.OnItemsChanged;
        if (newValue is ObservableCollection<TabItem> newItems)
            newItems.CollectionChanged += tabs.OnItemsChanged;
        tabs.UpdateSelectedTab();
    }

    private static void OnCustomContentChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tabs = (Tabs)bindable;
        tabs.HasCustomContent = newValue is View;
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateSelectedTab();

    private void SelectTab(TabItem? tab)
    {
        var index = tab is null ? -1 : Items.IndexOf(tab);
        if (index >= 0)
            SelectedIndex = index;
    }

    private void UpdateSelectedTab()
    {
        if (Items is null || Items.Count == 0)
        {
            SelectedTab = null;
            HasItems = false;
            return;
        }

        HasItems = true;
        if (SelectedIndex < 0 || SelectedIndex >= Items.Count)
            SelectedIndex = 0;
        else
            SelectedTab = Items[SelectedIndex];
    }
}
