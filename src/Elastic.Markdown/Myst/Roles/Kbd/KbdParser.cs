// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Markdown.Diagnostics;
using Markdig.Parsers;

namespace Elastic.Markdown.Myst.Roles.Kbd;

public class KbdParser : RoleParser<KbdRole>
{

	protected override KbdRole CreateRole(string role, string content, InlineProcessor parserContext)
		=> new(role, content, parserContext);

	protected override bool Matches(ReadOnlySpan<char> role) => role is "{kbd}";
}
