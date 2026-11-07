namespace EEE.Components.Utility;

public partial class ProfileCards : ContentView
{
    public static readonly BindableProperty NameProperty = BindableProperty.Create(nameof(Name), typeof(string), typeof(ProfileCards), string.Empty);
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(ProfileCards), string.Empty);
    public static readonly BindableProperty ProfileImageProperty = BindableProperty.Create(nameof(ProfileImage), typeof(string), typeof(ProfileCards), string.Empty, propertyChanged: OnProfileImageChanged);

    public ProfileCards() => InitializeComponent();

    public string Name { get => (string)GetValue(NameProperty); set => SetValue(NameProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string ProfileImage { get => (string)GetValue(ProfileImageProperty); set => SetValue(ProfileImageProperty, value); }
    public ImageSource? ProfileImageSource
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ProfileImage))
                return null;
            return Uri.TryCreate(ProfileImage, UriKind.Absolute, out var uri)
                ? ImageSource.FromUri(uri)
                : ImageSource.FromFile(ProfileImage);
        }
    }

    private static void OnProfileImageChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ProfileCards)bindable).OnPropertyChanged(nameof(ProfileImageSource));
}

public sealed class ProfileCard : ProfileCards { }
