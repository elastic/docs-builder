// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Svg;

/// <summary>
/// Provides access to EUI SVG icons embedded as resources.
/// Icons are sourced from https://eui.elastic.co/docs/components/display/icons/
/// </summary>
public static class EuiSvgIcons
{
	private static readonly Lazy<IReadOnlyDictionary<string, string>> LazyIconMap = new(LoadIcons);
	private static readonly Lazy<IReadOnlyDictionary<string, string>> LazyTokenMap = new(LoadTokens);

	// Maps old icon names (camelCase or superseded) to their current canonical EUI snake_case names.
	// Keeps existing docs working when EUI renames icons without requiring a bulk docs update.
	// Source: https://github.com/elastic/eui/pull/9279 and subsequent renames.
	private static readonly Dictionary<string, string> IconAliases = new()
	{
		["analyzeEvent"] = "analyze_event",
		["arrowEnd"] = "chevron_limit_right",
		["arrowStart"] = "chevron_limit_left",
		["bellSlash"] = "bell_slash",
		["branchUser"] = "branch_user",
		["checkCircle"] = "check_circle",
		["checkInCircleFilled"] = "check_circle_fill",
		["clickLeft"] = "click_left",
		["clickRight"] = "click_right",
		["clockCounter"] = "clock_counter",
		["cloudDrizzle"] = "cloud_drizzle",
		["cloudStormy"] = "cloud_stormy",
		["cloudSunny"] = "cloud_sunny",
		["continuityAbove"] = "continuity_above",
		["continuityAboveBelow"] = "continuity_above_below",
		["continuityBelow"] = "continuity_below",
		["continuityWithin"] = "continuity_within",
		["contrastHigh"] = "contrast_fill",
		["cross_in_circle"] = "cross_circle",
		["documentEdit"] = "document_edit",
		["dotInCircle"] = "dot_in_circle",
		["doubleArrowLeft"] = "chevron_double_left",
		["doubleArrowRight"] = "chevron_double_right",
		["editorDistributeHorizontal"] = "editor_distribute_horizontal",
		["editorDistributeVertical"] = "editor_distribute_vertical",
		["editorItemAlignBottom"] = "editor_item_align_bottom",
		["editorItemAlignCenter"] = "editor_item_align_center",
		["editorItemAlignLeft"] = "editor_item_align_left",
		["editorItemAlignMiddle"] = "editor_item_align_middle",
		["editorItemAlignRight"] = "editor_item_align_right",
		["editorItemAlignTop"] = "editor_item_align_top",
		["editorPositionBottomLeft"] = "editor_position_bottom_left",
		["editorPositionBottomRight"] = "editor_position_bottom_right",
		["editorPositionTopLeft"] = "editor_position_top_left",
		["editorPositionTopRight"] = "editor_position_top_right",
		["errorFilled"] = "error_fill",
		["esqlVis"] = "esql_vis",
		["expandMini"] = "expand",
		["export"] = "external",
		["filterExclude"] = "filter_exclude",
		["filterIgnore"] = "filter_ignore",
		["filterInCircle"] = "filter_in_circle",
		["filterInclude"] = "filter_include",
		["folder_closed"] = "folder_close",
		["frameNext"] = "frame_next",
		["framePrevious"] = "frame_previous",
		["fullScreenExit"] = "full_screen_exit",
		["grabOmnidirectional"] = "grab_omnidirectional",
		["inputOutput"] = "input_output",
		["kubernetesNode"] = "kubernetes_node",
		["kubernetesPod"] = "kubernetes_pod",
		["lineDashed"] = "line_dash",
		["lineDotted"] = "line_dot",
		["lineSolid"] = "line_solid",
		["lockOpen"] = "lock_open",
		["magnifyWithExclamation"] = "magnify_with_exclamation",
		["magnifyWithMinus"] = "magnify_with_minus",
		["magnifyWithPlus"] = "magnify_with_plus",
		["menuDown"] = "menu_down",
		["menuLeft"] = "menu_left",
		["menuRight"] = "menu_right",
		["menuUp"] = "menu_up",
		["minus_in_circle"] = "minus_circle",
		["minus_in_circle_filled"] = "minus_circle",
		["minus_in_square"] = "minus_square",
		["pageSelect"] = "page_select",
		["pagesSelect"] = "pages_select",
		["pin_filled"] = "pin_fill",
		["pipeBreaks"] = "line_break",
		["pipeNoBreaks"] = "line_break_slash",
		["playFilled"] = "play_filled",
		["plus_in_circle"] = "plus_circle",
		["plus_in_square"] = "plus_square",
		["readOnly"] = "read_only",
		["securitySignal"] = "security_signal",
		["securitySignalDetected"] = "security_signal_detected",
		["securitySignalResolved"] = "security_signal_resolved",
		["sessionViewer"] = "session_viewer",
		["sortAscending"] = "sort_ascending",
		["sortDescending"] = "sort_descending",
		["sortLeft"] = "sort_left",
		["sortRight"] = "sort_right",
		["starPlusEmpty"] = "star_plus_empty",
		["starPlusFilled"] = "star_plus_fill",
		["star_filled"] = "star_fill",
		["star_filled_space"] = "star_fill_space",
		["star_minus_filled"] = "star_minus_fill",
		["stop_filled"] = "stop_fill",
		["tableOfContents"] = "table_of_contents",
		["thumbDown"] = "thumb_down",
		["thumbUp"] = "thumb_up",
		["timeRefresh"] = "refresh_time",
		["timelineWithArrow"] = "timeline_with_arrow",
		["transitionLeftIn"] = "transition_left_in",
		["transitionLeftOut"] = "transition_left_out",
		["transitionTopIn"] = "transition_top_in",
		["transitionTopOut"] = "transition_top_out",
		["videoPlayer"] = "video_player",
		["vis_metric"] = "chart_metric",
		["warningFilled"] = "warning_fill",
		["wordWrap"] = "word_wrap",
		["wordWrapDisabled"] = "word_wrap_disabled",
	};

