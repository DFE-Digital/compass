using Markdig;

namespace Compass.Helpers
{
    public static class MarkdownHelper
    {
        /// <summary>
        /// Converts markdown to HTML
        /// </summary>
        public static string ToHtml(string? markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return string.Empty;

            try
            {
                // Convert markdown to HTML using Markdig
                var pipeline = new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .Build();

                return Markdown.ToHtml(markdown, pipeline);
            }
            catch (Exception)
            {
                // If markdown parsing fails, return the original content escaped
                return System.Net.WebUtility.HtmlEncode(markdown);
            }
        }
    }
}

