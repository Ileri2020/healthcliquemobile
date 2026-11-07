using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class LocalSalesSyncButton : ContentView
{
    public static readonly BindableProperty FullWidthProperty =
        BindableProperty.Create(nameof(FullWidth), typeof(bool), typeof(LocalSalesSyncButton), false, propertyChanged: OnLayoutChanged);
    public static readonly BindableProperty IsLocalProperty =
        BindableProperty.Create(nameof(IsLocal), typeof(bool), typeof(LocalSalesSyncButton), LocalOperationModeService.IsLocalEnvironment);
    public static readonly BindableProperty PendingSalesProperty =
        BindableProperty.Create(nameof(PendingSales), typeof(int), typeof(LocalSalesSyncButton), 0, propertyChanged: OnCountChanged);
    public static readonly BindableProperty PendingCountsProperty =
        BindableProperty.Create(nameof(PendingCounts), typeof(int), typeof(LocalSalesSyncButton), 0, propertyChanged: OnCountChanged);
    public static readonly BindableProperty PendingStocksProperty =
        BindableProperty.Create(nameof(PendingStocks), typeof(int), typeof(LocalSalesSyncButton), 0, propertyChanged: OnCountChanged);
    public static readonly BindableProperty IsSyncingProperty =
        BindableProperty.Create(nameof(IsSyncing), typeof(bool), typeof(LocalSalesSyncButton), false, propertyChanged: OnSyncStateChanged);
    public static readonly BindableProperty SyncCommandProperty =
        BindableProperty.Create(nameof(SyncCommand), typeof(ICommand), typeof(LocalSalesSyncButton), propertyChanged: OnCountChanged);
    public static readonly BindableProperty ButtonTextProperty =
        BindableProperty.Create(nameof(ButtonText), typeof(string), typeof(LocalSalesSyncButton), "Sync offline");
    public static readonly BindableProperty CanSyncProperty =
        BindableProperty.Create(nameof(CanSync), typeof(bool), typeof(LocalSalesSyncButton), false);

    public LocalSalesSyncButton()
    {
        InitializeComponent();
        UpdateState();
    }

    public bool FullWidth { get => (bool)GetValue(FullWidthProperty); set => SetValue(FullWidthProperty, value); }
    public bool IsLocal { get => (bool)GetValue(IsLocalProperty); set => SetValue(IsLocalProperty, value); }
    public int PendingSales { get => (int)GetValue(PendingSalesProperty); set => SetValue(PendingSalesProperty, value); }
    public int PendingCounts { get => (int)GetValue(PendingCountsProperty); set => SetValue(PendingCountsProperty, value); }
    public int PendingStocks { get => (int)GetValue(PendingStocksProperty); set => SetValue(PendingStocksProperty, value); }
    public bool IsSyncing { get => (bool)GetValue(IsSyncingProperty); set => SetValue(IsSyncingProperty, value); }
    public ICommand? SyncCommand { get => (ICommand?)GetValue(SyncCommandProperty); set => SetValue(SyncCommandProperty, value); }
    public string ButtonText { get => (string)GetValue(ButtonTextProperty); private set => SetValue(ButtonTextProperty, value); }
    public bool CanSync { get => (bool)GetValue(CanSyncProperty); private set => SetValue(CanSyncProperty, value); }
    public LayoutOptions LayoutOptions => FullWidth ? LayoutOptions.Fill : LayoutOptions.Start;

    private static void OnLayoutChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((LocalSalesSyncButton)bindable).OnPropertyChanged(nameof(LayoutOptions));

    private static void OnCountChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((LocalSalesSyncButton)bindable).UpdateState();

    private static void OnSyncStateChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((LocalSalesSyncButton)bindable).UpdateState();

    private void UpdateState()
    {
        var total = Math.Max(0, PendingSales) + Math.Max(0, PendingCounts) + Math.Max(0, PendingStocks);
        ButtonText = IsSyncing
            ? "Syncing..."
            : FullWidth
                ? $"Sync {PendingSales} sales · {PendingCounts} counts · {PendingStocks} stock"
                : total == 0 ? "Sync offline" : $"Sync offline ({total})";
        CanSync = !IsSyncing && total > 0 && SyncCommand is not null;
        OnPropertyChanged(nameof(LayoutOptions));
    }
}