	// Maps old token names (camelCase) to their current canonical EUI snake_case names.
	private static readonly Dictionary<string, string> TokenAliases = new()
	{
		["tokenAlias"] = "token_alias",
		["tokenAnnotation"] = "token_annotation",
		["tokenArray"] = "token_array",
		["tokenBinary"] = "token_binary",
		["tokenBoolean"] = "token_boolean",
		["tokenClass"] = "token_class",
		["tokenCompletionSuggester"] = "token_completion_suggester",
		["tokenConstant"] = "token_constant",
		["tokenDate"] = "token_date",
		["tokenDimension"] = "token_dimension",
		["tokenElement"] = "token_element",
		["tokenEnum"] = "token_enum",
		["tokenEnumMember"] = "token_enum_member",
		["tokenEvent"] = "token_event",
		["tokenException"] = "token_exception",
		["tokenField"] = "token_field",
		["tokenFile"] = "token_file",
		["tokenFlattened"] = "token_flattened",
		["tokenFunction"] = "token_function",
		["tokenGeo"] = "token_geo",
		["tokenHistogram"] = "token_histogram",
		["tokenIP"] = "token_ip",
		["tokenInterface"] = "token_interface",
		["tokenJoin"] = "token_join",
		["tokenKey"] = "token_key",
		["tokenKeyword"] = "token_keyword",
		["tokenMethod"] = "token_method",
		["tokenMetricCounter"] = "token_metric_counter",
		["tokenMetricGauge"] = "token_metric_gauge",
		["tokenModule"] = "token_module",
		["tokenNamespace"] = "token_namespace",
		["tokenNested"] = "token_nested",
		["tokenNull"] = "token_null",
		["tokenNumber"] = "token_number",
		["tokenObject"] = "token_object",
		["tokenOperator"] = "token_operator",
		["tokenPackage"] = "token_package",
		["tokenParameter"] = "token_parameter",
		["tokenPercolator"] = "token_percolator",
		["tokenProperty"] = "token_property",
		["tokenRange"] = "token_range",
		["tokenRankFeature"] = "token_rank_feature",
		["tokenRankFeatures"] = "token_rank_features",
		["tokenRepo"] = "token_repo",
		["tokenSearchType"] = "token_search_type",
		["tokenSemanticText"] = "token_semantic_text",
		["tokenShape"] = "token_shape",
		["tokenString"] = "token_string",
		["tokenStruct"] = "token_struct",
		["tokenSymbol"] = "token_symbol",
		["tokenTag"] = "token_tag",
		["tokenText"] = "token_text",
		["tokenTokenCount"] = "token_token_count",
		["tokenVariable"] = "token_variable",
		["tokenVectorDense"] = "token_vector_dense",
		["tokenVectorSparse"] = "token_vector_sparse",
	};

