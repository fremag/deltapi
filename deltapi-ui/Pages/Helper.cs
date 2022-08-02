using Microsoft.AspNetCore.Components;

namespace deltapi_ui.Pages;

public static class Helper
{
    public static MarkupString Icon(string name) => (MarkupString)$"<span class=\"oi oi-{name}\"/>";
}