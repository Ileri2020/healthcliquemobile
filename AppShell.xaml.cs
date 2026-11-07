using EEE.App.Pages;

namespace EEE;

public partial class AppShell : Shell
{
	public AppShell(HomePage homePage)
	{
		InitializeComponent();
		Routing.RegisterRoute("store", typeof(StorePage));
		Routing.RegisterRoute("product", typeof(ProductDetailsPage));
		Items.Add(new ShellContent
		{
			Title = "Home",
			Route = "home",
			Content = homePage
		});
	}
}
