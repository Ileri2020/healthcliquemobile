namespace EEE.Components.Utility;

public static class UtilityComponentExports
{
    public static IReadOnlyDictionary<string, Type> Components { get; } =
        new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            [nameof(HeavilyDiscountedCarousel)] = typeof(HeavilyDiscountedCarousel),
            [nameof(Hero)] = typeof(Hero),
            [nameof(HeroBadge)] = typeof(HeroBadge),
            [nameof(Item)] = typeof(Item),
            [nameof(Login)] = typeof(Login),
            [nameof(LoginPopup)] = typeof(LoginPopup),
            [nameof(NotificationUI)] = typeof(NotificationUI),
            [nameof(PartnerBrands)] = typeof(PartnerBrands),
            [nameof(Post)] = typeof(Post),
            [nameof(Posts)] = typeof(Posts),
            [nameof(PriceFeedback)] = typeof(PriceFeedback),
            [nameof(InlinePriceFeedback)] = typeof(InlinePriceFeedback),
            [nameof(ProductCard)] = typeof(ProductCard),
            [nameof(ProductReviews)] = typeof(ProductReviews),
            [nameof(ProfileCards)] = typeof(ProfileCards),
            [nameof(ProfileCard)] = typeof(ProfileCard),
            [nameof(Search)] = typeof(Search),
            [nameof(SearchInput)] = typeof(SearchInput),
            [nameof(Signup)] = typeof(Signup),
            [nameof(SignupForm)] = typeof(SignupForm),
            [nameof(Similar)] = typeof(Similar),
            [nameof(SnapPrescription)] = typeof(SnapPrescription),
            [nameof(SpecialOrderForm)] = typeof(SpecialOrderForm),
            [nameof(Stocks)] = typeof(Stocks),
            [nameof(TermsAgreements)] = typeof(TermsAgreements),
            [nameof(TestimonialProp)] = typeof(TestimonialProp),
            [nameof(TestimonialCard)] = typeof(TestimonialCard),
            [nameof(Testimonials)] = typeof(Testimonials),
            [nameof(TestimonialsSection)] = typeof(TestimonialsSection),
            [nameof(TextCtrl)] = typeof(TextCtrl),
            [nameof(TextFade)] = typeof(TextFade),
            [nameof(SlideInText)] = typeof(SlideInText),
            [nameof(SlideUpText)] = typeof(SlideUpText),
            [nameof(ScaleInText)] = typeof(ScaleInText),
            [nameof(RotateInText)] = typeof(RotateInText),
            [nameof(FlipInText)] = typeof(FlipInText),
            [nameof(BounceInText)] = typeof(BounceInText),
            [nameof(RiseAndFadeText)] = typeof(RiseAndFadeText),
            [nameof(ZoomInText)] = typeof(ZoomInText),
            [nameof(UserEdit)] = typeof(UserEdit),
            [nameof(EditUser)] = typeof(EditUser)
        };
}
