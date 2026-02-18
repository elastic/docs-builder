// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;

namespace Elastic.Documentation.Assembler.Mcp;

/// <summary>Provides content type templates and guidelines from embedded resources.</summary>
public class ContentTypeProvider
{
	private static readonly Dictionary<string, string> EmbeddedTemplateNames = new()
	{
		["overview"] = "Elastic.Documentation.Assembler.Mcp.Resources.Templates.overview.md.txt",
		["how-to"] = "Elastic.Documentation.Assembler.Mcp.Resources.Templates.how-to.md.txt",
		["tutorial"] = "Elastic.Documentation.Assembler.Mcp.Resources.Templates.tutorial.md.txt",
		["troubleshooting"] = "Elastic.Documentation.Assembler.Mcp.Resources.Templates.troubleshooting.md.txt",
		["changelog"] = "Elastic.Documentation.Assembler.Mcp.Resources.Templates.changelog.yaml"
	};

	private static readonly Dictionary<string, string> EmbeddedGuidelineNames = new()
	{
		["overview"] = "Elastic.Documentation.Assembler.Mcp.Resources.Guidelines.overview.md.txt",
		["how-to"] = "Elastic.Documentation.Assembler.Mcp.Resources.Guidelines.how-to.md.txt",
		["tutorial"] = "Elastic.Documentation.Assembler.Mcp.Resources.Guidelines.tutorial.md.txt",
		["troubleshooting"] = "Elastic.Documentation.Assembler.Mcp.Resources.Guidelines.troubleshooting.md.txt",
		["changelog"] = "Elastic.Documentation.Assembler.Mcp.Resources.Guidelines.changelog.md.txt"
	};

	public static readonly string[] ValidContentTypes = ["overview", "how-to", "tutorial", "troubleshooting", "changelog"];

	public static bool IsValidContentType(string contentType) =>
		EmbeddedTemplateNames.ContainsKey(contentType);

	/// <summary>Gets a template for the given content type from embedded resources.</summary>
	public string GetTemplate(string contentType) =>
		ReadEmbeddedResource(EmbeddedTemplateNames[contentType]);

	/// <summary>Gets guidelines for the given content type from embedded resources.</summary>
	public string GetGuidelines(string contentType) =>
		ReadEmbeddedResource(EmbeddedGuidelineNames[contentType]);

	private static string ReadEmbeddedResource(string resourceName)
	{
		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream(resourceName)
			?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}
}
