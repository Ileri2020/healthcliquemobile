namespace EEE.Components.Utility;

public enum TextAnimation
{
    Fade,
    SlideIn,
    SlideUp,
    ScaleIn,
    RotateIn,
    FlipIn,
    BounceIn,
    RiseAndFade,
    Zoom
}

public partial class TextCtrl : ContentView
{
    private IDispatcherTimer? _timer;
    private int _index;
    private bool _isAnimating;

    public static readonly BindableProperty TextsProperty = BindableProperty.Create(nameof(Texts), typeof(IList<string>), typeof(TextCtrl), defaultValueCreator: _ => new List<string>(), propertyChanged: OnTextsChanged);
    public static readonly BindableProperty VersesProperty = BindableProperty.Create(nameof(Verses), typeof(IList<ZoomVerse>), typeof(TextCtrl), defaultValueCreator: _ => new List<ZoomVerse>(), propertyChanged: OnVersesChanged);
    public static readonly BindableProperty IntervalProperty = BindableProperty.Create(nameof(Interval), typeof(int), typeof(TextCtrl), 6000, propertyChanged: OnIntervalChanged);
    public static readonly BindableProperty AnimationProperty = BindableProperty.Create(nameof(Animation), typeof(TextAnimation), typeof(TextCtrl), TextAnimation.Fade, propertyChanged: OnAnimationChanged);
    public static readonly BindableProperty DisplayTextProperty = BindableProperty.Create(nameof(DisplayText), typeof(string), typeof(TextCtrl), string.Empty);
    public static readonly BindableProperty DisplayPageProperty = BindableProperty.Create(nameof(DisplayPage), typeof(string), typeof(TextCtrl), string.Empty);

    public TextCtrl()
    {
        InitializeComponent();
        Loaded += (_, _) => StartRotation();
        Unloaded += (_, _) => StopRotation();
    }

    public IList<string> Texts { get => (IList<string>)GetValue(TextsProperty); set => SetValue(TextsProperty, value); }
    public IList<ZoomVerse> Verses { get => (IList<ZoomVerse>)GetValue(VersesProperty); set => SetValue(VersesProperty, value); }
    public int Interval { get => (int)GetValue(IntervalProperty); set => SetValue(IntervalProperty, value); }
    public TextAnimation Animation { get => (TextAnimation)GetValue(AnimationProperty); set => SetValue(AnimationProperty, value); }
    public string DisplayText { get => (string)GetValue(DisplayTextProperty); private set => SetValue(DisplayTextProperty, value); }
    public string DisplayPage { get => (string)GetValue(DisplayPageProperty); private set => SetValue(DisplayPageProperty, value); }
    public event EventHandler? RotationChanged;

    private static void OnTextsChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((TextCtrl)bindable).ResetRotation();

    private static void OnVersesChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((TextCtrl)bindable).ResetRotation();

    private static void OnIntervalChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((TextCtrl)bindable).StartRotation();

    private static void OnAnimationChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((TextCtrl)bindable).ApplyInitialState();

    private void ResetRotation()
    {
        _index = 0;
        SetDisplayValue();
        StartRotation();
    }

    private void StartRotation()
    {
        StopRotation();
        SetDisplayValue();
        if (ItemCount < 2 || Interval < 1)
            return;
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(Interval);
        _timer.Tick += async (_, _) => await RotateAsync();
        _timer.Start();
    }

    private void StopRotation()
    {
        if (_timer is null)
            return;
        _timer.Stop();
        _timer = null;
    }

    private int ItemCount => Animation == TextAnimation.Zoom ? Verses.Count : Texts.Count;

    private async Task RotateAsync()
    {
        if (_isAnimating || ItemCount == 0)
            return;
        _isAnimating = true;
        try
        {
            await AnimateOutAsync();
            _index = (_index + 1) % ItemCount;
            SetDisplayValue();
            await AnimateInAsync();
            RotationChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _isAnimating = false;
        }
    }

