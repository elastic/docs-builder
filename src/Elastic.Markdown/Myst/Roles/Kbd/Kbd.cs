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

	public static KeyboardShortcut Unknown { get; } = new([
		new CharacterKeyNode
		{
			Key = '?'
		}
	]);

	public static KeyboardShortcut Parse(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
			return new KeyboardShortcut([]);

		var keySegments = input.Split('+', StringSplitOptions.RemoveEmptyEntries);
		var keys = keySegments.Select(ParseKey).ToList();
		return new KeyboardShortcut(keys);
	}

	private static IKeyNode ParseKey(string keySegment)
	{
		var trimmedSegment = keySegment.Trim();
		var alternateParts = trimmedSegment.Split('|', StringSplitOptions.RemoveEmptyEntries);

		if (alternateParts.Length > 2)
			throw new ArgumentException($"You can only use two alternate keyboard keys: {keySegment}", nameof(keySegment));

		return alternateParts.Length == 2
			? new AlternateKeyNode { Primary = ParseSingleKey(alternateParts[0]), Alternate = ParseSingleKey(alternateParts[1]) }
			: ParseSingleKey(trimmedSegment);
	}

	private static IKeyNode ParseSingleKey(string key)
	{
		var trimmedKey = key.Trim().ToLowerInvariant();
		if (NamedKeyboardKeyExtensions.TryParse(trimmedKey, out var namedKey, true, true))
			return new NamedKeyNode { Key = namedKey };

		if (trimmedKey.Length == 1)
			return new CharacterKeyNode { Key = trimmedKey[0] };

		throw new ArgumentException($"Unknown keyboard key: {key}", nameof(key));
	}

	public static string Render(KeyboardShortcut shortcut)
	{
		var viewModels = shortcut.Keys.Select(ToViewModel);
		var kbdElements = viewModels.Select(viewModel => viewModel switch
		{
			SingleKeyboardKeyViewModel singleKeyboardKeyViewModel => Render(singleKeyboardKeyViewModel),
			AlternateKeyboardKeyViewModel alternateKeyboardKeyViewModel => Render(alternateKeyboardKeyViewModel),
			_ => throw new ArgumentException($"Unsupported key: {viewModel}", nameof(viewModel))
		});
		return string.Join(" + ", kbdElements);
	}

	public static string RenderLlm(KeyboardShortcut shortcut)
	{
		var viewModels = shortcut.Keys.Select(ToViewModel);
		var kbdElements = viewModels.Select(viewModel => viewModel switch
		{
			SingleKeyboardKeyViewModel singleKeyboardKeyViewModel => RenderLlm(singleKeyboardKeyViewModel),
			AlternateKeyboardKeyViewModel alternateKeyboardKeyViewModel => RenderLlm(alternateKeyboardKeyViewModel),
			_ => throw new ArgumentException($"Unsupported key: {viewModel}", nameof(viewModel))
		});
		return string.Join(" + ", kbdElements);
	}

	private static string RenderLlm(SingleKeyboardKeyViewModel singleKeyboardKeyViewModel)
	{
		var sb = new StringBuilder();
		_ = sb.Append("<kbd>");
		_ = sb.Append(singleKeyboardKeyViewModel.DisplayText);
		_ = sb.Append("</kbd>");
		return sb.ToString();
	}

	private static string RenderLlm(AlternateKeyboardKeyViewModel alternateKeyboardKeyViewModel)
	{
		var sb = new StringBuilder();
		_ = sb.Append("<kbd>");
		_ = sb.Append(alternateKeyboardKeyViewModel.Primary.DisplayText);
		_ = sb.Append(" / ");
		_ = sb.Append(alternateKeyboardKeyViewModel.Alternate.DisplayText);
		_ = sb.Append("</kbd>");
		return sb.ToString();
	}

	private static string Render(AlternateKeyboardKeyViewModel alternateKeyboardKeyViewModel)
	{
		var sb = new StringBuilder();
		_ = sb.Append("<kbd class=\"kbd\"");
		if (alternateKeyboardKeyViewModel.Primary.AriaLabel is not null)
			_ = sb.Append(" aria-label=\"" + alternateKeyboardKeyViewModel.Primary.AriaLabel + " or " + alternateKeyboardKeyViewModel.Alternate.AriaLabel + "\"");
		_ = sb.Append('>');

		if (alternateKeyboardKeyViewModel.Primary.UnicodeIcon is not null)
			_ = sb.Append($"<span class=\"kbd-icon\">{alternateKeyboardKeyViewModel.Primary.UnicodeIcon}</span>");
		_ = sb.Append(alternateKeyboardKeyViewModel.Primary.DisplayText);

		_ = sb.Append("<span class=\"kbd-separator\"></span>");

		if (alternateKeyboardKeyViewModel.Alternate.UnicodeIcon is not null)
			_ = sb.Append($"<span class=\"kbd-icon\">{alternateKeyboardKeyViewModel.Alternate.UnicodeIcon}</span>");
		_ = sb.Append(alternateKeyboardKeyViewModel.Alternate.DisplayText);
		_ = sb.Append("</kbd>");
		return sb.ToString();
	}

	private static string Render(SingleKeyboardKeyViewModel singleKeyboardKeyViewModel)
	{
		var sb = new StringBuilder();
		_ = sb.Append("<kbd class=\"kbd\"");
		if (singleKeyboardKeyViewModel.AriaLabel is not null)
			_ = sb.Append(" aria-label=\"" + singleKeyboardKeyViewModel.AriaLabel + "\"");
		_ = sb.Append('>');
		if (singleKeyboardKeyViewModel.UnicodeIcon is not null)
			_ = sb.Append($"<span class=\"kbd-icon\">{singleKeyboardKeyViewModel.UnicodeIcon}</span>");
		_ = sb.Append(singleKeyboardKeyViewModel.DisplayText);
		_ = sb.Append("</kbd>");
		return sb.ToString();
	}

	private static IKeyboardViewModel ToViewModel(IKeyNode keyNode) =>
		keyNode switch
		{
			AlternateKeyNode alternateKeyNode => ToViewModel(alternateKeyNode),
			CharacterKeyNode characterKeyNode => ToViewModel(characterKeyNode),
			NamedKeyNode namedKeyNode => ToViewModel(namedKeyNode),
			_ => throw new ArgumentException($"Unknown key: {keyNode}")
		};

	private static AlternateKeyboardKeyViewModel ToViewModel(AlternateKeyNode keyNode) =>
		new()
		{
			Primary = keyNode.Primary switch
			{
				NamedKeyNode namedKeyNode => ToViewModel(namedKeyNode),
				CharacterKeyNode characterKeyNode => ToViewModel(characterKeyNode),
				_ => throw new ArgumentException($"Unsupported key: {keyNode.Primary}")
			},
			Alternate = keyNode.Alternate switch
			{
				NamedKeyNode namedKeyNode => ToViewModel(namedKeyNode),
				CharacterKeyNode characterKeyNode => ToViewModel(characterKeyNode),
				_ => throw new ArgumentException($"Unsupported key: {keyNode.Primary}")
			},
		};

	private static SingleKeyboardKeyViewModel ToViewModel(CharacterKeyNode keyNode) => new()
	{
		DisplayText = HttpUtility.HtmlEncode(keyNode.Key.ToString()),
		UnicodeIcon = null
	};

	private static SingleKeyboardKeyViewModel ToViewModel(NamedKeyNode keyNode) => ViewModelMapping[keyNode.Key];

	private static FrozenDictionary<NamedKeyboardKey, SingleKeyboardKeyViewModel> ViewModelMapping { get; } =
		Enum.GetValues<NamedKeyboardKey>().ToFrozenDictionary(k => k, GetDisplayModel);

	private static SingleKeyboardKeyViewModel GetDisplayModel(NamedKeyboardKey key) =>
		key switch
		{
			// Modifier keys with special symbols
			NamedKeyboardKey.Command => new SingleKeyboardKeyViewModel
			{
				DisplayText = "Cmd",
				UnicodeIcon = "⌘",
				AriaLabel = "Command"
			},
			NamedKeyboardKey.Shift => new SingleKeyboardKeyViewModel
			{
				DisplayText = "Shift",
				UnicodeIcon = "⇧"
			},
			NamedKeyboardKey.Ctrl => new SingleKeyboardKeyViewModel
			{
				DisplayText = "Ctrl",
				UnicodeIcon = "⌃",
				AriaLabel = "Control"
			},
			NamedKeyboardKey.Alt => new SingleKeyboardKeyViewModel
			{
				DisplayText = "Alt",
				UnicodeIcon = "⌥"
			},
			NamedKeyboardKey.Option => new SingleKeyboardKeyViewModel
			{
				DisplayText = "Opt",
				UnicodeIcon = "⌥",
				AriaLabel = "Option"
			},
			NamedKeyboardKey.Win => new SingleKeyboardKeyViewModel
			{
				DisplayText = "Win",
				UnicodeIcon = "⊞",
				AriaLabel = "Windows"
			},
			// Directional keys
			NamedKeyboardKey.Up => new SingleKeyboardKeyViewModel
			{
				DisplayText = "Up",
				UnicodeIcon = "↑",
				AriaLabel = "Up Arrow"
			},
			NamedKeyboardKey.Down => new SingleKeyboardKeyViewModel
			{
				DisplayText = "Down",
				UnicodeIcon = "↓",
				AriaLabel = "Down Arrow"
			},
			NamedKeyboardKey.Left => new SingleKeyboardKeyViewModel
			{
				DisplayText = "Left",
				UnicodeIcon = "←",
				AriaLabel = "Left Arrow"
			},
			NamedKeyboardKey.Right => new SingleKeyboardKeyViewModel
			{
				DisplayText = "Right",
				UnicodeIcon = "→",
				AriaLabel = "Right Arrow"
			},
			// Other special keys with symbols
			NamedKeyboardKey.Enter => new SingleKeyboardKeyViewModel
			{
				DisplayText = "Enter",
				UnicodeIcon = "↵"
			},
			NamedKeyboardKey.Escape => new SingleKeyboardKeyViewModel
			{
				DisplayText = "Esc",
				UnicodeIcon = "⎋",
				AriaLabel = "Escape"
			},
			NamedKeyboardKey.Tab => new SingleKeyboardKeyViewModel
			{
				DisplayText = "Tab",
				UnicodeIcon = "↹",
				AriaLabel = "Tab"
			},
			NamedKeyboardKey.Backspace => new SingleKeyboardKeyViewModel
			{
				DisplayText = "Backspace",
				UnicodeIcon = "⌫"
			},
			NamedKeyboardKey.Delete => new SingleKeyboardKeyViewModel
			{
				DisplayText = "Del",
				AriaLabel = "Delete"
			},
			NamedKeyboardKey.Home => new SingleKeyboardKeyViewModel
			{
				DisplayText = "Home",
				UnicodeIcon = "⇱"
			},
			NamedKeyboardKey.End => new SingleKeyboardKeyViewModel
			{
				DisplayText = "End",
				UnicodeIcon = "⇲"
			},
			NamedKeyboardKey.PageUp => new SingleKeyboardKeyViewModel
			{
				DisplayText = "PageUp",
				UnicodeIcon = "⇞",
				AriaLabel = "Page Up"
			},
			NamedKeyboardKey.PageDown => new SingleKeyboardKeyViewModel
			{
				DisplayText = "PageDown",
				UnicodeIcon = "⇟",
				AriaLabel = "Page Down"
			},
			NamedKeyboardKey.Space => new SingleKeyboardKeyViewModel
			{
				DisplayText = "Space",
				UnicodeIcon = "␣"
			},
			NamedKeyboardKey.Insert => new SingleKeyboardKeyViewModel
			{
				DisplayText = "Ins",
				AriaLabel = "Insert"
			},
			NamedKeyboardKey.Plus => new SingleKeyboardKeyViewModel
			{
				DisplayText = "+",
			},
			NamedKeyboardKey.Pipe => new SingleKeyboardKeyViewModel
			{
				DisplayText = "|",
				AriaLabel = "Pipe"
			},
			NamedKeyboardKey.Fn => new SingleKeyboardKeyViewModel
			{
				DisplayText = "Fn",
				AriaLabel = "Function key"
			},
			NamedKeyboardKey.F1 => new SingleKeyboardKeyViewModel
			{
				DisplayText = "F1",
			},
			NamedKeyboardKey.F2 => new SingleKeyboardKeyViewModel
			{
				DisplayText = "F2",
			},
			NamedKeyboardKey.F3 => new SingleKeyboardKeyViewModel
			{
				DisplayText = "F3",
			},
			NamedKeyboardKey.F4 => new SingleKeyboardKeyViewModel
			{
				DisplayText = "F4",
			},
			NamedKeyboardKey.F5 => new SingleKeyboardKeyViewModel
			{
				DisplayText = "F5",
				UnicodeIcon = null
			},
			NamedKeyboardKey.F6 => new SingleKeyboardKeyViewModel
			{
				DisplayText = "F6",
			},
			NamedKeyboardKey.F7 => new SingleKeyboardKeyViewModel
			{
				DisplayText = "F7",
			},
			NamedKeyboardKey.F8 => new SingleKeyboardKeyViewModel
			{
				DisplayText = "F8",
			},
			NamedKeyboardKey.F9 => new SingleKeyboardKeyViewModel
			{
				DisplayText = "F9",
			},
			NamedKeyboardKey.F10 => new SingleKeyboardKeyViewModel
			{
				DisplayText = "F10",
			},
			NamedKeyboardKey.F11 => new SingleKeyboardKeyViewModel
			{
				DisplayText = "F11",
			},
			NamedKeyboardKey.F12 => new SingleKeyboardKeyViewModel
			{
				DisplayText = "F12",
			},
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
	[Display(Name = "fn")] Fn,
	[Display(Name = "pipe")] Pipe
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

public interface IKeyboardViewModel;

public record SingleKeyboardKeyViewModel : IKeyboardViewModel
{
	public string? UnicodeIcon { get; init; }
	public required string DisplayText { get; init; }
	public string? AriaLabel { get; init; }
}

public record AlternateKeyboardKeyViewModel : IKeyboardViewModel
{
	public required SingleKeyboardKeyViewModel Primary { get; init; }
	public required SingleKeyboardKeyViewModel Alternate { get; init; }
}

public class AlternateKeyNode : IKeyNode
{
	public required IKeyNode Primary { get; init; }
	public required IKeyNode Alternate { get; init; }
}
