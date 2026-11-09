namespace EEE.ClientApp.Pages;

public abstract class StaticInfoPage : ContentPage
{
    protected readonly RootLayout PageLayout;
    private readonly VerticalStackLayout _content;

    protected StaticInfoPage(string title, string path, string intro, params (string Title, string Body)[] sections)
    {
        Title = title;
        _content = new VerticalStackLayout { Padding = 18, Spacing = 14 };
        _content.Children.Add(new Label
        {
            Text = title,
            FontSize = 30,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#176B57")
        });
        _content.Children.Add(new Label { Text = intro, FontSize = 16 });
        foreach (var section in sections)
        {
            _content.Children.Add(new Border
            {
                Padding = 14,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Content = new VerticalStackLayout
                {
                    Spacing = 6,
                    Children =
                    {
                        new Label { Text = section.Title, FontSize = 18, FontAttributes = FontAttributes.Bold },
                        new Label { Text = section.Body }
                    }
                }
            });
        }
        PageLayout = new RootLayout
        {
            CurrentPath = path,
            PageContent = new ScrollView { Content = _content }
        };
        Content = PageLayout;
    }

    protected void AddExternalLink(string title, string path)
    {
        var button = new Button { Text = title };
        button.Clicked += async (_, _) =>
        {
            var target = path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? path
                : $"/{path.TrimStart('/')}";
            await PageLayout.NavigateToAsync(target);
        };
        _content.Children.Add(button);
    }

    protected void AddUiSample()
    {
        _content.Children.Add(new EEE.Components.UI.Badge { Text = "HealthClique" });
        _content.Children.Add(new Button { Text = "Continue browsing" });
    }
}

public sealed class AboutPage : StaticInfoPage
{
    public AboutPage() : base("About Healthclique Limited", "/about",
        "We believe access to safe, effective, and affordable medicines should be a standard. Healthclique Limited works to improve medicine access in Nigeria and underserved African communities.",
        ("Who we are", "Healthclique Limited is owned and managed by licensed pharmacists with expertise in pharmaceutical care, supply-chain management, and patient-centered service."),
        ("Our vision and promise", "We aim to become a trusted digital healthcare platform in Africa. From ordering to delivery, we work to provide a smooth, secure, and memorable experience."),
        ("Who we serve", "Individuals and families, healthcare professionals, and organizations seeking reliable pharmaceutical products and supply."),
        ("Quality and integrity", "We prioritize authentic medicines, fair pricing, secure supply chains, and compliance with applicable pharmaceutical regulations."),
        ("Technology", "Our platform supports prescription requests, product discovery, order management, and access to pharmacy support. It does not replace professional medical care."))
    {
        AddExternalLink("Contact HealthClique", "contact");
    }
}

public sealed class HelpPage : StaticInfoPage
{
    public HelpPage() : base("Help and support", "/help",
        "Find help with orders, prescriptions, delivery, and your HealthClique account.",
        ("Ordering", "Browse the store, add products to your cart, and complete checkout after signing in and selecting a delivery address."),
        ("Prescriptions", "Prescription-only products require a valid prescription. Use the prescription request options on the home page or contact the pharmacy team."),
        ("Delivery", "Delivery options and fees depend on the delivery address. Check your saved addresses and order details before checkout."),
        ("Account access", "Sign in to review your account, addresses, and saved orders. Contact support if you cannot access your account."),
        ("Need more help?", "Send a message to the HealthClique team or start a chat with support."))
    {
        AddExternalLink("Contact support", "contact");
        AddUiSample();
    }
}

public sealed class LunchPage : StaticInfoPage
{
    public LunchPage() : base("Lunch", "/lunch",
        "This section is currently informational. For product and prescription requests, use the store or contact our pharmacy team.",
        ("Explore products", "Browse HealthClique products and request assistance with prescription items."),
        ("Support", "Contact the team for current service availability and ordering assistance."))
    {
        AddExternalLink("Browse the store", "store");
        AddExternalLink("Contact support", "contact");
    }
}

public sealed class PrivacyPage : StaticInfoPage
{
    public PrivacyPage() : base("Privacy policy", "/privacy",
        "Healthclique Limited is committed to protecting personal data in line with applicable data-protection laws, including the Nigeria Data Protection Act 2023 and, where applicable, the GDPR.",
        ("Information collected", "Information may include contact details, delivery addresses, prescription information, transaction details, device information, and platform usage data."),
        ("How information is used", "Information is used to process orders and prescriptions, coordinate deliveries, communicate with customers, improve services, and meet legal obligations."),
        ("Sharing and retention", "Information may be shared with service providers, healthcare professionals, logistics partners, payment processors, or authorities where necessary. Data is retained only as long as needed for service and legal purposes."),
        ("Security and your choices", "We use appropriate safeguards, but no system can be guaranteed completely secure. Contact HealthClique to ask about your personal data or privacy rights."))
    {
        AddExternalLink("View the full privacy policy", "privacy");
        AddExternalLink("Contact privacy support", "contact");
    }
}

public sealed class TermsPage : StaticInfoPage
{
    public TermsPage() : base("Terms and conditions", "/terms",
        "Using HealthClique means agreeing to use the platform lawfully and provide accurate information. Users must be at least 18 or use the service under the supervision of a guardian.",
        ("Nature of services", "Healthclique Limited facilitates prescription fulfillment, medication sourcing, healthcare product distribution, and delivery coordination. The platform does not replace a healthcare provider."),
        ("Prescriptions and safety", "Prescription-only medicines are dispensed only after a valid prescription is received and validated. Do not misuse or alter prescriptions."),
        ("Orders and payments", "Orders are subject to product availability and regulatory clearance. Prices and estimated delivery timelines may change; payment processing is handled by approved providers."),
        ("Contact", "Contact HealthClique for questions about these terms, orders, or account use."))
    {
        AddExternalLink("View the full terms", "terms");
        AddExternalLink("Contact support", "contact");
    }
}

public sealed class MyUiPage : StaticInfoPage
{
    public MyUiPage() : base("UI component gallery", "/myui",
        "A small native gallery for checking the reusable MAUI controls used across the HealthClique app.",
        ("Buttons", "Actions use native touch targets and the app's shared color palette."),
        ("Cards and badges", "Content sections use rounded cards and compact status badges."),
        ("Forms", "Inputs, pickers, checkboxes, and date controls are used throughout native workflows."))
    {
        AddUiSample();
    }
}
