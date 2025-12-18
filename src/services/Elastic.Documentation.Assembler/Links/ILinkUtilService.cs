// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;

namespace Elastic.Documentation.Assembler.Links;

/// <summary>
/// Service for working with cross-links and the link index.
/// </summary>
public interface ILinkUtilService
{
	/// <summary>
	/// Resolves a cross-link URI to its target URL.
	/// </summary>
	/// <param name="crossLink">The cross-link URI to resolve (e.g., 'docs-content://get-started/intro.md').</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The resolved cross-link information, or an error.</returns>
	Task<LinkUtilResult<CrossLinkResolveResult>> ResolveCrossLinkAsync(string crossLink, CancellationToken cancellationToken = default);

	/// <summary>
	/// Lists all repositories available in the link index.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of repositories with their metadata.</returns>
	Task<LinkUtilResult<ListRepositoriesResult>> ListRepositoriesAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all pages and their anchors published by a specific repository.
	/// </summary>
	/// <param name="repository">The repository name (e.g., 'docs-content', 'elasticsearch').</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Repository links information, or an error.</returns>
	Task<LinkUtilResult<RepositoryLinksResult>> GetRepositoryLinksAsync(string repository, CancellationToken cancellationToken = default);

	/// <summary>
	/// Finds all cross-links between repositories.
	/// </summary>
	/// <param name="sourceRepository">Source repository to find links FROM (optional).</param>
	/// <param name="targetRepository">Target repository to find links TO (optional).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Cross-links matching the criteria.</returns>
	Task<LinkUtilResult<FindCrossLinksResult>> FindCrossLinksAsync(string? sourceRepository, string? targetRepository, CancellationToken cancellationToken = default);

	/// <summary>
	/// Validates cross-links to a repository and reports any broken links.
	/// </summary>
	/// <param name="repository">Target repository to validate links TO.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Validation results including broken links.</returns>
	Task<LinkUtilResult<ValidateCrossLinksResult>> ValidateCrossLinksAsync(string repository, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a link utility operation.
/// </summary>
#pragma warning disable CA1000 // Do not declare static members on generic types - factory methods are appropriate here
public readonly struct LinkUtilResult<T>
{
	private LinkUtilResult(T? value, LinkUtilError? error, bool isSuccess)
	{
		Value = value;
		Error = error;
		IsSuccess = isSuccess;
	}

	/// <summary>
	/// Whether the operation succeeded.
	/// </summary>
	[MemberNotNullWhen(true, nameof(Value))]
	[MemberNotNullWhen(false, nameof(Error))]
	public bool IsSuccess { get; }

	/// <summary>
	/// The result value (if successful).
	/// </summary>
	public T? Value { get; }

	/// <summary>
	/// The error (if failed).
	/// </summary>
	public LinkUtilError? Error { get; }

	/// <summary>
	/// Creates a successful result.
	/// </summary>
	public static LinkUtilResult<T> CreateSuccess(T value) => new(value, null, true);

	/// <summary>
	/// Creates a failed result.
	/// </summary>
	public static LinkUtilResult<T> CreateFailure(string error, List<string>? details = null, List<string>? availableRepositories = null) =>
		new(default, new LinkUtilError(error, details, availableRepositories), false);
}
#pragma warning restore CA1000

/// <summary>
/// Error information from a link utility operation.
/// </summary>
public sealed record LinkUtilError(string Message, List<string>? Details = null, List<string>? AvailableRepositories = null);
