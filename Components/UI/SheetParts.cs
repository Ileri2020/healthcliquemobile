namespace EEE.Components.UI;

public class SheetPortal : ContentView { }

public class SheetOverlay : ThemedOverlay { }

public class SheetContent : ContentView
{
    public SheetContent() => Padding = 24;
}

public class SheetHeader : VerticalStackLayout
{
    public SheetHeader() => Spacing = 6;
}

public class SheetFooter : HorizontalStackLayout
{
    public SheetFooter() => Spacing = 8;
}

public class SheetTitle : CardTitle { }

public class SheetDescription : CardDescription { }

public class SheetTrigger : OverlayTrigger
{
    protected override bool Opens => true;
}

public class SheetClose : OverlayTrigger
{
    protected override bool Opens => false;
}
