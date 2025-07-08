// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Web;
using NetEscapades.EnumGenerators;

namespace Elastic.Markdown.Myst.Roles.Kbd;

public class KeyboardShortcut(IReadOnlyList<IKeyNode> keys)
{
	private IReadOnlyList<IKeyNode> Keys { get; } = keys;

	public static KeyboardShortcut Empty { get; } = new([]);

	public static KeyboardShortcut Parse(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
			return new KeyboardShortcut([]);

		var parts = input.Split('+', StringSplitOptions.RemoveEmptyEntries);
		var keys = new List<IKeyNode>();

		foreach (var part in parts)
		{
			var trimmedPart = part.Trim().ToLowerInvariant();
			if (NamedKeyboardKeyExtensions.TryParse(trimmedPart, out var specialKey, true, true))
				keys.Add(new NamedKeyNode { Key = specialKey });
			else
			{
				switch (trimmedPart.Length)
				{
					case 1:
						keys.Add(new CharacterKeyNode { Key = trimmedPart[0] });
						break;
					default:
						throw new ArgumentException($"Invalid keyboard shortcut: {input}", nameof(input));
				}
			}
		}
		return new KeyboardShortcut(keys);
	}

	public static string Render(KeyboardShortcut shortcut)
	{
		var viewModels = shortcut.Keys.Select(keyNode =>
		{
			return keyNode switch
			{
				NamedKeyNode s => ViewModelMapping[s.Key],
				CharacterKeyNode c => new KeyboardKeyViewModel { DisplayText = c.Key.ToString(), UnicodeIcon = null },
				_ => throw new ArgumentException($"Unknown key: {keyNode}")
			};
		});

		var kbdElements = viewModels.Select(viewModel =>
		{
			var sb = new StringBuilder();
			_ = sb.Append("<kbd class=\"kbd\">");
			if (viewModel.UnicodeIcon is not null)
				_ = sb.Append($"<span class=\"kbd-icon\">{viewModel.UnicodeIcon}</span>");
			_ = sb.Append(viewModel.DisplayText);
			_ = sb.Append("</kbd>");
			return sb.ToString();
		});

		return string.Join(" + ", kbdElements);
	}

	private static FrozenDictionary<NamedKeyboardKey, KeyboardKeyViewModel> ViewModelMapping { get; } =
		Enum.GetValues<NamedKeyboardKey>().ToFrozenDictionary(k => k, GetDisplayModel);

