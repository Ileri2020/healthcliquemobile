using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.IO;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EEE.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		this.InitializeComponent();
		UnhandledException += OnUnhandledException;
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs args)
	{
		var details = $"{DateTimeOffset.Now:O}{Environment.NewLine}{args.Exception}{Environment.NewLine}{Environment.StackTrace}{Environment.NewLine}";
		Debug.WriteLine(details);
		try
		{
			var logDirectory = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"HealthClique");
			Directory.CreateDirectory(logDirectory);
			File.AppendAllText(Path.Combine(logDirectory, "windows-crash.log"), details);
		}
		catch (IOException exception)
		{
			Debug.WriteLine($"Unable to write the Windows crash log: {exception.Message}");
		}
		catch (UnauthorizedAccessException exception)
		{
			Debug.WriteLine($"Unable to write the Windows crash log: {exception.Message}");
		}
	}
}
