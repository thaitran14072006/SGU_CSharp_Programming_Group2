using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MocBakery.Web.TagHelpers;

// Decorative icons keep the visible labels and accessible names of each action intact.
[HtmlTargetElement("a", Attributes = "admin-icon")]
[HtmlTargetElement("button", Attributes = "admin-icon")]
[HtmlTargetElement("span", Attributes = "admin-icon")]
public sealed class AdminIconTagHelper : TagHelper
{
    [HtmlAttributeName("admin-icon")]
    public string Icon { get; set; } = "";
    public override int Order => 1000;

    private static readonly IReadOnlyDictionary<string, string> Shapes = new Dictionary<string, string>
    {
        ["home"] = "<path d='m3 10 9-7 9 7v10H3zM9 20v-7h6v7'/>",
        ["orders"] = "<rect x='5' y='4' width='14' height='17' rx='2'/><path d='M9 4V2h6v2M9 10h6M9 14h6M9 18h3'/>",
        ["cake"] = "<path d='M4 12h16v8H4zM4 16c2 3 3-3 5 0s3-3 5 0 4-3 6 0M7 12V8h10v4M12 8V5M12 2v1'/>",
        ["layers"] = "<path d='m12 3 10 5-10 5L2 8zM2 12l10 5 10-5M2 16l10 5 10-5'/>",
        ["box"] = "<path d='m3 7 9-4 9 4v11l-9 4-9-4zM3 7l9 4 9-4M12 11v11M7.5 5l9 4'/>",
        ["calendar"] = "<rect x='3' y='5' width='18' height='16' rx='2'/><path d='M7 3v4M17 3v4M3 11h18M8 15h2M14 15h2M8 18h2'/>",
        ["receipt"] = "<path d='M5 3h14v18l-3-2-4 2-4-2-3 2zM9 7h6M9 11h6M9 15h3'/>",
        ["truck"] = "<path d='M3 5h11v12H3zM14 9h4l3 4v4h-7M14 13h7'/><circle cx='7' cy='18' r='2'/><circle cx='18' cy='18' r='2'/>",
        ["leaf"] = "<path d='M20 3C9 2 2 7 5 15c6 7 16 0 15-12ZM4 21 15 10M8 17v-6M11 14h6'/>",
        ["users"] = "<circle cx='9' cy='7' r='3'/><path d='M3 21v-3a6 6 0 0 1 12 0v3M16 4a3 3 0 0 1 0 6M18 14a5 5 0 0 1 3 5v2'/>",
        ["user"] = "<circle cx='12' cy='7' r='4'/><path d='M4 21v-2a8 8 0 0 1 16 0v2'/>",
        ["shield"] = "<path d='m12 3 8 3v6c0 5-8 9-8 9s-8-4-8-9V6zM9 12l2 2 4-4'/>",
        ["chart"] = "<path d='M4 3v18h17M8 16v-5M13 16V7M18 16V4'/>",
        ["store"] = "<path d='M4 10v11h16V10M3 3h18v7a3 3 0 0 1-6 0 3 3 0 0 1-6 0 3 3 0 0 1-6 0zM9 21v-7h6v7'/>",
        ["key"] = "<circle cx='8' cy='8' r='5'/><path d='m12 12 9 9M15 15l3-3M18 18l3-3'/>",
        ["logout"] = "<path d='M10 3H4v18h6M9 12h12m-4-4 4 4-4 4'/>",
        ["edit"] = "<path d='m15 4 5 5M4 20l5-1L21 7a2 2 0 0 0-5-5L4 14z'/>",
        ["save"] = "<path d='M4 3h13l4 4v14H3V3zM7 3v6h10V3M7 21v-8h10v8'/>",
        ["trash"] = "<path d='M3 6h18M9 6V3h6v3M6 6l1 15h10l1-15M10 10v7M14 10v7'/>",
        ["close"] = "<circle cx='12' cy='12' r='9'/><path d='m9 9 6 6m0-6-6 6'/>",
        ["search"] = "<circle cx='10.5' cy='10.5' r='6.5'/><path d='m16 16 5 5'/>",
        ["filter"] = "<path d='M3 5h18M6 12h12M9 19h6'/><circle cx='8' cy='5' r='2'/><circle cx='16' cy='12' r='2'/>",
        ["check"] = "<circle cx='12' cy='12' r='9'/><path d='m8 12 3 3 5-6'/>",
        ["pause"] = "<circle cx='12' cy='12' r='9'/><path d='M9 8v8M15 8v8'/>",
        ["play"] = "<circle cx='12' cy='12' r='9'/><path d='m10 8 6 4-6 4z'/>",
        ["excel"] = "<path d='M14 3H5v18h14V8zM14 3v5h5'/><rect x='8' y='11' width='8' height='6' rx='.5'/><path d='M12 11v6M8 14h8'/>",
        ["pdf"] = "<path d='M14 3H5v18h14V8zM14 3v5h5M8 12h8M8 15h8M8 18h4'/>",
        ["history"] = "<path d='M3 10a9 9 0 1 1 0 5M3 4v6h6M12 7v5l3 2'/>",
        ["stocktake"] = "<path d='M4 4h16v16H4zM8 8h8M8 12h3M8 16h3m3-3 2 2 4-4'/>",
    };

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.Attributes.RemoveAll("admin-icon");
        if (!Shapes.TryGetValue(Icon, out string? shape)) return;
        var content = await output.GetChildContentAsync();
        output.Content.SetHtmlContent($"<svg class=\"admin-icon\" viewBox=\"0 0 24 24\" width=\"20\" height=\"20\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\" focusable=\"false\">{shape}</svg>");
        output.Content.AppendHtml(content);
    }
}
