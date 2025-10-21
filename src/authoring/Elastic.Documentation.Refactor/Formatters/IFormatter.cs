// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Refactor.Formatters;

/// <summary>
/// Result of a formatting operation
/// </summary>
/// <param name="Content">The formatted content</param>
/// <param name="Changes">The number of changes made</param>
public record FormatResult(string Content, int Changes);

/// <summary>
/// Defines a formatter that can process and modify file content
/// </summary>
public interface IFormatter
{
	/// <summary>
	/// Gets the name of this formatter for logging purposes
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Formats the content and returns the result
	/// </summary>
	/// <param name="content">The content to format</param>
	/// <returns>The format result containing the formatted content and number of changes</returns>
	FormatResult Format(string content);
}
