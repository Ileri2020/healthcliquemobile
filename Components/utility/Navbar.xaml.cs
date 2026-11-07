namespace EEE.Components.Utility;

public partial class Navbar : ContentView
{
    public static readonly BindableProperty BrandContentProperty =
        BindableProperty.Create(nameof(BrandContent), typeof(View), typeof(Navbar));
    public static readonly BindableProperty SideNavigationProperty =
        BindableProperty.Create(nameof(SideNavigation), typeof(View), typeof(Navbar));
    public static readonly BindableProperty NavigationContentProperty =
        BindableProperty.Create(nameof(NavigationContent), typeof(View), typeof(Navbar));
    public static readonly BindableProperty ActionsContentProperty =
        BindableProperty.Create(nameof(ActionsContent), typeof(View), typeof(Navbar));
    public static readonly BindableProperty BannerContentProperty =
        BindableProperty.Create(nameof(BannerContent), typeof(View), typeof(Navbar));

    public Navbar() => InitializeComponent();

    public View? BrandContent { get => (View?)GetValue(BrandContentProperty); set => SetValue(BrandContentProperty, value); }
    public View? SideNavigation { get => (View?)GetValue(SideNavigationProperty); set => SetValue(SideNavigationProperty, value); }
    public View? NavigationContent { get => (View?)GetValue(NavigationContentProperty); set => SetValue(NavigationContentProperty, value); }
    public View? ActionsContent { get => (View?)GetValue(ActionsContentProperty); set => SetValue(ActionsContentProperty, value); }
    public View? BannerContent { get => (View?)GetValue(BannerContentProperty); set => SetValue(BannerContentProperty, value); }
}
