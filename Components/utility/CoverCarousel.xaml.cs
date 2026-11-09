using System.Collections.ObjectModel;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class CoverCarousel : ContentView
{
    public static readonly BindableProperty NumCardsProperty = BindableProperty.Create(nameof(NumCards), typeof(int), typeof(CoverCarousel), 0, propertyChanged: OnNumCardsChanged);
    public static readonly BindableProperty CarouselHeightProperty = BindableProperty.Create(nameof(CarouselHeight), typeof(double), typeof(CoverCarousel), 400d);
    public static readonly BindableProperty CardWidthProperty = BindableProperty.Create(nameof(CardWidth), typeof(double), typeof(CoverCarousel), 320d);
    public static readonly BindableProperty CurrentIndexProperty = BindableProperty.Create(nameof(CurrentIndex), typeof(int), typeof(CoverCarousel), 0);
    public static readonly BindableProperty HasCardsProperty = BindableProperty.Create(nameof(HasCards), typeof(bool), typeof(CoverCarousel), false);

    public CoverCarousel()
    {
        InitializeComponent();
        Cards = [];
        PreviousCommand = new Command(Previous);
        NextCommand = new Command(Next);
        RebuildCards();
    }

    public int NumCards { get => (int)GetValue(NumCardsProperty); set => SetValue(NumCardsProperty, value); }
    public double CarouselHeight { get => (double)GetValue(CarouselHeightProperty); set => SetValue(CarouselHeightProperty, value); }
    public double CardWidth { get => (double)GetValue(CardWidthProperty); set => SetValue(CardWidthProperty, value); }
    public int CurrentIndex { get => (int)GetValue(CurrentIndexProperty); private set => SetValue(CurrentIndexProperty, value); }
    public bool HasCards { get => (bool)GetValue(HasCardsProperty); private set => SetValue(HasCardsProperty, value); }
    public ObservableCollection<string> Cards { get; }
    public ICommand PreviousCommand { get; }
    public ICommand NextCommand { get; }

    private static void OnNumCardsChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((CoverCarousel)bindable).RebuildCards();

    private void RebuildCards()
    {
        Cards.Clear();
        for (var index = 0; index < Math.Max(0, NumCards); index++)
            Cards.Add($"Card {index + 1}");
        HasCards = Cards.Count > 0;
        CurrentIndex = HasCards ? CurrentIndex % Cards.Count : 0;
        if (HasCards)
            CardCollection.ScrollTo(CurrentIndex, position: ScrollToPosition.Start, animate: false);
    }

    private void Previous()
    {
        if (Cards.Count > 0)
        {
            CurrentIndex = (CurrentIndex - 1 + Cards.Count) % Cards.Count;
            CardCollection.ScrollTo(CurrentIndex, position: ScrollToPosition.Start, animate: true);
        }
    }

    private void Next()
    {
        if (Cards.Count > 0)
        {
            CurrentIndex = (CurrentIndex + 1) % Cards.Count;
            CardCollection.ScrollTo(CurrentIndex, position: ScrollToPosition.Start, animate: true);
        }
    }
}
