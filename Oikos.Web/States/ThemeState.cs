using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Utilities;

namespace Oikos.Web.States;

public class ThemeState
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _jsRuntime;

    // Original Palette
    private static readonly MudColor DefaultPrimaryColor = new("#ff5722");
    private static readonly MudColor DefaultSecondaryColor = new("#1668dc");

    private bool _shouldResetDarkCookie;
    private bool _shouldResetPrimaryColorCookie;

    public ThemeState(IHttpContextAccessor httpContextAccessor,
        NavigationManager navigationManager,
        IJSRuntime jSRuntime)
    {
        _httpContextAccessor = httpContextAccessor;
        _navigationManager = navigationManager;
        _jsRuntime = jSRuntime;

        var value = _httpContextAccessor.HttpContext?.Request.Cookies.Where(c => c.Key == "IsDark")
            .FirstOrDefault().Value;
        _shouldResetDarkCookie = !string.IsNullOrEmpty(value) && bool.TryParse(value, out var isDarkCookie) && isDarkCookie;

        // Allow initial state to reflect cookie or default
        _isDark = _shouldResetDarkCookie;

        var primaryColor = _httpContextAccessor.HttpContext?.Request.Cookies.Where(c => c.Key == "PrimaryColor")
            .FirstOrDefault().Value;
        _shouldResetPrimaryColorCookie = !string.IsNullOrEmpty(primaryColor) &&
            !string.Equals(primaryColor, DefaultPrimaryColor.Value, StringComparison.OrdinalIgnoreCase);

        _theme = new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = DefaultPrimaryColor,
                Secondary = DefaultSecondaryColor,
                Background = "#F8FAFC",
                Surface = "#FFFFFF",
                AppbarBackground = "#FFFFFF",
                AppbarText = "#1E293B",
                DrawerBackground = "#FFFFFF",
                DrawerText = "#334155",
                TextPrimary = "#0F172A",
                TextSecondary = "#475569",
                ActionDefault = "#64748B",
                TableLines = "#E2E8F0",
                LinesDefault = "#E2E8F0",
                Divider = "#E2E8F0",
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#3B82F6",
                Secondary = "#94A3B8",
                Tertiary = "#594AE2",
                Black = "#0F172A",
                Background = "#0F172A",
                BackgroundGray = "#1E293B",
                Surface = "#1E293B",
                DrawerBackground = "#1E293B",
                DrawerText = "rgba(255,255,255, 0.70)",
                DrawerIcon = "rgba(255,255,255, 0.70)",
                AppbarBackground = "#1E293B",
                AppbarText = "rgba(255,255,255, 0.90)",
                TextPrimary = "rgba(255,255,255, 0.90)",
                TextSecondary = "rgba(255,255,255, 0.60)",
                ActionDefault = "#94A3B8",
                TableLines = "rgba(255, 255, 255, 0.12)",
                TextDisabled = "rgba(255, 255, 255, 0.3)",
                Divider = "rgba(255, 255, 255, 0.12)",
                LinesDefault = "rgba(255, 255, 255, 0.12)"
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "8px",
            },
            Typography = new Typography()
            {
                Default = new DefaultTypography()
                {
                    FontFamily = new[] { "Inter", "system-ui", "-apple-system", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif" },
                    FontSize = "0.875rem",
                    FontWeight = "400",
                    LineHeight = "1.5",
                    LetterSpacing = "0.01em"
                },
                H1 = new H1Typography()
                {
                    FontFamily = new[] { "Inter", "system-ui", "-apple-system", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif" },
                    FontSize = "2.25rem",
                    FontWeight = "700",
                    LineHeight = "1.2",
                    LetterSpacing = "-0.02em"
                },
                H2 = new H2Typography()
                {
                    FontFamily = new[] { "Inter", "system-ui", "-apple-system", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif" },
                    FontSize = "1.875rem",
                    FontWeight = "700",
                    LineHeight = "1.25",
                    LetterSpacing = "-0.015em"
                },
                H3 = new H3Typography()
                {
                    FontFamily = new[] { "Inter", "system-ui", "-apple-system", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif" },
                    FontSize = "1.5rem",
                    FontWeight = "600",
                    LineHeight = "1.3",
                    LetterSpacing = "-0.01em"
                },
                H4 = new H4Typography()
                {
                    FontFamily = new[] { "Inter", "system-ui", "-apple-system", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif" },
                    FontSize = "1.25rem",
                    FontWeight = "600",
                    LineHeight = "1.35",
                    LetterSpacing = "0em"
                },
                H5 = new H5Typography()
                {
                    FontFamily = new[] { "Inter", "system-ui", "-apple-system", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif" },
                    FontSize = "1.125rem",
                    FontWeight = "600",
                    LineHeight = "1.4",
                    LetterSpacing = "0em"
                },
                H6 = new H6Typography()
                {
                    FontFamily = new[] { "Inter", "system-ui", "-apple-system", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif" },
                    FontSize = "1rem",
                    FontWeight = "600",
                    LineHeight = "1.4",
                    LetterSpacing = "0.005em"
                },
                Subtitle1 = new Subtitle1Typography()
                {
                    FontFamily = new[] { "Inter", "system-ui", "-apple-system", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif" },
                    FontSize = "1rem",
                    FontWeight = "500",
                    LineHeight = "1.5",
                    LetterSpacing = "0.005em"
                },
                Subtitle2 = new Subtitle2Typography()
                {
                    FontFamily = new[] { "Inter", "system-ui", "-apple-system", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif" },
                    FontSize = "0.875rem",
                    FontWeight = "500",
                    LineHeight = "1.5",
                    LetterSpacing = "0.005em"
                },
                Body1 = new Body1Typography()
                {
                    FontFamily = new[] { "Inter", "system-ui", "-apple-system", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif" },
                    FontSize = "1rem",
                    FontWeight = "400",
                    LineHeight = "1.5",
                    LetterSpacing = "0.005em"
                },
                Body2 = new Body2Typography()
                {
                    FontFamily = new[] { "Inter", "system-ui", "-apple-system", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif" },
                    FontSize = "0.875rem",
                    FontWeight = "400",
                    LineHeight = "1.5",
                    LetterSpacing = "0.01em"
                },
                Button = new ButtonTypography()
                {
                    FontFamily = new[] { "Inter", "system-ui", "-apple-system", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif" },
                    FontSize = "0.875rem",
                    FontWeight = "600",
                    LineHeight = "1.75",
                    LetterSpacing = "0.02em",
                    TextTransform = "none"
                },
                Caption = new CaptionTypography()
                {
                    FontFamily = new[] { "Inter", "system-ui", "-apple-system", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif" },
                    FontSize = "0.75rem",
                    FontWeight = "400",
                    LineHeight = "1.4",
                    LetterSpacing = "0.01em"
                },
                Overline = new OverlineTypography()
                {
                    FontFamily = new[] { "Inter", "system-ui", "-apple-system", "Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif" },
                    FontSize = "0.75rem",
                    FontWeight = "600",
                    LineHeight = "1.4",
                    LetterSpacing = "0.08em",
                    TextTransform = "uppercase"
                }
            }
        };

        ApplySecondaryColor();
        UpdatePaletteColor(DefaultPrimaryColor);
    }

    private void UpdatePaletteColor(MudColor color)
    {
        _theme.PaletteLight.Primary = color;
        _theme.PaletteLight.PrimaryDarken = color.ColorRgbDarken().ToString(MudColorOutputFormats.RGB);
        _theme.PaletteLight.PrimaryLighten = color.ColorRgbLighten().ToString(MudColorOutputFormats.RGB);

        _theme.PaletteDark.Primary = color;
        _theme.PaletteDark.PrimaryDarken = color.ColorRgbDarken().ToString(MudColorOutputFormats.RGB);
        _theme.PaletteDark.PrimaryLighten = color.ColorRgbLighten().ToString(MudColorOutputFormats.RGB);
    }

    private void ApplySecondaryColor()
    {
        var secondaryDarken = DefaultSecondaryColor.ColorRgbDarken().ToString(MudColorOutputFormats.RGB);
        var secondaryLighten = DefaultSecondaryColor.ColorRgbLighten().ToString(MudColorOutputFormats.RGB);

        _theme.PaletteLight.Secondary = DefaultSecondaryColor;
        _theme.PaletteLight.SecondaryDarken = secondaryDarken;
        _theme.PaletteLight.SecondaryLighten = secondaryLighten;

        _theme.PaletteDark.Secondary = DefaultSecondaryColor;
        _theme.PaletteDark.SecondaryDarken = secondaryDarken;
        _theme.PaletteDark.SecondaryLighten = secondaryLighten;
    }

    private bool _isDark;
    private MudTheme _theme = new();

    public event Action? ThemeChangeEvent;
    public event Action? IsDarkChangeEvent;

    public void LoadTheme()
    {
        var themeChangedByCookieReset = false;

        if (_shouldResetDarkCookie)
        {
            _shouldResetDarkCookie = false;
            IsDarkStateChanged();
        }
        else
        {
            IsDarkStateChanged();
        }

        if (_shouldResetPrimaryColorCookie)
        {
            _shouldResetPrimaryColorCookie = false;
            _ = SetCookie("PrimaryColor", DefaultPrimaryColor.Value);
            themeChangedByCookieReset = true;
        }

        if (!themeChangedByCookieReset)
            ThemeStateChanged();
    }

    public bool IsDark
    {
        get => _isDark;
        set
        {
            if (_isDark == value) return;

            _isDark = value;
            _shouldResetDarkCookie = false;
            _ = SetCookie("IsDark", value.ToString());
        }
    }

    public MudColor PrimaryColor
    {
        get => _theme.PaletteLight.Primary;
        set
        {
            if (_theme.PaletteLight.Primary == DefaultPrimaryColor) return;

            UpdatePaletteColor(DefaultPrimaryColor);
            _shouldResetPrimaryColorCookie = false;
            _ = SetCookie("PrimaryColor", DefaultPrimaryColor.Value);
        }
    }

    private async Task SetCookie(string key, string value)
    {
        var cookieUtil = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/cookieUtil.js");
        await cookieUtil.InvokeVoidAsync("setCookie", key, value);
        IsDarkStateChanged();
        ThemeStateChanged();
    }

    public MudTheme MudTheme => _theme;

    private void ThemeStateChanged() => ThemeChangeEvent?.Invoke();
    private void IsDarkStateChanged() => IsDarkChangeEvent?.Invoke();
}
