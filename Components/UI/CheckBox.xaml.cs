using System.Windows.Input;

namespace EEE.Components.UI;

public partial class CheckBox : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(CheckBox), string.Empty);
    public static readonly BindableProperty IsCheckedProperty =
        BindableProperty.Create(nameof(IsChecked), typeof(bool), typeof(CheckBox), false, BindingMode.TwoWay);
    public static readonly BindableProperty IsEnabledProperty =
        BindableProperty.Create(nameof(IsEnabled), typeof(bool), typeof(CheckBox), true);

    public CheckBox()
    {
        InitializeComponent();
        ToggleCommand = new Command(() =>
        {
            if (IsEnabled)
                IsChecked = !IsChecked;
        });
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public bool IsChecked { get => (bool)GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }
    public new bool IsEnabled { get => (bool)GetValue(IsEnabledProperty); set => SetValue(IsEnabledProperty, value); }
    public ICommand ToggleCommand { get; }
}
