# EEE

A .NET MAUI application targeting Android and Windows desktop, using UraniumUI Material and CommunityToolkit.Mvvm.

## Requirements

- .NET 10 SDK
- .NET MAUI workload
- Android: Android SDK and an emulator or connected device
- Windows desktop: Windows 10 version 1809 (build 17763) or later

## Run on Windows

In VS Code, run the **EEE: Run Windows** task. From a terminal in the project directory, run `dotnet run --framework net10.0-windows10.0.19041.0`.

## Build

Run `devrunners\buildwindows.bat` to publish the Windows executable, or `devrunners\builandroid.bat` to publish the Android APK. Both artifacts are placed under `Platforms\dist`. Android publishing requires the Android SDK platform/build tools and an accepted SDK license. Configure an Android device/emulator to deploy and run the APK.

### GitHub Actions builds

The workflows in `.github\workflows` build and upload Android and Windows desktop artifacts on pushes and pull requests. The Android workflow produces an ARM64 APK. The Windows installer workflow runs manually from the Actions tab or on a `v*` tag and uploads an unsigned MSIX package. Installing an unsigned MSIX requires trusting a signing certificate; configure signing before distributing an installer to end users.

The shared page shell is in `App\RootLayout.xaml`. It composes the reusable `Navbar`, `Sidenav`, and `Footer` from `Components\Utility`; pages can place their own content inside it and set `ApiBaseUrl` and `CurrentPath` as needed. The default API and web-navigation origin is `https://healthcliquecare.com/`. Route-shaped folders under `App` mirror the web app's `app` routes and are scaffolding for the corresponding MAUI pages. The native home screen is in `App\home\HomePage.xaml` and its data sections use the shared HealthClique API. Register new `ContentPage` types and Shell routes in `AppShell` and their services in `MauiProgram.cs`.

```xml
xmlns:app="clr-namespace:EEE.App"

<app:RootLayout ApiBaseUrl="{Binding ApiBaseUrl}" CurrentPath="/store">
    <!-- Page-specific content -->
</app:RootLayout>
```

The home screen in `App\home\HomePage.xaml` follows the web discovery-page section order: hero/search, curated medications, rapid-sale deals, partner brands, health concerns, categories, ingredients, featured products, and service benefits. Its search, category, brand, ingredient, deal, and product controls use the `ApiBaseUrl` from `RootLayout`; native product detail and other unimplemented routes open the corresponding HealthClique web page. Prescription analysis is handled by the shared server-side endpoint.

## Shared UI components

Reusable controls live in `Components\UI` and can be used from XAML with:

```xml
xmlns:ui="clr-namespace:EEE.Components.UI"
```

For example:

```xml
<ui:Input Placeholder="Your name" Text="{Binding Name}" />
<ui:Button Text="Save" Command="{Binding SaveCommand}" />
<ui:Card Title="Details">
    <ui:Card.BodyContent>
        <ui:CardContent>
            <ui:Label Text="Card content goes here." />
        </ui:CardContent>
    </ui:Card.BodyContent>
</ui:Card>
```

The component namespace exports the same named compound parts as the web UI library, adapted to MAUI XAML:

- `Card`: `CardHeader`, `CardTitle`, `CardDescription`, `CardContent`, `CardBody`, `CardFooter`.
- `Table`: `TableHeader`, `TableBody`, `TableFooter`, `TableRow`, `TableHead`, `TableCell`, `TableCaption`. Set the root's `HeaderContent`, `BodyContent`, `FooterContent`, or `CaptionContent` for a composed table; `ItemsSource` and `ItemTemplate` remain available for generated rows.
- `Select`: `SelectGroup`, `SelectValue`, `SelectTrigger`, `SelectContent`, `SelectLabel`, `SelectItem`, `SelectSeparator`, `SelectScrollUpButton`, `SelectScrollDownButton`. The root `CustomContent` slot accepts a composed select; its default control is UraniumUI Material `DropdownField`.
- `Dialog`: `DialogPortal`, `DialogOverlay`, `DialogClose`, `DialogTrigger`, `DialogContent`, `DialogHeader`, `DialogFooter`, `DialogTitle`, `DialogDescription`.
- `Carousel`: `CarouselApi`, `CarouselContent`, `CarouselItem`, `CarouselPrevious`, `CarouselNext`.
- `Drawer`: `DrawerPortal`, `DrawerOverlay`, `DrawerClose`, `DrawerTrigger`, `DrawerContent`, `DrawerHeader`, `DrawerFooter`, `DrawerTitle`, `DrawerDescription`.
- `HoverCard`: `HoverCardTrigger`, `HoverCardContent`.
- `Sheet`: `SheetPortal`, `SheetOverlay`, `SheetClose`, `SheetTrigger`, `SheetContent`, `SheetHeader`, `SheetFooter`, `SheetTitle`, `SheetDescription`.
- `Tabs`: `TabsList`, `TabsTrigger`, `TabsContent`.

