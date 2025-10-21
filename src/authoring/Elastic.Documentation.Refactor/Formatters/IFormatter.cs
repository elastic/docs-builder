// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Refactor.Formatters;

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
	/// Formats the content and returns the modified content along with the number of changes made
	/// </summary>
	/// <param name="content">The content to format</param>
	/// <returns>A tuple containing the formatted content and the number of changes made</returns>
	(string content, int changes) Format(string content);
}