    private void SetDisplayValue()
    {
        if (ItemCount == 0)
        {
            DisplayText = string.Empty;
            DisplayPage = string.Empty;
            return;
        }
        _index %= ItemCount;
        if (Animation == TextAnimation.Zoom)
        {
            DisplayText = Verses[_index].Verse;
            DisplayPage = Verses[_index].Page;
        }
        else
        {
            DisplayText = Texts[_index];
            DisplayPage = string.Empty;
        }
        ApplyInitialState();
    }

    private void ApplyInitialState()
    {
        AnimatedText.Opacity = 0;
        AnimatedText.TranslationX = Animation == TextAnimation.SlideIn ? -36 : 0;
        AnimatedText.TranslationY = Animation is TextAnimation.SlideUp or TextAnimation.RiseAndFade ? 24 : 0;
        AnimatedText.Scale = Animation is TextAnimation.ScaleIn or TextAnimation.BounceIn or TextAnimation.Zoom ? 0.85 : 1;
        AnimatedText.Rotation = Animation is TextAnimation.RotateIn or TextAnimation.FlipIn ? -30 : 0;
    }

    private async Task AnimateOutAsync()
    {
        await AnimatedText.FadeToAsync(0, 180, Easing.CubicIn);
        if (Animation == TextAnimation.SlideIn)
            await AnimatedText.TranslateToAsync(36, 0, 180, Easing.CubicIn);
        else if (Animation is TextAnimation.SlideUp or TextAnimation.RiseAndFade)
            await AnimatedText.TranslateToAsync(0, -18, 180, Easing.CubicIn);
        else if (Animation is TextAnimation.ScaleIn or TextAnimation.BounceIn or TextAnimation.Zoom)
            await AnimatedText.ScaleToAsync(0.85, 180, Easing.CubicIn);
        else if (Animation is TextAnimation.RotateIn or TextAnimation.FlipIn)
            await AnimatedText.RotateToAsync(30, 180, Easing.CubicIn);
    }

    private async Task AnimateInAsync()
    {
        var duration = Animation == TextAnimation.RiseAndFade ? 900u : Animation == TextAnimation.Zoom ? 900u : 400u;
        var easing = Animation == TextAnimation.BounceIn ? Easing.BounceOut : Easing.CubicOut;
        var tasks = new List<Task> { AnimatedText.FadeToAsync(1, duration, easing) };
        if (Animation == TextAnimation.SlideIn)
            tasks.Add(AnimatedText.TranslateToAsync(0, 0, duration, easing));
        else if (Animation is TextAnimation.SlideUp or TextAnimation.RiseAndFade)
            tasks.Add(AnimatedText.TranslateToAsync(0, 0, duration, easing));
        else if (Animation is TextAnimation.ScaleIn or TextAnimation.BounceIn or TextAnimation.Zoom)
            tasks.Add(AnimatedText.ScaleToAsync(1, duration, easing));
        else if (Animation is TextAnimation.RotateIn or TextAnimation.FlipIn)
            tasks.Add(AnimatedText.RotateToAsync(0, duration, easing));
        await Task.WhenAll(tasks);
    }
}

public sealed class TextFade : TextCtrl { public TextFade() => Animation = TextAnimation.Fade; }
public sealed class SlideInText : TextCtrl { public SlideInText() => Animation = TextAnimation.SlideIn; }
public sealed class SlideUpText : TextCtrl { public SlideUpText() => Animation = TextAnimation.SlideUp; }
public sealed class ScaleInText : TextCtrl { public ScaleInText() => Animation = TextAnimation.ScaleIn; }
public sealed class RotateInText : TextCtrl { public RotateInText() => Animation = TextAnimation.RotateIn; }
public sealed class FlipInText : TextCtrl { public FlipInText() => Animation = TextAnimation.FlipIn; }
public sealed class BounceInText : TextCtrl { public BounceInText() => Animation = TextAnimation.BounceIn; }
public sealed class RiseAndFadeText : TextCtrl { public RiseAndFadeText() => Animation = TextAnimation.RiseAndFade; }
public sealed class ZoomInText : TextCtrl { public ZoomInText() => Animation = TextAnimation.Zoom; }