Parts accept nested MAUI views as content. The compound roots keep their bindable slots and item-based APIs; bind related `IsOpen` properties for overlay triggers, `SelectedKey` for composed tabs, and `Position`/`ItemCount` for carousel navigation. `Button` also accepts `LeadingContent` and `TrailingContent`. Input uses UraniumUI Material `TextField`; all components use the centralized light/dark palette in `Resources\Styles\ThemeColors.xaml`.

### Utility components

The web utilities are available from `EEE.Components.Utility`:

- `ContactForm`: set `EndpointUrl` to an absolute HTTP(S) notification endpoint; it raises `MessageSent` or `SubmissionFailed`.
- `LocalOperationModeToggle`: switches the persisted `LocalOperationModeService.Mode`; it is shown in Debug/local builds and supports `FullWidth`.
- `LocalSalesSyncButton`: bind pending `PendingSales`, `PendingCounts`, and `PendingStocks` values and a `SyncCommand` from the app's offline-data service. The MAUI starter has no local inventory store or sync API, so syncing is deliberately delegated instead of assuming web endpoints.
- `Navbar`: compose `BrandContent`, `SideNavigation`, `NavigationContent`, `ActionsContent`, and `BannerContent`.
- `Nav` and `Sidenav`: bind collections of `UtilityNavigationItem` (`Name`, `Title`, and a registered Shell `Route` or `Command`).
- `Social`: bind a collection of `SocialLink` (`Name`, `Uri`, and `IconText`).
- `VisitTracker`: set an absolute `EndpointUrl` and bind `Path`; it skips stock, sales, and expenses routes, de-duplicates visited paths per app session, and raises `TrackingFailed` on request errors.

`TextArea` was added to `EEE.Components.UI` for the contact form and can be used by other pages. Example:

```xml
xmlns:utility="clr-namespace:EEE.Components.Utility"

<utility:ContactForm EndpointUrl="{Binding NotificationEndpoint}" />
<utility:VisitTracker Path="{Binding CurrentPath}" EndpointUrl="{Binding VisitEndpoint}" />
```

### Account and administration utilities

The web app's account utilities are adapted as MAUI controls under the same utility namespace. Set `ApiBaseUrl` to the server origin (for example, `https://example.com/`) for controls that call the web API. Administrative controls require the host page to explicitly set `IsAdmin`; they are not an authorization boundary and the server must enforce permissions.

```xml
<utility:AccountUpgrade ApiBaseUrl="{Binding ApiBaseUrl}"
                        UserId="{Binding User.Id}"
                        UserRole="{Binding User.Role}" />
<utility:AddressEdit ApiBaseUrl="{Binding ApiBaseUrl}"
                     UserId="{Binding User.Id}"
                     AddressId="{Binding Address.Id}" />
<utility:AffiliateDialog ApiBaseUrl="{Binding ApiBaseUrl}" />
<utility:AdminBulkManager ApiBaseUrl="{Binding ApiBaseUrl}" IsAdmin="{Binding IsAdmin}" />
<utility:AdminUserManager ApiBaseUrl="{Binding ApiBaseUrl}" IsAdmin="{Binding IsAdmin}" />
<utility:Advert ApiBaseUrl="{Binding ApiBaseUrl}" IsAdmin="{Binding IsAdmin}" />
<utility:Avatar Source="{Binding User.Avatar}" FallbackText="{Binding User.Name}" />
<utility:Auth LoginCommand="{Binding LoginCommand}" />
```

