// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.SearchDialog;

public static class SearchDialogExamples
{
	public static ComponentSection Dialogs { get; } = new(
		"Search dialog",
		"A native dialog opened by the search field or the keyboard shortcut. Results are drawn into the list from a template, so there is one markup source. It is closed here; open it from the header on a page.",
		"search-dialog",
		[ComponentExample.Of("Closed dialog markup", _SearchDialog.Create, new SearchDialogModel { Id = "search-dialog-example" })]
	);
}
