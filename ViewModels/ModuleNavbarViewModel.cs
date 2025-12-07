namespace Compass.ViewModels
{
    public class ModuleNavbarViewModel
    {
        public string BrandText { get; set; } = string.Empty;
        public List<NavLinkItem> NavLinks { get; set; } = new List<NavLinkItem>();
    }

    public class NavLinkItem
    {
        public string Text { get; set; } = string.Empty;
        public string? Action { get; set; }
        public string? Controller { get; set; }
        public object? RouteValues { get; set; }
        public string? Url { get; set; }
        public bool IsActive { get; set; }
        public bool IsButton { get; set; }
    }
}