`Auth` delegates credential submission to the supplied `LoginCommand`; otherwise, set `ApiBaseUrl` to sign in through the web app's NextAuth credentials endpoint. The shared API client retains the resulting session cookie for subsequent requests, and `LoginSucceeded` returns the signed-in user. Account/address/affiliate/admin controls expose status state and failure events. `AdminBulkManager` uses the configured bulk endpoint and the platform share/file picker for CSV import and export; stock export is supported, while stock import is disabled because the server does not implement it. `Advert` can display active messages without admin access and only shows management tools when `IsAdmin` is true. `Avatar` wraps the UI avatar control; its `AvatarImage` and `AvatarFallback` composition parts are available from the UI namespace.

Commerce controls in `EEE.Components.Utility` provide MAUI counterparts to the web carousel, cart, cart item, cart details, cart details dialog, chat, common medications, and concern grid. Set `ApiBaseUrl` for controls that call the backend. `Cart` accepts an `ObservableCollection<CommerceCartItem>` and exposes checkout/payment events; `CommonMedications` raises `AddToCartRequested` so the page can add a selected product to its cart; `ConcernGrid` raises `ConcernSelected`; `ChatInterface` requires the signed-in user's ID, name, and role. Administrative actions and customer-owned record updates require a valid authenticated web session.

```xml
<utility:CommonMedications ApiBaseUrl="{Binding ApiBaseUrl}"
                           IsAdmin="{Binding IsAdmin}"
                           AddToCartRequested="OnAddToCartRequested" />
<utility:Cart ApiBaseUrl="{Binding ApiBaseUrl}"
              UserId="{Binding User.Id}"
              UserRole="{Binding User.Role}"
              Items="{Binding CartItems}" />
<utility:ChatInterface ApiBaseUrl="{Binding ApiBaseUrl}"
                        CurrentUserId="{Binding User.Id}"
                        CurrentUserName="{Binding User.Name}"
                        CurrentUserRole="{Binding User.Role}" />
<utility:ConcernGrid ConcernSelected="OnConcernSelected" />
```

### Discovery and content utilities

The discovery controls replicate the web dashboard, data table, featured categories/ingredients/products, feature cards, filters, footer, product gallery, global search, image/profile upload, post publishing, and Google sign-in integration. API-backed controls require `ApiBaseUrl`; admin mutations also require an authenticated session and the host's `IsAdmin` state. The API remains responsible for authorization.

`DataTable` accepts `ItemsSource` and optional `Columns` (`DiscoveryDataColumn` values) and exposes `RowSelected`. `GlobalSearch`, `FeaturedCategories`, `FeaturedIngredients`, `FeaturedProducts`, and `Gallery` expose selection/cart events so the host page can perform navigation and cart updates. `Footer` raises `NavigationRequested` and `CategorySelected`, hides itself for stock/sales/expenses routes, and submits affiliate applications through the authenticated `/api/affiliate` endpoint. `ProfileImage` and `PostButton` use the shared file picker and multipart upload APIs; post publishing requires `AuthorId` and admin access. `GoogleSignIn` raises `SignInRequested` or executes a host-provided `HostSignInCommand`; configure the host to launch the platform's OAuth flow.

```xml
<utility:GlobalSearch ApiBaseUrl="{Binding ApiBaseUrl}"
                      UserRole="{Binding User.Role}"
                      ProductSelected="OnProductSelected" />
<utility:FeaturedProducts ApiBaseUrl="{Binding ApiBaseUrl}"
                           UserRole="{Binding User.Role}"
                           IsAdmin="{Binding IsAdmin}"
                           AddToCartRequested="OnAddToCartRequested" />
<utility:Footer ApiBaseUrl="{Binding ApiBaseUrl}"
                CurrentPath="{Binding CurrentPath}"
                NavigationRequested="OnFooterNavigationRequested" />
<utility:PostButton ApiBaseUrl="{Binding ApiBaseUrl}"
                    AuthorId="{Binding User.Id}"
                    IsAdmin="{Binding IsAdmin}" />
```

### Hero, login, and notifications

