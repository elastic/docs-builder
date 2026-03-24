// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Diagnostics;

/// <summary>
/// Types of hints that can be emitted during documentation processing.
/// </summary>
public enum HintType
{
	/// <summary>
	/// Hint about deep-linking virtual files (files with paths that have children).
	/// Suggests using 'folder' instead of 'file' for better navigation structure.
	/// </summary>
	DeepLinkingVirtualFile,

	/// <summary>
	/// Hint about file name not matching folder name in folder+file combinations.
	/// Best practice is to name the file the same as the folder.
	/// </summary>
	FolderFileNameMismatch,

	/// <summary>
	/// Hint about autolinks pointing to elastic.co/docs that should use crosslinks or relative links instead.
	/// </summary>
	AutolinkElasticCoDocs
}
