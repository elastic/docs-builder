namespace Elastic.Markdown.Helpers;

public static class TitleSanitizer
{
    // Removes markdown formatting from the title and returns only the text
    // Currently, only support 'bold' and 'code' formatting
	public static string Sanitize(string title) => title.Replace("`", "").Replace("*", "");
}