The hero controls are available under `EEE.Components.Utility` (the MAUI equivalent of the web `index.ts` barrel; XAML imports the namespace rather than a generated barrel file). `Hero` includes a rotating headline, product search, calls to action, pharmacist contact prompt, and the bundled pharmacist image. It raises navigation, prescription, special-order, and call failure events for app-specific routing/actions. `HeroBadge` exposes its text and destination and raises `NavigationRequested`. `Item` intentionally remains the empty placeholder from the web component.

`HeavilyDiscountedCarousel` loads `/api/heavily-discounted`, displays approved and own pending listings, and exposes product/navigation events. Professional users can search the product catalogue and submit products for approval; admin/staff users can approve or remove listings. The endpoint derives privileges and creator IDs from the authenticated NextAuth session instead of trusting query/body identity values. `Login` authenticates through the same NextAuth credentials endpoints as the app and retains the shared cookie session. `LoginPopup` displays once after one minute when the host sets `AuthenticationStateKnown="True"` and the user is unauthenticated. OAuth and registration are host events/commands.

`NotificationUI` polls `/api/dbhandler?model=message` every 30 seconds for the signed-in user and routes to `/contact` via `NavigationRequested`. It requires an authenticated NextAuth session; API errors are exposed through `RequestFailed`. The web login references email verification endpoints that are not present in this repository; the control surfaces server-reported verification requirements, but those flows require the backend routes to be implemented.

```xml
<utility:Hero ApiBaseUrl="{Binding ApiBaseUrl}"
              IsAuthenticated="{Binding IsAuthenticated}"
              NavigationRequested="OnNavigate" />
<utility:HeavilyDiscountedCarousel ApiBaseUrl="{Binding ApiBaseUrl}"
                                   UserId="{Binding User.Id}"
                                   UserRole="{Binding User.Role}"
                                   ProductSelected="OnProductSelected" />
<utility:LoginPopup ApiBaseUrl="{Binding ApiBaseUrl}"
                    AuthenticationStateKnown="{Binding AuthenticationStateKnown}"
                    IsAuthenticated="{Binding IsAuthenticated}"
                    LoginSucceeded="OnLoginSucceeded" />
<utility:NotificationUI ApiBaseUrl="{Binding ApiBaseUrl}"
                         UserId="{Binding User.Id}"
                         UserRole="{Binding User.Role}"
                         NavigationRequested="OnNavigate" />
```

### Community, product, and account utilities

The web counterparts `partner-brands`, `post`, `posts`, `priceFeedback`, `productCard`, `productReviews`, `profileCards`, `search`, `searchcomponent`, and `signup` are available in `EEE.Components.Utility`. These controls use the existing `Components\UI` Button, Card, Dialog, Input, Select, TextArea, and Avatar components and the shared light/dark palette. Controls that call the server require `ApiBaseUrl`; signed-in actions also require the shared authenticated session. `PartnerBrands` requires both host `IsAdmin` state and server admin/staff authorization for mutations.

`Posts` loads `/api/dbhandler?model=post` and filters by category, media type, and date. `Post` reads and writes likes and comments through `/api/dbhandler?model=likes` and `model=comments`; writes require the authenticated session and the API derives the user identity from that session. The matching Prisma models are enabled in the schema. `PriceFeedback` and `ProductReviews` use the existing `priceFeedback`, `product`, and `review` models. `ProductCard` emits cart/product-selection events and toggles the signed-in user's wishlist through `/api/wishlist`. `SearchInput` uses `/api/dbhandler?model=product&query=...`; `Search` intentionally matches the empty web placeholder.

After updating Prisma Client, apply the schema to the configured MongoDB database (`npx prisma db push`) before deploying these comment/like endpoints so the new collections and unique like index are available. Review the target database and back it up before applying schema changes.

Signup posts to `/api/auth/register`. That route now validates account roles and role-specific registration fields, hashes passwords, prevents privileged role assignment, and returns a safe user summary. Google sign-in and switching to Login are host events because OAuth launch and app navigation are platform-specific.

The HealthClique brand mark is provided as `public\healthclique-logo.svg` and packaged directly as a MAUI image. The Hero and Sidenav use this asset; its colors follow the shared brand palette.

