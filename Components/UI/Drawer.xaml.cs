namespace EEE.Components.UI;

public enum DrawerPosition
{
    Left,
    Right
}

public partial class Drawer : ContentView
{
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(Drawer), false, BindingMode.TwoWay);
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(Drawer), string.Empty);
    public static readonly BindableProperty BodyContentProperty = BindableProperty.Create(nameof(BodyContent), typeof(View), typeof(Drawer));
    public static readonly BindableProperty DrawerWidthProperty = BindableProperty.Create(nameof(DrawerWidth), typeof(double), typeof(Drawer), 320d);
    public static readonly BindableProperty PositionProperty = BindableProperty.Create(nameof(Position), typeof(DrawerPosition), typeof(Drawer), DrawerPosition.Left, propertyChanged: OnPositionChanged);

    public Drawer()
    {
        InitializeComponent();
        UpdatePosition();
    }

    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public View? BodyContent { get => (View?)GetValue(BodyContentProperty); set => SetValue(BodyContentProperty, value); }
    public double DrawerWidth { get => (double)GetValue(DrawerWidthProperty); set => SetValue(DrawerWidthProperty, value); }
    public DrawerPosition Position { get => (DrawerPosition)GetValue(PositionProperty); set => SetValue(PositionProperty, value); }

    public event EventHandler? Dismissed;

    private static void OnPositionChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((Drawer)bindable).UpdatePosition();

    private void UpdatePosition()
    {
        if (DrawerPanel is not null)
            DrawerPanel.HorizontalOptions = Position == DrawerPosition.Right ? LayoutOptions.End : LayoutOptions.Start;
    }

    private void OnCloseClicked(object? sender, EventArgs e) => Close();
    private void OnBackdropClicked(object? sender, EventArgs e) => Close();

    private void Close()
    {
        IsOpen = false;
        Dismissed?.Invoke(this, EventArgs.Empty);
    }
}
