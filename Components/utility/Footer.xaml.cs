using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class Footer : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(Footer), string.Empty, propertyChanged: OnApiBaseUrlChanged);
    public static readonly BindableProperty CurrentPathProperty = BindableProperty.Create(nameof(CurrentPath), typeof(string), typeof(Footer), "/", propertyChanged: OnPathChanged);
    public static readonly BindableProperty SiteNameProperty = BindableProperty.Create(nameof(SiteName), typeof(string), typeof(Footer), "Health Clique");
    public static readonly BindableProperty CopyrightTextProperty = BindableProperty.Create(nameof(CopyrightText), typeof(string), typeof(Footer), $"© {DateTime.Now.Year} Health Clique. All rights reserved.");
    public static readonly BindableProperty ShowFooterProperty = BindableProperty.Create(nameof(ShowFooter), typeof(bool), typeof(Footer), true);
    public static readonly BindableProperty IsAffiliateFormOpenProperty = BindableProperty.Create(nameof(IsAffiliateFormOpen), typeof(bool), typeof(Footer), false);
    public static readonly BindableProperty AffiliateNameProperty = BindableProperty.Create(nameof(AffiliateName), typeof(string), typeof(Footer), string.Empty);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(Footer), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(Footer), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(Footer), string.Empty);

    public Footer()
    {
        InitializeComponent();
        Categories = [];
        NavigateCommand = new Command<string>(path =>
        {
            if (!string.IsNullOrWhiteSpace(path))
                NavigationRequested?.Invoke(this, path);
        });
        CategoryCommand = new Command<DiscoveryCategory>(category =>
        {
            if (category is not null)
                CategorySelected?.Invoke(this, category);
        });
        ToggleAffiliateCommand = new Command(() => IsAffiliateFormOpen = !IsAffiliateFormOpen);
        AffiliateCommand = new Command(async () => await ApplyForAffiliateAsync(), () => !IsLoading);
        Loaded += async (_, _) => await LoadCategoriesAsync();
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public string CurrentPath { get => (string)GetValue(CurrentPathProperty); set => SetValue(CurrentPathProperty, value); }
    public string SiteName { get => (string)GetValue(SiteNameProperty); set => SetValue(SiteNameProperty, value); }
    public string CopyrightText { get => (string)GetValue(CopyrightTextProperty); set => SetValue(CopyrightTextProperty, value); }
    public bool ShowFooter { get => (bool)GetValue(ShowFooterProperty); private set => SetValue(ShowFooterProperty, value); }
    public bool IsAffiliateFormOpen { get => (bool)GetValue(IsAffiliateFormOpenProperty); private set => SetValue(IsAffiliateFormOpenProperty, value); }
    public string AffiliateName { get => (string)GetValue(AffiliateNameProperty); set => SetValue(AffiliateNameProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public ObservableCollection<DiscoveryCategory> Categories { get; }
    public ICommand NavigateCommand { get; }
    public ICommand CategoryCommand { get; }
    public ICommand ToggleAffiliateCommand { get; }
    public ICommand AffiliateCommand { get; }
    public event Action<object?, string>? NavigationRequested;
    public event Action<object?, DiscoveryCategory>? CategorySelected;
    public event Action<object?, Exception>? RequestFailed;

    private static void OnApiBaseUrlChanged(BindableObject bindable, object oldValue, object newValue) =>
        _ = ((Footer)bindable).LoadCategoriesAsync();

    private static void OnPathChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var footer = (Footer)bindable;
        var path = footer.CurrentPath.Trim('/');
        footer.ShowFooter = !(path.Equals("stock", StringComparison.OrdinalIgnoreCase)
            || path.Equals("sales", StringComparison.OrdinalIgnoreCase)
            || path.Equals("expenses", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("stock/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("sales/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("expenses/", StringComparison.OrdinalIgnoreCase));
    }

    private async Task LoadCategoriesAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiBaseUrl) || IsLoading)
            return;
        IsLoading = true;
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=category&limit=5");
            using var response = await UtilityApi.SendAsync(HttpMethod.Get, uri, (object?)null);
            var categories = await response.Content.ReadFromJsonAsync<List<DiscoveryCategory>>(JsonOptions)
                ?? throw new JsonException("Footer category API returned an invalid response.");
            Categories.Clear();
            foreach (var category in categories.Take(5))
                Categories.Add(category);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ApplyForAffiliateAsync()
    {
        if (string.IsNullOrWhiteSpace(AffiliateName) || AffiliateName.Trim().Length < 3)
        {
            SetError(new InvalidOperationException("Enter an affiliate name with at least three characters."));
            return;
        }
        IsLoading = true;
        HasStatus = false;
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, "api/affiliate");
            using var response = await UtilityApi.SendAsync(HttpMethod.Post, uri, new { name = AffiliateName.Trim() });
            var result = await response.Content.ReadFromJsonAsync<AffiliateResponse>(JsonOptions)
                ?? throw new JsonException("Affiliate API returned an invalid response.");
            if (!result.Success)
                throw new InvalidOperationException("Affiliate application was not accepted.");
            AffiliateName = string.Empty;
            IsAffiliateFormOpen = false;
            StatusMessage = "Affiliate application submitted.";
            HasStatus = true;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
            (AffiliateCommand as Command)?.ChangeCanExecute();
        }
    }

    private void SetError(Exception exception)
    {
        StatusMessage = exception.Message;
        HasStatus = true;
        RequestFailed?.Invoke(this, exception);
    }

    private sealed class AffiliateResponse
    {
        public bool Success { get; set; }
    }
}
