using System.Collections.ObjectModel;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class Nav : ContentView
{
    public static readonly BindableProperty ItemsProperty =
        BindableProperty.Create(nameof(Items), typeof(ObservableCollection<UtilityNavigationItem>), typeof(Nav));
    public static readonly BindableProperty CurrentPathProperty =
        BindableProperty.Create(nameof(CurrentPath), typeof(string), typeof(Nav), string.Empty);

    public Nav()
    {
        InitializeComponent();
        Items = [];
        NavigateCommand = new Command<UtilityNavigationItem>(async item => await NavigateAsync(item));
    }

    public ObservableCollection<UtilityNavigationItem> Items { get => (ObservableCollection<UtilityNavigationItem>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public string CurrentPath { get => (string)GetValue(CurrentPathProperty); set => SetValue(CurrentPathProperty, value); }
    public ICommand NavigateCommand { get; }
    public event Action<object?, Exception>? NavigationFailed;

    private async void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not UtilityNavigationItem item)
            return;

        try
        {
            await NavigateAsync(item);
        }
        finally
        {
            ((CollectionView)sender!).SelectedItem = null;
        }
    }

    private async Task NavigateAsync(UtilityNavigationItem? item)
    {
        if (item is null)
            return;

        try
        {
            await NavigationUtility.NavigateAsync(item);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            NavigationFailed?.Invoke(this, exception);
        }
    }
}