	/// <summary>
	/// Dictionary of icon names to their SVG content.
	/// </summary>
	public static IReadOnlyDictionary<string, string> Icons => LazyIconMap.Value;

	/// <summary>
	/// Dictionary of token names to their SVG content.
	/// </summary>
	public static IReadOnlyDictionary<string, string> Tokens => LazyTokenMap.Value;

	/// <summary>
	/// Tries to get an icon SVG by name, resolving aliases for renamed icons transparently.
	/// </summary>
	/// <param name="name">The icon name (without .svg extension)</param>
	/// <param name="svg">The SVG content if found</param>
	/// <returns>True if the icon was found, false otherwise</returns>
	public static bool TryGetIcon(string name, out string? svg)
	{
		if (IconAliases.TryGetValue(name, out var canonical))
			name = canonical;
		return Icons.TryGetValue(name, out svg);
	}

	/// <summary>
	/// Tries to get a token SVG by name, resolving aliases for renamed tokens transparently.
	/// </summary>
	/// <param name="name">The token name (without .svg extension)</param>
	/// <param name="svg">The SVG content if found</param>
	/// <returns>True if the token was found, false otherwise</returns>
	public static bool TryGetToken(string name, out string? svg)
	{
		if (TokenAliases.TryGetValue(name, out var canonical))
			name = canonical;
		return Tokens.TryGetValue(name, out svg);
	}

	/// <summary>
	/// Gets an icon SVG by name, returning null if not found.
	/// </summary>
	/// <param name="name">The icon name (without .svg extension)</param>
	/// <returns>The SVG content or null if not found</returns>
	public static string? GetIcon(string name)
	{
		if (IconAliases.TryGetValue(name, out var canonical))
			name = canonical;
		return Icons.TryGetValue(name, out var svg) ? svg : null;
	}

	/// <summary>
	/// Gets an icon SVG by name with an optional CSS class injected into the svg element.
	/// </summary>
	/// <param name="name">The icon name (without .svg extension)</param>
	/// <param name="cssClass">Optional CSS class to add to the svg element</param>
	/// <returns>The SVG content or null if not found</returns>
	public static string? GetIcon(string name, string? cssClass)
	{
		if (IconAliases.TryGetValue(name, out var canonical))
			name = canonical;
		return Icons.TryGetValue(name, out var svg)
			? cssClass is not null ? InjectClass(svg, cssClass) : svg
			: null;
	}

	private static string InjectClass(string svg, string cssClass) =>
		svg.Replace("<svg ", $"<svg class=\"{cssClass}\" ");

	/// <summary>
	/// Gets a token SVG by name, returning null if not found.
	/// </summary>
	/// <param name="name">The token name (without .svg extension)</param>
	/// <returns>The SVG content or null if not found</returns>
	public static string? GetToken(string name) =>
		Tokens.TryGetValue(name, out var svg) ? svg : null;

	private static IReadOnlyDictionary<string, string> LoadIcons() =>
		LoadFromPrefix("svgs.");

	private static IReadOnlyDictionary<string, string> LoadTokens() =>
		LoadFromPrefix("svgs.tokens.");

	private static IReadOnlyDictionary<string, string> LoadFromPrefix(string folderPrefix)
	{
		var assembly = typeof(EuiSvgIcons).Assembly;
		var assemblyName = assembly.GetName().Name;
		var fullPrefix = $"{assemblyName}.{folderPrefix}";

		return assembly.GetManifestResourceNames()
			.Where(r => r.StartsWith(fullPrefix, StringComparison.Ordinal) && r.EndsWith(".svg", StringComparison.Ordinal))
			.Where(r =>
			{
				// For the main svgs folder, exclude tokens subfolder
				if (folderPrefix == "svgs.")
				{
					var afterPrefix = r[fullPrefix.Length..];
					return !afterPrefix.StartsWith("tokens.", StringComparison.Ordinal);
				}
				return true;
			})
			.ToDictionary(
				r => r[fullPrefix.Length..^4], // Remove prefix and ".svg" suffix
				r =>
				{
					using var stream = assembly.GetManifestResourceStream(r);
					if (stream is null)
						return string.Empty;
					using var reader = new StreamReader(stream);
					return reader.ReadToEnd();
				}
			);
	}
}