```xml
<utility:PartnerBrands ApiBaseUrl="{Binding ApiBaseUrl}" IsAdmin="{Binding IsAdmin}" />
<utility:Posts ApiBaseUrl="{Binding ApiBaseUrl}" Category="All"
               IsAdmin="{Binding IsAdmin}" UserId="{Binding User.Id}" />
<utility:ProductCard Product="{Binding Product}" ApiBaseUrl="{Binding ApiBaseUrl}"
                     UserId="{Binding User.Id}"
                     UserRole="{Binding User.Role}" AddToCartRequested="OnAddToCartRequested" />
<utility:ProductReviews ApiBaseUrl="{Binding ApiBaseUrl}" ProductId="{Binding Product.Id}"
                        UserId="{Binding User.Id}" />
<utility:PriceFeedback ApiBaseUrl="{Binding ApiBaseUrl}" ProductId="{Binding Product.Id}"
                       ProductName="{Binding Product.Name}" UserId="{Binding User.Id}" />
<utility:SearchInput ApiBaseUrl="{Binding ApiBaseUrl}" ProductSelected="OnProductSelected" />
<utility:Signup ApiBaseUrl="{Binding ApiBaseUrl}" SignupSucceeded="OnSignupSucceeded" />
```

### Store, prescription, policy, and testimonial utilities

The MAUI counterparts to `similar`, `SnapPrescription`, `SpecialOrderForm`, `stocks`, `TermsAgreements`, `testimonialProp`, `testimonials`, `textctrl`, and `useredit` are also available in `EEE.Components.Utility`. These controls use the shared `Components\UI` primitives and `ThemeColors.xaml`. Cart and navigation integration is exposed through events so the host page can connect the controls to its own cart and routes.

`SnapPrescription` uses the HealthClique `/api/products/parse-prescription` and `/api/products/search-batch` endpoints; image analysis remains server-side and the Gemini key is not sent to the MAUI client. Camera capture requires runtime permission on Android; file selection and typed prescriptions remain available without a camera. Add selected results through `AddToCartRequested`. `SpecialOrderForm` raises `AddToCartRequested` plus `SpecialOrderRequested`, matching the web component's behavior of adding a special item to the cart without uploading the optional file.

`Stocks` uses `dbhandler?model=product` and the existing featured/discounted APIs, and raises `AddToCartRequested` or `ProductSelected`. `TermsAgreements` posts agreement field updates to the signed-in user's `dbhandler?model=user` record. `UserEdit` (also exported as `EditUser`) reads and updates the signed-in user's profile and shipping address. Both account controls require a valid session retained by the shared API client.

`TestimonialProp` (also exported as `TestimonialCard`) displays an individual author and quote; `Testimonials` (also exported as `TestimonialsSection`) accepts a collection of `TestimonialItem` values. `TextCtrl` rotates text or `ZoomVerse` values with a selectable `TextAnimation`; convenience controls are exported for each web animation variant (`TextFade`, `SlideInText`, `SlideUpText`, `ScaleInText`, `RotateInText`, `FlipInText`, `BounceInText`, `RiseAndFadeText`, and `ZoomInText`).

```xml
<utility:SnapPrescription ApiBaseUrl="{Binding ApiBaseUrl}"
                          AddToCartRequested="OnAddToCartRequested" />
<utility:SpecialOrderForm AddToCartRequested="OnAddToCartRequested" />
<utility:Stocks ApiBaseUrl="{Binding ApiBaseUrl}" UserRole="{Binding User.Role}"
                ProductSelected="OnProductSelected" AddToCartRequested="OnAddToCartRequested" />
<utility:TermsAgreements ApiBaseUrl="{Binding ApiBaseUrl}" UserId="{Binding User.Id}"
                         AcceptedTerms="{Binding User.AcceptedTerms}"
                         AcceptedPrivacy="{Binding User.AcceptedPrivacy}"
                         AcceptedReturns="{Binding User.AcceptedReturns}" />
<utility:Testimonials Title="What our customers say" TestimonialItems="{Binding Testimonials}" />
<utility:TextFade Texts="{Binding RotatingTexts}" Interval="6000" />
<utility:EditUser ApiBaseUrl="{Binding ApiBaseUrl}" UserProfile="{Binding UserProfile}" />
```
