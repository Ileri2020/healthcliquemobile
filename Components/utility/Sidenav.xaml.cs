using System.Collections.ObjectModel;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class Sidenav : ContentView
{
    public static readonly BindableProperty ItemsProperty =
        BindableProperty.Create(nameof(Items), typeof(ObservableCollection<UtilityNavigationItem>), typeof(Sidenav));
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(Sidenav), string.Empty);
    public static readonly BindableProperty MenuTextProperty =
        BindableProperty.Create(nameof(MenuText), typeof(string), typeof(Sidenav), "Menu");
    public static readonly BindableProperty IsOpenProperty =
        BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(Sidenav), false, BindingMode.TwoWay);
    public static readonly BindableProperty PanelWidthProperty =
        BindableProperty.Create(nameof(PanelWidth), typeof(double), typeof(Sidenav), 320d);
    public static readonly BindableProperty FooterContentProperty =
        BindableProperty.Create(nameof(FooterContent), typeof(View), typeof(Sidenav));

    public Sidenav()
    {
        InitializeComponent();
        Items = [];
        ToggleCommand = new Command(() => IsOpen = !IsOpen);
        CloseCommand = new Command(() => IsOpen = false);
        NavigateCommand = new Command<UtilityNavigationItem>(async item => await NavigateAsync(item));
    }

    public ObservableCollection<UtilityNavigationItem> Items { get => (ObservableCollection<UtilityNavigationItem>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string MenuText { get => (string)GetValue(MenuTextProperty); set => SetValue(MenuTextProperty, value); }
    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public double PanelWidth { get => (double)GetValue(PanelWidthProperty); set => SetValue(PanelWidthProperty, value); }
    public View? FooterContent { get => (View?)GetValue(FooterContentProperty); set => SetValue(FooterContentProperty, value); }
    public ICommand ToggleCommand { get; }
    public ICommand CloseCommand { get; }
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
            IsOpen = false;
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            NavigationFailed?.Invoke(this, exception);
        }
    }
}
