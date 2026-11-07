using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class DataTable : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable<object>), typeof(DataTable), propertyChanged: OnSourceChanged);
    public static readonly BindableProperty ColumnsProperty = BindableProperty.Create(nameof(Columns), typeof(IEnumerable<DiscoveryDataColumn>), typeof(DataTable), propertyChanged: OnColumnsChanged);
    public static readonly BindableProperty SearchTextProperty = BindableProperty.Create(nameof(SearchText), typeof(string), typeof(DataTable), string.Empty, BindingMode.TwoWay, propertyChanged: OnSearchChanged);
    public static readonly BindableProperty PageSizeProperty = BindableProperty.Create(nameof(PageSize), typeof(int), typeof(DataTable), 10, propertyChanged: OnPageSizeChanged);
    public static readonly BindableProperty CurrentPageProperty = BindableProperty.Create(nameof(CurrentPage), typeof(int), typeof(DataTable), 1);
    public static readonly BindableProperty PageSummaryProperty = BindableProperty.Create(nameof(PageSummary), typeof(string), typeof(DataTable), "Page 1 of 1");
    public static readonly BindableProperty CanGoPreviousProperty = BindableProperty.Create(nameof(CanGoPrevious), typeof(bool), typeof(DataTable), false);
    public static readonly BindableProperty CanGoNextProperty = BindableProperty.Create(nameof(CanGoNext), typeof(bool), typeof(DataTable), false);
    public static readonly BindableProperty SortPropertyProperty = BindableProperty.Create(nameof(SortProperty), typeof(string), typeof(DataTable), string.Empty);
    public static readonly BindableProperty SortAscendingProperty = BindableProperty.Create(nameof(SortAscending), typeof(bool), typeof(DataTable), true);
    private List<DiscoveryDataColumn>? _generatedColumns;
    private Type? _generatedColumnType;
    private bool _isUpdatingGeneratedColumns;

    public DataTable()
    {
        InitializeComponent();
        PageRows = [];
        Columns = [];
        ClearCommand = new Command(() => SearchText = string.Empty);
        PreviousCommand = new Command(() => { CurrentPage--; RefreshRows(); }, () => CanGoPrevious);
        NextCommand = new Command(() => { CurrentPage++; RefreshRows(); }, () => CanGoNext);
        SortCommand = new Command<DiscoveryDataColumn>(SortBy);
        SelectRowCommand = new Command<DiscoveryDataTableRow>(row =>
        {
            if (row?.Source is not null)
                RowSelected?.Invoke(this, row.Source);
        });
        Loaded += (_, _) => RefreshRows();
    }

    public IEnumerable<object> ItemsSource { get => (IEnumerable<object>?)GetValue(ItemsSourceProperty) ?? []; set => SetValue(ItemsSourceProperty, value); }
    public IEnumerable<DiscoveryDataColumn> Columns { get => (IEnumerable<DiscoveryDataColumn>?)GetValue(ColumnsProperty) ?? []; set => SetValue(ColumnsProperty, value); }
    public string SearchText { get => (string)GetValue(SearchTextProperty); set => SetValue(SearchTextProperty, value); }
    public int PageSize { get => (int)GetValue(PageSizeProperty); set => SetValue(PageSizeProperty, value); }
    public int CurrentPage { get => (int)GetValue(CurrentPageProperty); private set => SetValue(CurrentPageProperty, value); }
    public string PageSummary { get => (string)GetValue(PageSummaryProperty); private set => SetValue(PageSummaryProperty, value); }
    public bool CanGoPrevious { get => (bool)GetValue(CanGoPreviousProperty); private set => SetValue(CanGoPreviousProperty, value); }
    public bool CanGoNext { get => (bool)GetValue(CanGoNextProperty); private set => SetValue(CanGoNextProperty, value); }
    public string SortProperty { get => (string)GetValue(SortPropertyProperty); private set => SetValue(SortPropertyProperty, value); }
    public bool SortAscending { get => (bool)GetValue(SortAscendingProperty); private set => SetValue(SortAscendingProperty, value); }
    public ObservableCollection<DiscoveryDataTableRow> PageRows { get; }
    public ICommand ClearCommand { get; }
    public ICommand PreviousCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand SortCommand { get; }
    public ICommand SelectRowCommand { get; }
    public event Action<object?, object>? RowSelected;

    private static void OnSourceChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((DataTable)bindable).RefreshRows();

    private static void OnColumnsChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((DataTable)bindable).OnColumnsChanged();

    private static void OnSearchChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var table = (DataTable)bindable;
        table.CurrentPage = 1;
        table.RefreshRows();
    }

    private static void OnPageSizeChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((DataTable)bindable).RefreshRows();

    private void OnColumnsChanged()
    {
        if (!_isUpdatingGeneratedColumns)
            RefreshRows();
    }

    private void SortBy(DiscoveryDataColumn? column)
    {
        if (column is null)
            return;
        SortAscending = SortProperty == column.Key && !SortAscending;
        SortProperty = column.Key;
        CurrentPage = 1;
        RefreshRows();
    }

    private void RefreshRows()
    {
        var source = ItemsSource.ToList();
        var columns = Columns.ToList();
        if (columns.Count == 0 && source.FirstOrDefault() is { } first)
        {
            if (_generatedColumnType != first.GetType())
            {
                _generatedColumnType = first.GetType();
                _generatedColumns = _generatedColumnType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                    .Select(property => new DiscoveryDataColumn(property.Name, property.Name))
                    .ToList();
            }
            columns = _generatedColumns;
            _isUpdatingGeneratedColumns = true;
            try
            {
                Columns = columns;
            }
            finally
            {
                _isUpdatingGeneratedColumns = false;
            }
        }

        var query = SearchText.Trim();
        if (query.Length > 0)
            source = source.Where(item => columns.Any(column => ReadValue(item, column.Key).Contains(query, StringComparison.OrdinalIgnoreCase))).ToList();

        if (!string.IsNullOrWhiteSpace(SortProperty))
        {
            source = SortAscending
                ? source.OrderBy(item => ReadValue(item, SortProperty), StringComparer.OrdinalIgnoreCase).ToList()
                : source.OrderByDescending(item => ReadValue(item, SortProperty), StringComparer.OrdinalIgnoreCase).ToList();
        }

        var pageSize = Math.Max(1, PageSize);
        var pageCount = Math.Max(1, (int)Math.Ceiling(source.Count / (double)pageSize));
        CurrentPage = Math.Clamp(CurrentPage, 1, pageCount);
        PageRows.Clear();
        foreach (var item in source.Skip((CurrentPage - 1) * pageSize).Take(pageSize))
        {
            PageRows.Add(new DiscoveryDataTableRow
            {
                Source = item,
                Cells = columns.Select(column => new DiscoveryDataTableCell
                {
                    Text = ReadValue(item, column.Key),
                    SortValue = ReadValue(item, column.Key)
                }).ToList()
            });
        }

        PageSummary = $"Page {CurrentPage} of {pageCount}  |  {source.Count} rows";
        CanGoPrevious = CurrentPage > 1;
        CanGoNext = CurrentPage < pageCount;
        (PreviousCommand as Command)?.ChangeCanExecute();
        (NextCommand as Command)?.ChangeCanExecute();
    }

    private static string ReadValue(object item, string key)
    {
        object? value = item;
        foreach (var segment in key.Split('.'))
        {
            if (value is null)
                return string.Empty;
            if (value is System.Collections.IDictionary dictionary)
            {
                value = dictionary.Contains(segment) ? dictionary[segment] : null;
                continue;
            }
            var property = value.GetType().GetProperty(segment, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            value = property?.GetValue(value);
        }
        return value switch
        {
            null => string.Empty,
            DateTime date => date.ToLocalTime().ToString("g"),
            System.Collections.IEnumerable enumerable when value is not string => string.Join(", ", enumerable.Cast<object?>()),
            _ => value.ToString() ?? string.Empty
        };
    }
}
