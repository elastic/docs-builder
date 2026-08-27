// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Builds an amend <see cref="Bundle"/> document without any filesystem dependency,
/// so both the CLI amend service and the Lambda's <c>NoteAmendReconciler</c> can reuse it.
/// </summary>
public static class AmendDocumentBuilder
{
	/// <summary>
	/// Builds an amend bundle that copies the parent's products (so registry routing and
	/// <c>:version:</c> selection work), records the supplied exclusions, and adds the supplied entries.
	/// </summary>
	public static Bundle Build(
		IReadOnlyList<BundledProduct> parentProducts,
		IReadOnlyList<BundledEntry> entriesToAdd,
		IReadOnlyList<BundledEntry> exclusions) =>
		new()
		{
			Products = parentProducts,
			ExcludeEntries = exclusions,
			Entries = entriesToAdd
		};
}
