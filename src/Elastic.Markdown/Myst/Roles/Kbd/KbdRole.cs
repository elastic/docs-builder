// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;

namespace Elastic.Markdown.Myst.Roles.Kbd;

[DebuggerDisplay("{GetType().Name} Line: {Line}, Role: {Role}, Content: {Content}")]
public class KbdRole(string role, string content) : RoleLeaf(role, content)
{
	public KeyboardShortcut KeyboardShortcut { get; } = KeyboardShortcut.Parse(content);
}
