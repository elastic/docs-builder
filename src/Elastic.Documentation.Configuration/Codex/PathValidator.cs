// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.Codex;

/// <summary>
/// Validates path components to prevent path traversal attacks.
/// </summary>
public static class PathValidator
{
	/// <summary>
	/// Validates that a path component does not contain path traversal sequences.
	/// </summary>
	/// <param name="pathComponent">The path component to validate.</param>
	/// <param name="parameterName">The parameter name for error messages.</param>
	/// <returns>The validated path component.</returns>
	/// <exception cref="ArgumentException">Thrown when the path component contains invalid characters or sequences.</exception>
	public static string ValidatePathComponent(string pathComponent, string parameterName)
	{
		if (string.IsNullOrWhiteSpace(pathComponent))
			throw new ArgumentException("Path component cannot be null or whitespace.", parameterName);

		// Normalize the path to detect traversal attempts
		var normalizedPath = Path.GetFullPath(Path.Combine(Path.DirectorySeparatorChar.ToString(), pathComponent));
		var expectedPath = Path.DirectorySeparatorChar + pathComponent.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

		// If normalized path doesn't match expected, it contains traversal sequences
		if (!normalizedPath.Equals(expectedPath, StringComparison.OrdinalIgnoreCase))
			throw new ArgumentException($"Path component '{pathComponent}' contains invalid path traversal sequences.", parameterName);

		// Check for absolute path attempts
		if (Path.IsPathRooted(pathComponent))
			throw new ArgumentException($"Path component '{pathComponent}' cannot be an absolute path.", parameterName);

		// Check for invalid characters (beyond what Path.GetInvalidPathChars covers)
		var invalidChars = new[] { '\0', '<', '>', '|', '\"', '?' };
		if (pathComponent.Any(c => invalidChars.Contains(c)))
			throw new ArgumentException($"Path component '{pathComponent}' contains invalid characters.", parameterName);

		return pathComponent;
	}

	/// <summary>
	/// Validates a relative path that may contain subdirectories.
	/// </summary>
	/// <param name="relativePath">The relative path to validate.</param>
	/// <param name="parameterName">The parameter name for error messages.</param>
	/// <returns>The validated relative path.</returns>
	/// <exception cref="ArgumentException">Thrown when the path contains invalid sequences.</exception>
	public static string ValidateRelativePath(string relativePath, string parameterName)
	{
		if (string.IsNullOrWhiteSpace(relativePath))
			throw new ArgumentException("Relative path cannot be null or whitespace.", parameterName);

		// Normalize path separators
		var normalizedPath = relativePath.Replace('\\', '/').Trim('/');

		// Split into components and validate each
		var components = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
		foreach (var component in components)
		{
			// Reject parent directory references
			if (component == "..")
				throw new ArgumentException($"Relative path '{relativePath}' cannot contain parent directory references (..).", parameterName);

			// Reject current directory references (unnecessary)
			if (component == ".")
				throw new ArgumentException($"Relative path '{relativePath}' cannot contain current directory references (.).", parameterName);

			// Validate component doesn't have invalid characters
			var invalidChars = new[] { '\0', '<', '>', '|', '\"', '?', '*', ':' };
			if (component.Any(c => invalidChars.Contains(c)))
				throw new ArgumentException($"Relative path component '{component}' contains invalid characters.", parameterName);
		}

		return relativePath;
	}
}
