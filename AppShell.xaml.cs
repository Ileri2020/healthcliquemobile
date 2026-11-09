using EEE.ClientApp.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace EEE;

public partial class AppShell : Shell
{
	public AppShell(IServiceProvider services)
	{
		InitializeComponent();
		Routing.RegisterRoute("store", typeof(StorePage));
		Routing.RegisterRoute("product", typeof(ProductDetailsPage));
		Routing.RegisterRoute("account", typeof(AccountPage));
		Routing.RegisterRoute("sales", typeof(SalesPage));
		Routing.RegisterRoute("stock", typeof(StockPage));
		Routing.RegisterRoute("blog", typeof(BlogPage));
		Routing.RegisterRoute("cart", typeof(CartPage));
		Routing.RegisterRoute("contact", typeof(ContactPage));
		Routing.RegisterRoute("count", typeof(CountPage));
		Routing.RegisterRoute("expenses", typeof(ExpensesPage));
		Routing.RegisterRoute("admin", typeof(AdminPage));
		Routing.RegisterRoute("analytics", typeof(AnalyticsPage));
		Routing.RegisterRoute("login", typeof(LoginPage));
		Routing.RegisterRoute("signup", typeof(SignupPage));
		Routing.RegisterRoute("staff", typeof(StaffPage));
		Routing.RegisterRoute("vitals", typeof(VitalsPage));
		Routing.RegisterRoute("sheet", typeof(SheetPage));
		Routing.RegisterRoute("about", typeof(AboutPage));
		Routing.RegisterRoute("help", typeof(HelpPage));
		Routing.RegisterRoute("lunch", typeof(LunchPage));
		Routing.RegisterRoute("privacy", typeof(PrivacyPage));
		Routing.RegisterRoute("terms", typeof(TermsPage));
		Routing.RegisterRoute("myui", typeof(MyUiPage));
		Items.Add(new ShellContent
		{
			Title = "Home",
			Route = "home",
			ContentTemplate = new DataTemplate(() => services.GetRequiredService<HomePage>())
		});
	}
}
