// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.GitHub;

/// <summary>
/// What an entry synthesized from a pull request needs of it. The body of a pull request can be long, so what is read from
/// it, the release note and the linked issues, is kept in its place.
/// </summary>
/// <param name="Number">The number of the pull request in its repository.</param>
/// <param name="Title">The title of the pull request.</param>
/// <param name="Labels">The names of all its labels.</param>
/// <param name="ReleaseNote">The release note section of the body, and whether more followed it.</param>
/// <param name="LinkedIssues">The issues that the body says the pull request fixes, as URLs.</param>
public sealed record PullRequestSummary(
	int Number,
	string Title,
	IReadOnlyList<string> Labels,
	ReleaseNoteExtractionResult ReleaseNote,
	IReadOnlyList<string> LinkedIssues
);
