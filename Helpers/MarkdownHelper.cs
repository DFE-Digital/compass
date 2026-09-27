using System.Net;
using Ganss.Xss;
using Markdig;

namespace Compass.Helpers
{
    public static class MarkdownHelper
    {
        private static readonly MarkdownPipeline DefaultPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        /// <summary>
        /// Pipeline for user-authored guidance: common formatting only, no raw HTML.
        /// </summary>
        private static readonly MarkdownPipeline SafePipeline = new MarkdownPipelineBuilder()
            .UseEmphasisExtras()
            .UseListExtras()
            .UsePipeTables()
            .UseAutoLinks()
            .DisableHtml()
            .Build();

        private static readonly HtmlSanitizer GuidanceSanitizer = CreateGuidanceSanitizer();

        /// <summary>
        /// Converts markdown to HTML (legacy / trusted admin content such as standards).
        /// </summary>
        public static string ToHtml(string? markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return string.Empty;

            try
            {
                return Markdown.ToHtml(markdown, DefaultPipeline);
            }
            catch (Exception)
            {
                return WebUtility.HtmlEncode(markdown);
            }
        }

        /// <summary>
        /// Converts markdown to sanitized HTML for user-authored census guidance.
        /// Disables raw HTML in Markdown and strips unsafe tags/schemes after conversion.
        /// </summary>
        public static string ToSafeHtml(string? markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return string.Empty;

            try
            {
                var html = Markdown.ToHtml(markdown, SafePipeline);
                return GuidanceSanitizer.Sanitize(html);
            }
            catch (Exception)
            {
                return WebUtility.HtmlEncode(markdown);
            }
        }

        private static HtmlSanitizer CreateGuidanceSanitizer()
        {
            var sanitizer = new HtmlSanitizer();

            sanitizer.AllowedTags.Clear();
            foreach (var tag in new[]
                     {
                         "p", "br", "strong", "b", "em", "i", "ul", "ol", "li",
                         "a", "h1", "h2", "h3", "h4", "blockquote", "code", "pre",
                         "table", "thead", "tbody", "tr", "th", "td"
                     })
            {
                sanitizer.AllowedTags.Add(tag);
            }

            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedAttributes.Add("href");
            sanitizer.AllowedAttributes.Add("title");

            sanitizer.AllowedSchemes.Clear();
            sanitizer.AllowedSchemes.Add("http");
            sanitizer.AllowedSchemes.Add("https");
            sanitizer.AllowedSchemes.Add("mailto");

            sanitizer.AllowDataAttributes = false;

            return sanitizer;
        }
    }
}
