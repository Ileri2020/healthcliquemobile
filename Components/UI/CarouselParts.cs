namespace EEE.Components.UI;

public sealed class CarouselApi
{
    private int _position;
    private int _itemCount;

    public int Position
    {
        get => _position;
        set => SetPosition(value);
    }

    public int ItemCount
    {
        get => _itemCount;
        set
        {
            _itemCount = Math.Max(0, value);
            SetPosition(_position);
        }
    }

    public event Action<object?, int>? PositionChanged;
    public void ScrollPrevious() => Position = Math.Max(0, Position - 1);
    public void ScrollNext() => Position = Math.Min(Math.Max(0, ItemCount - 1), Position + 1);
    public void ScrollTo(int position) => Position = Math.Clamp(position, 0, Math.Max(0, ItemCount - 1));

    private void SetPosition(int position)
    {
        var newPosition = Math.Clamp(position, 0, Math.Max(0, ItemCount - 1));
        if (_position == newPosition)
            return;

        _position = newPosition;
        PositionChanged?.Invoke(this, newPosition);
    }
}

public class CarouselContent : ContentView { }

public class CarouselItem : ContentView { }

public abstract class CarouselNavigationButton : ContentView
{
    public static readonly BindableProperty PositionProperty =
        BindableProperty.Create(nameof(Position), typeof(int), typeof(CarouselNavigationButton), 0, BindingMode.TwoWay);
    public static readonly BindableProperty ItemCountProperty =
        BindableProperty.Create(nameof(ItemCount), typeof(int), typeof(CarouselNavigationButton), 0);

    protected CarouselNavigationButton()
    {
        var button = new Button();
        button.Text = Label;
        button.Command = new Command(Move);
        Content = button;
    }

    public int Position { get => (int)GetValue(PositionProperty); set => SetValue(PositionProperty, value); }
    public int ItemCount { get => (int)GetValue(ItemCountProperty); set => SetValue(ItemCountProperty, value); }
    protected abstract string Label { get; }
    protected abstract int Direction { get; }

    private void Move()
    {
        var lastPosition = Math.Max(0, ItemCount - 1);
        Position = Math.Clamp(Position + Direction, 0, lastPosition);
    }
}

public class CarouselPrevious : CarouselNavigationButton
{
    protected override string Label => "Previous";
    protected override int Direction => -1;
}

public class CarouselNext : CarouselNavigationButton
{
    protected override string Label => "Next";
    protected override int Direction => 1;
}