	private static KeyboardKeyViewModel GetDisplayModel(NamedKeyboardKey key) =>
		key switch
		{
			// Modifier keys with special symbols
			NamedKeyboardKey.Command => new KeyboardKeyViewModel { DisplayText = "Cmd", UnicodeIcon = "⌘" },
			NamedKeyboardKey.Shift => new KeyboardKeyViewModel { DisplayText = "Shift", UnicodeIcon = "⇧" },
			NamedKeyboardKey.Ctrl => new KeyboardKeyViewModel { DisplayText = "Ctrl", UnicodeIcon = "⌃" },
			NamedKeyboardKey.Alt => new KeyboardKeyViewModel { DisplayText = "Alt", UnicodeIcon = "⌥" },
			NamedKeyboardKey.Option => new KeyboardKeyViewModel { DisplayText = "Option", UnicodeIcon = "⌥" },
			NamedKeyboardKey.Win => new KeyboardKeyViewModel { DisplayText = "Win", UnicodeIcon = "⊞" },
			// Directional keys
			NamedKeyboardKey.Up => new KeyboardKeyViewModel { DisplayText = "Up", UnicodeIcon = "↑" },
			NamedKeyboardKey.Down => new KeyboardKeyViewModel { DisplayText = "Down", UnicodeIcon = "↓" },
			NamedKeyboardKey.Left => new KeyboardKeyViewModel { DisplayText = "Left", UnicodeIcon = "←" },
			NamedKeyboardKey.Right => new KeyboardKeyViewModel { DisplayText = "Right", UnicodeIcon = "→" },
			// Other special keys with symbols
			NamedKeyboardKey.Enter => new KeyboardKeyViewModel { DisplayText = "Enter", UnicodeIcon = "↵" },
			NamedKeyboardKey.Escape => new KeyboardKeyViewModel { DisplayText = "Esc", UnicodeIcon = "⎋" },
			NamedKeyboardKey.Tab => new KeyboardKeyViewModel { DisplayText = "Tab", UnicodeIcon = "↹" },
			NamedKeyboardKey.Backspace => new KeyboardKeyViewModel { DisplayText = "Backspace", UnicodeIcon = "⌫" },
			NamedKeyboardKey.Delete => new KeyboardKeyViewModel { DisplayText = "Del", UnicodeIcon = null },
			NamedKeyboardKey.Home => new KeyboardKeyViewModel { DisplayText = "Home", UnicodeIcon = "⇱" },
			NamedKeyboardKey.End => new KeyboardKeyViewModel { DisplayText = "End", UnicodeIcon = "⇲" },
			NamedKeyboardKey.PageUp => new KeyboardKeyViewModel { DisplayText = "PageUp", UnicodeIcon = "⇞" },
			NamedKeyboardKey.PageDown => new KeyboardKeyViewModel { DisplayText = "PageDown", UnicodeIcon = "⇟" },
			NamedKeyboardKey.Space => new KeyboardKeyViewModel { DisplayText = "Space", UnicodeIcon = "␣" },
			NamedKeyboardKey.Insert => new KeyboardKeyViewModel { DisplayText = "Ins", UnicodeIcon = null },
			NamedKeyboardKey.Plus => new KeyboardKeyViewModel { DisplayText = "+", UnicodeIcon = null },
			NamedKeyboardKey.Fn => new KeyboardKeyViewModel { DisplayText = "Fn", UnicodeIcon = null },
			NamedKeyboardKey.F1 => new KeyboardKeyViewModel { DisplayText = "F1", UnicodeIcon = null },
			NamedKeyboardKey.F2 => new KeyboardKeyViewModel { DisplayText = "F2", UnicodeIcon = null },
			NamedKeyboardKey.F3 => new KeyboardKeyViewModel { DisplayText = "F3", UnicodeIcon = null },
			NamedKeyboardKey.F4 => new KeyboardKeyViewModel { DisplayText = "F4", UnicodeIcon = null },
			NamedKeyboardKey.F5 => new KeyboardKeyViewModel { DisplayText = "F5", UnicodeIcon = null },
			NamedKeyboardKey.F6 => new KeyboardKeyViewModel { DisplayText = "F6", UnicodeIcon = null },
			NamedKeyboardKey.F7 => new KeyboardKeyViewModel { DisplayText = "F7", UnicodeIcon = null },
			NamedKeyboardKey.F8 => new KeyboardKeyViewModel { DisplayText = "F8", UnicodeIcon = null },
			NamedKeyboardKey.F9 => new KeyboardKeyViewModel { DisplayText = "F9", UnicodeIcon = null },
			NamedKeyboardKey.F10 => new KeyboardKeyViewModel { DisplayText = "F10", UnicodeIcon = null },
			NamedKeyboardKey.F11 => new KeyboardKeyViewModel { DisplayText = "F11", UnicodeIcon = null },
			NamedKeyboardKey.F12 => new KeyboardKeyViewModel { DisplayText = "F12", UnicodeIcon = null },
			// Function keys
			_ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
		};
}

[EnumExtensions]
public enum NamedKeyboardKey
{
	// Modifier Keys
	[Display(Name = "shift")] Shift,
	[Display(Name = "ctrl")] Ctrl,
	[Display(Name = "alt")] Alt,
	[Display(Name = "option")] Option,
	[Display(Name = "cmd")] Command,
	[Display(Name = "win")] Win,

	// Directional Keys
	[Display(Name = "up")] Up,
	[Display(Name = "down")] Down,
	[Display(Name = "left")] Left,
	[Display(Name = "right")] Right,

	// Control Keys
	[Display(Name = "space")] Space,
	[Display(Name = "tab")] Tab,
	[Display(Name = "enter")] Enter,
	[Display(Name = "esc")] Escape,
	[Display(Name = "backspace")] Backspace,
	[Display(Name = "del")] Delete,
	[Display(Name = "ins")] Insert,

	// Navigation Keys
	[Display(Name = "pageup")] PageUp,
	[Display(Name = "pagedown")] PageDown,
	[Display(Name = "home")] Home,
	[Display(Name = "end")] End,

	// Function Keys
	[Display(Name = "f1")] F1,
	[Display(Name = "f2")] F2,
	[Display(Name = "f3")] F3,
	[Display(Name = "f4")] F4,
	[Display(Name = "f5")] F5,
	[Display(Name = "f6")] F6,
	[Display(Name = "f7")] F7,
	[Display(Name = "f8")] F8,
	[Display(Name = "f9")] F9,
	[Display(Name = "f10")] F10,
	[Display(Name = "f11")] F11,
	[Display(Name = "f12")] F12,

	// Other Keys
	[Display(Name = "plus")] Plus,
	[Display(Name = "fn")] Fn
}

public class IKeyNode;

public class NamedKeyNode : IKeyNode
{
	public required NamedKeyboardKey Key { get; init; }
}

public class CharacterKeyNode : IKeyNode
{
	public required char Key { get; init; }
}

public record KeyboardKeyViewModel
{
	public required string? UnicodeIcon { get; init; }
	public required string DisplayText { get; init; }
}
