namespace EEE.Components.UI;

public partial class Card : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(Card), string.Empty, propertyChanged: OnHeaderChanged);
    public static readonly BindableProperty SubtitleProperty = BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(Card), string.Empty, propertyChanged: OnHeaderChanged);
    public static readonly BindableProperty HeaderContentProperty = BindableProperty.Create(nameof(HeaderContent), typeof(View), typeof(Card), propertyChanged: OnHeaderChanged);
    public static readonly BindableProperty BodyContentProperty = BindableProperty.Create(nameof(BodyContent), typeof(View), typeof(Card));
    public static readonly BindableProperty FooterContentProperty = BindableProperty.Create(nameof(FooterContent), typeof(View), typeof(Card));
    public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(nameof(BorderColor), typeof(Color), typeof(Card), null, propertyChanged: OnBorderColorChanged);
    public static readonly BindableProperty CardPaddingProperty = BindableProperty.Create(nameof(CardPadding), typeof(Thickness), typeof(Card), new Thickness(20));
    public static readonly BindableProperty HasHeaderProperty = BindableProperty.Create(nameof(HasHeader), typeof(bool), typeof(Card), false);

    public Card() => InitializeComponent();

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Subtitle { get => (string)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
    public View? HeaderContent { get => (View?)GetValue(HeaderContentProperty); set => SetValue(HeaderContentProperty, value); }
    public View? BodyContent { get => (View?)GetValue(BodyContentProperty); set => SetValue(BodyContentProperty, value); }
    public View? FooterContent { get => (View?)GetValue(FooterContentProperty); set => SetValue(FooterContentProperty, value); }
    public Color? BorderColor { get => (Color?)GetValue(BorderColorProperty); set => SetValue(BorderColorProperty, value); }
    public Thickness CardPadding { get => (Thickness)GetValue(CardPaddingProperty); set => SetValue(CardPaddingProperty, value); }
    public bool HasHeader { get => (bool)GetValue(HasHeaderProperty); private set => SetValue(HasHeaderProperty, value); }

    private static void OnHeaderChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var card = (Card)bindable;
        card.HasHeader = !string.IsNullOrWhiteSpace(card.Title)
            || !string.IsNullOrWhiteSpace(card.Subtitle)
            || card.HeaderContent is not null;
    }

    private static void OnBorderColorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var card = (Card)bindable;
        if (newValue is Color color && card.CardBorder is not null)
            card.CardBorder.Stroke = color;
    }
}
