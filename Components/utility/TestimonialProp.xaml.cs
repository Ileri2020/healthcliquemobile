using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class TestimonialProp : ContentView
{
    public static readonly BindableProperty AuthorProperty = BindableProperty.Create(nameof(Author), typeof(TestimonialAuthor), typeof(TestimonialProp), defaultValueCreator: _ => new TestimonialAuthor(), propertyChanged: OnAuthorChanged);
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(TestimonialProp), string.Empty);
    public static readonly BindableProperty HrefProperty = BindableProperty.Create(nameof(Href), typeof(string), typeof(TestimonialProp), string.Empty, propertyChanged: OnHrefChanged);
    public static readonly BindableProperty NameProperty = BindableProperty.Create(nameof(Name), typeof(string), typeof(TestimonialProp), string.Empty);
    public static readonly BindableProperty HandleProperty = BindableProperty.Create(nameof(Handle), typeof(string), typeof(TestimonialProp), string.Empty);
    public static readonly BindableProperty AvatarProperty = BindableProperty.Create(nameof(Avatar), typeof(string), typeof(TestimonialProp), string.Empty);

    public TestimonialProp()
    {
        InitializeComponent();
        OpenLinkCommand = new Command(async () => await OpenLinkAsync(), () => !string.IsNullOrWhiteSpace(Href));
    }

    public TestimonialAuthor Author { get => (TestimonialAuthor)GetValue(AuthorProperty); set => SetValue(AuthorProperty, value); }
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string Href { get => (string)GetValue(HrefProperty); set => SetValue(HrefProperty, value); }
    public string Name { get => (string)GetValue(NameProperty); private set => SetValue(NameProperty, value); }
    public string Handle { get => (string)GetValue(HandleProperty); private set => SetValue(HandleProperty, value); }
    public string Avatar { get => (string)GetValue(AvatarProperty); private set => SetValue(AvatarProperty, value); }
    public bool HasLink => !string.IsNullOrWhiteSpace(Href);
    public ICommand OpenLinkCommand { get; }
    public event Action<object?, string>? NavigationRequested;

    private static void OnAuthorChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((TestimonialProp)bindable).UpdateAuthor((TestimonialAuthor)newValue);

    private static void OnHrefChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (TestimonialProp)bindable;
        control.OnPropertyChanged(nameof(HasLink));
        (control.OpenLinkCommand as Command)?.ChangeCanExecute();
    }

    private void UpdateAuthor(TestimonialAuthor author)
    {
        Name = author.Name;
        Handle = author.Handle;
        Avatar = author.Avatar;
    }

    private async Task OpenLinkAsync()
    {
        if (string.IsNullOrWhiteSpace(Href))
            return;
        NavigationRequested?.Invoke(this, Href);
        if (Uri.TryCreate(Href, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            await Launcher.Default.OpenAsync(uri);
    }
}

public sealed class TestimonialCard : TestimonialProp { }
