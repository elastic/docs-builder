// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Markdown.Diagnostics;
using Markdig.Parsers;

namespace Elastic.Markdown.Myst.Roles.Kbd;

[DebuggerDisplay("{GetType().Name} Line: {Line}, Role: {Role}, Content: {Content}")]
public class KbdRole : RoleLeaf
{
	public KbdRole(string role, string content, InlineProcessor parserContext) : base(role, content)
	{
		try
		{
			KeyboardShortcut = KeyboardShortcut.Parse(content);
		}
		catch (Exception ex)
		{
			parserContext.EmitError(this, Role.Length + content.Length, $"Failed to parse keyboard shortcut: \"{content}\"", ex);
			KeyboardShortcut = KeyboardShortcut.Empty;
		}
	}
	public KeyboardShortcut KeyboardShortcut { get; }
}
