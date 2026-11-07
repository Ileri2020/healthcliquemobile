using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class FeaturedCategories : ContentView
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private FileResult? _imageFile;
    private DiscoveryCategory? _editingCategory;
    public static readonly BindableProperty ApiBaseUrlProperty = BindableProperty.Create(nameof(ApiBaseUrl), typeof(string), typeof(FeaturedCategories), string.Empty, propertyChanged: OnApiBaseUrlChanged);
    public static readonly BindableProperty IsAdminProperty = BindableProperty.Create(nameof(IsAdmin), typeof(bool), typeof(FeaturedCategories), false, propertyChanged: OnAdminChanged);
    public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(nameof(IsLoading), typeof(bool), typeof(FeaturedCategories), false);
    public static readonly BindableProperty HasStatusProperty = BindableProperty.Create(nameof(HasStatus), typeof(bool), typeof(FeaturedCategories), false);
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(FeaturedCategories), string.Empty);
    public static readonly BindableProperty HasCategoriesProperty = BindableProperty.Create(nameof(HasCategories), typeof(bool), typeof(FeaturedCategories), false);
    public static readonly BindableProperty IsExpandedProperty = BindableProperty.Create(nameof(IsExpanded), typeof(bool), typeof(FeaturedCategories), false, propertyChanged: OnExpandedChanged);
    public static readonly BindableProperty IsEditingProperty = BindableProperty.Create(nameof(IsEditing), typeof(bool), typeof(FeaturedCategories), false);
    public static readonly BindableProperty EditNameProperty = BindableProperty.Create(nameof(EditName), typeof(string), typeof(FeaturedCategories), string.Empty);
    public static readonly BindableProperty EditDescriptionProperty = BindableProperty.Create(nameof(EditDescription), typeof(string), typeof(FeaturedCategories), string.Empty);
    public static readonly BindableProperty SelectedFileNameProperty = BindableProperty.Create(nameof(SelectedFileName), typeof(string), typeof(FeaturedCategories), "No image selected");
    public static readonly BindableProperty EditorTitleProperty = BindableProperty.Create(nameof(EditorTitle), typeof(string), typeof(FeaturedCategories), "Add category");
    public static readonly BindableProperty ExpandTextProperty = BindableProperty.Create(nameof(ExpandText), typeof(string), typeof(FeaturedCategories), "See all");

    public FeaturedCategories()
    {
        InitializeComponent();
        Categories = [];
        VisibleCategories = [];
        RefreshCommand = new Command(async () => await LoadAsync());
        NewCommand = new Command(BeginCreate, () => IsAdmin && !IsLoading);
        EditCommand = new Command<DiscoveryCategory>(BeginEdit, category => IsAdmin && category is not null && !IsLoading);
        DeleteCommand = new Command<DiscoveryCategory>(async category => await DeleteAsync(category), category => IsAdmin && category is not null && !IsLoading);
        SelectCommand = new Command<DiscoveryCategory>(category =>
        {
            if (category is not null)
                CategorySelected?.Invoke(this, category);
        });
        ToggleExpandedCommand = new Command(() => IsExpanded = !IsExpanded);
        PickImageCommand = new Command(async () => await PickImageAsync());
        SaveCommand = new Command(async () => await SaveAsync(), () => IsAdmin && !IsLoading);
        CancelCommand = new Command(CancelEdit);
        Loaded += async (_, _) => await LoadAsync();
    }

    public string ApiBaseUrl { get => (string)GetValue(ApiBaseUrlProperty); set => SetValue(ApiBaseUrlProperty, value); }
    public bool IsAdmin { get => (bool)GetValue(IsAdminProperty); set => SetValue(IsAdminProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); private set => SetValue(IsLoadingProperty, value); }
    public bool HasStatus { get => (bool)GetValue(HasStatusProperty); private set => SetValue(HasStatusProperty, value); }
    public string StatusMessage { get => (string)GetValue(StatusMessageProperty); private set => SetValue(StatusMessageProperty, value); }
    public bool HasCategories { get => (bool)GetValue(HasCategoriesProperty); private set => SetValue(HasCategoriesProperty, value); }
    public bool IsExpanded { get => (bool)GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }
    public bool IsEditing { get => (bool)GetValue(IsEditingProperty); private set => SetValue(IsEditingProperty, value); }
    public string EditName { get => (string)GetValue(EditNameProperty); set => SetValue(EditNameProperty, value); }
    public string EditDescription { get => (string)GetValue(EditDescriptionProperty); set => SetValue(EditDescriptionProperty, value); }
    public string SelectedFileName { get => (string)GetValue(SelectedFileNameProperty); private set => SetValue(SelectedFileNameProperty, value); }
    public string EditorTitle { get => (string)GetValue(EditorTitleProperty); private set => SetValue(EditorTitleProperty, value); }
    public string ExpandText { get => (string)GetValue(ExpandTextProperty); private set => SetValue(ExpandTextProperty, value); }
    public ObservableCollection<CategoryCardView> Categories { get; }
    public ObservableCollection<CategoryCardView> VisibleCategories { get; }
    public ICommand RefreshCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SelectCommand { get; }
    public ICommand ToggleExpandedCommand { get; }
    public ICommand PickImageCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public event Action<object?, DiscoveryCategory>? CategorySelected;
    public event Action<object?, Exception>? RequestFailed;

    private static void OnApiBaseUrlChanged(BindableObject bindable, object oldValue, object newValue) =>
        _ = ((FeaturedCategories)bindable).LoadAsync();

    private static void OnAdminChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((FeaturedCategories)bindable).UpdateCommands();

    private static void OnExpandedChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (FeaturedCategories)bindable;
        control.ExpandText = (bool)newValue ? "Show fewer" : "See all";
        control.RefreshVisibleCategories();
    }

    private async Task LoadAsync()
    {
        if (IsLoading || string.IsNullOrWhiteSpace(ApiBaseUrl))
            return;
        IsLoading = true;
        HasStatus = false;
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, "api/dbhandler?model=category&limit=100");
            using var response = await UtilityApi.SendAsync(HttpMethod.Get, uri, (object?)null);
            var values = await response.Content.ReadFromJsonAsync<List<DiscoveryCategory>>(JsonOptions)
                ?? throw new JsonException("Category API returned an invalid response.");
            Categories.Clear();
            foreach (var category in values)
                Categories.Add(new CategoryCardView(category));
            HasCategories = Categories.Count > 0;
            RefreshVisibleCategories();
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
            UpdateCommands();
        }
    }

    private void RefreshVisibleCategories()
    {
        VisibleCategories.Clear();
        foreach (var category in IsExpanded ? Categories : Categories.Take(6))
            VisibleCategories.Add(category);
    }

    private void BeginCreate()
    {
        _editingCategory = null;
        EditorTitle = "Add category";
        EditName = string.Empty;
        EditDescription = string.Empty;
        _imageFile = null;
        SelectedFileName = "No image selected";
        IsEditing = true;
    }

    private void BeginEdit(DiscoveryCategory? category)
    {
        if (category is null)
            return;
        _editingCategory = category;
        EditorTitle = "Edit category";
        EditName = category.Name;
        EditDescription = category.Description ?? string.Empty;
        _imageFile = null;
        SelectedFileName = string.IsNullOrWhiteSpace(category.Image) ? "No image selected" : "Using current category image";
        IsEditing = true;
    }

    private async Task PickImageAsync()
    {
        try
        {
            _imageFile = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a category image",
                FileTypes = FilePickerFileType.Images
            });
            SelectedFileName = _imageFile?.FileName ?? "No image selected";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or OperationCanceledException)
        {
            if (exception is not OperationCanceledException)
                SetError(exception);
        }
    }

    private async Task SaveAsync()
    {
        if (!IsAdmin)
        {
            SetError(new InvalidOperationException("Administrator access is required."));
            return;
        }
        if (string.IsNullOrWhiteSpace(EditName))
        {
            SetError(new InvalidOperationException("Enter a category name."));
            return;
        }

        IsLoading = true;
        HasStatus = false;
        try
        {
            var creating = _editingCategory is null;
            var method = creating ? HttpMethod.Post : HttpMethod.Put;
            var path = creating
                ? "api/dbhandler?model=category"
                : $"api/dbhandler?model=category&id={Uri.EscapeDataString(_editingCategory!.Id)}";
            var payload = creating
                ? (object)new { name = EditName.Trim(), description = EditDescription.Trim() }
                : new { name = EditName.Trim(), description = EditDescription.Trim() };
            using var response = _imageFile is null
                ? await UtilityApi.SendAsync(method, UtilityApi.CreateUri(ApiBaseUrl, path), payload)
                : await UtilityApi.SendFileAsync(method, UtilityApi.CreateUri(ApiBaseUrl, path), _imageFile, CreateCategoryFields());
            var saved = await response.Content.ReadFromJsonAsync<DiscoveryCategory>(JsonOptions)
                ?? throw new JsonException("Category API returned an invalid saved record.");
            ReplaceCategory(saved);
            HasCategories = Categories.Count > 0;
            RefreshVisibleCategories();
            CancelEdit();
            StatusMessage = creating ? "Category created." : "Category updated.";
            HasStatus = true;
        }

        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
            UpdateCommands();
        }
    }

    private Dictionary<string, string> CreateCategoryFields()
    {
        var fields = new Dictionary<string, string>
        {
            ["name"] = EditName.Trim(),
            ["description"] = EditDescription.Trim()
        };
        return fields;
    }

    private async Task DeleteAsync(DiscoveryCategory? category)
    {
        if (!IsAdmin || category is null)
            return;
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        var confirmed = page is null || await page.DisplayAlert("Delete category", $"Delete {category.Name}?", "Delete", "Cancel");
        if (!confirmed)
            return;
        IsLoading = true;
        try
        {
            var uri = UtilityApi.CreateUri(ApiBaseUrl, $"api/dbhandler?model=category&id={Uri.EscapeDataString(category.Id)}");
            using var response = await UtilityApi.SendAsync(HttpMethod.Delete, uri, (object?)null);
            var card = Categories.FirstOrDefault(item => item.Category.Id == category.Id);
            if (card is not null)
                Categories.Remove(card);
            HasCategories = Categories.Count > 0;
            RefreshVisibleCategories();
            StatusMessage = "Category deleted.";
            HasStatus = true;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            SetError(exception);
        }
        finally
        {
            IsLoading = false;
            UpdateCommands();
        }
    }

    private void ReplaceCategory(DiscoveryCategory saved)
    {
        var existing = Categories.FirstOrDefault(item => item.Category.Id == saved.Id);
        if (existing is not null)
        {
            var index = Categories.IndexOf(existing);
            Categories[index] = new CategoryCardView(saved);
        }
        else
            Categories.Insert(0, new CategoryCardView(saved));
    }

    private void CancelEdit()
    {
        IsEditing = false;
        _editingCategory = null;
        _imageFile = null;
    }

    private void UpdateCommands()
    {
        (NewCommand as Command)?.ChangeCanExecute();
        (EditCommand as Command<DiscoveryCategory>)?.ChangeCanExecute();
        (DeleteCommand as Command<DiscoveryCategory>)?.ChangeCanExecute();
        (SaveCommand as Command)?.ChangeCanExecute();
    }

    private void SetError(Exception exception)
    {
        StatusMessage = exception.Message;
        HasStatus = true;
        RequestFailed?.Invoke(this, exception);
    }

    public sealed class CategoryCardView
    {
        public CategoryCardView(DiscoveryCategory category)
        {
            Category = category;
            Name = category.Name;
            Description = category.Description ?? string.Empty;
            ImageUrl = category.ImageUrl;
            CountText = $"{category.ProductCount} products";
        }
        public DiscoveryCategory Category { get; }
        public string Name { get; }
        public string Description { get; }
        public string ImageUrl { get; }
        public string CountText { get; }
    }
}
