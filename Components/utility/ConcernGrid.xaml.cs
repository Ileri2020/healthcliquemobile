using System.Collections.ObjectModel;
using System.Windows.Input;

namespace EEE.Components.Utility;

public partial class ConcernGrid : ContentView
{
    public static readonly BindableProperty ColumnsProperty = BindableProperty.Create(nameof(Columns), typeof(int), typeof(ConcernGrid), 4);

    public ConcernGrid()
    {
        InitializeComponent();
        Concerns =
        [
            new() { Name = "Pain Relief", Category = "Pain Relief", IconText = "+" },
            new() { Name = "Cough, Cold & Flu", Category = "Cough, Cold & Flu", IconText = "✚" },
            new() { Name = "Mother & Kids", Category = "Baby & Child Health", IconText = "♡" },
            new() { Name = "Gut Health", Category = "Gut Health", IconText = "↗" },
            new() { Name = "Vitamins", Category = "Vitamins", IconText = "✿" },
            new() { Name = "His Health", Category = "His Health", IconText = "◉" },
            new() { Name = "Her Health", Category = "Her Health", IconText = "◎" },
            new() { Name = "Mental Wellness", Category = "Stress & Sleep", IconText = "〰" }
        ];
        SelectCommand = new Command<CommerceConcern>(SelectConcern);
    }

    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }
    public ObservableCollection<CommerceConcern> Concerns { get; }
    public ICommand SelectCommand { get; }
    public event Action<object?, CommerceConcern>? ConcernSelected;

    private void SelectConcern(CommerceConcern? concern)
    {
        if (concern is not null)
            ConcernSelected?.Invoke(this, concern);
    }
}
