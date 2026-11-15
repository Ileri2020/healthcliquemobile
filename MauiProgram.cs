using Microsoft.Extensions.Logging;
using EEE.ClientApp.Pages;
using EEE.ViewModels;
using UraniumUI;

namespace EEE;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseUraniumUI()
			.UseUraniumUIMaterial()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<MainViewModel>();
		builder.Services.AddSingleton<AppShell>();

		// Pages — all registered so Shell routing can resolve them via DI
		builder.Services.AddTransient<HomePage>();
		builder.Services.AddTransient<StorePage>();
		builder.Services.AddTransient<ProductDetailsPage>();
		builder.Services.AddTransient<AccountPage>();
		builder.Services.AddTransient<SalesPage>();
		builder.Services.AddTransient<StockPage>();
		builder.Services.AddTransient<BlogPage>();
		builder.Services.AddTransient<CartPage>();
		builder.Services.AddTransient<ContactPage>();
		builder.Services.AddTransient<CountPage>();
		builder.Services.AddTransient<ExpensesPage>();
		builder.Services.AddTransient<AdminPage>();
		builder.Services.AddTransient<AnalyticsPage>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<SignupPage>();
		builder.Services.AddTransient<StaffPage>();
		builder.Services.AddTransient<VitalsPage>();
		builder.Services.AddTransient<SheetPage>();
		builder.Services.AddTransient<AboutPage>();
		builder.Services.AddTransient<HelpPage>();
		builder.Services.AddTransient<LunchPage>();
		builder.Services.AddTransient<PrivacyPage>();
		builder.Services.AddTransient<TermsPage>();
		builder.Services.AddTransient<MyUiPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
