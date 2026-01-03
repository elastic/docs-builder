// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Services.Changelog;

/// <summary>
/// Input for a single bundle file with optional directory and repo
/// </summary>
public class BundleInput
{
	public string BundleFile { get; set; } = string.Empty;
	public string? Directory { get; set; }
	public string? Repo { get; set; }
}

