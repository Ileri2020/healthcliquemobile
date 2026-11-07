namespace EEE.Components.UI;

public class DrawerPortal : ContentView { }

public class DrawerOverlay : ThemedOverlay { }

public class DrawerContent : ContentView
{
    public DrawerContent() => Padding = 24;
}

public class DrawerHeader : VerticalStackLayout
{
    public DrawerHeader() => Spacing = 6;
}

public class DrawerFooter : HorizontalStackLayout
{
    public DrawerFooter() => Spacing = 8;
}

public class DrawerTitle : CardTitle { }

public class DrawerDescription : CardDescription { }

public class DrawerTrigger : OverlayTrigger
{
    protected override bool Opens => true;
}

public class DrawerClose : OverlayTrigger
{
    protected override bool Opens => false;
}
