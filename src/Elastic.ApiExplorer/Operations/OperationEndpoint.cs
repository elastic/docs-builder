// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

/// <summary>One HTTP method and route an operation accepts.</summary>
public sealed record EndpointVariant(string Method, string Route);

/// <summary>One <c>/segment</c> of a route. Optional segments can be left out to call a shorter variant.</summary>
public sealed record EndpointSegment(string Text, bool Optional);

/// <summary>One distinct route of an operation, with its dominant method and the methods that behave the same.</summary>
public sealed record EndpointRow(string Route, string Method, IReadOnlyList<string> AlsoMethods, IReadOnlyList<EndpointSegment> Segments);

/// <summary>
/// Every route and method of one operation merged into display rows: longest route first, the dominant method per
/// route (POST &gt; PUT &gt; PATCH &gt; GET &gt; DELETE &gt; HEAD) with the rest listed as alternatives, and path segments
/// that some variants leave out marked optional.
/// </summary>
public sealed partial record OperationEndpoint(
	IReadOnlyList<EndpointRow> Rows,
	IReadOnlySet<string> OptionalPathParameters,
	string ShortestRoute,
	string? Description
)
{
	private static readonly string[] MethodRank = ["post", "put", "patch", "get", "delete", "head", "options", "trace"];

	/// <summary>
	/// Builds the endpoint from the operation itself, the bump.sh "All methods and paths" HTML in its description
	/// (stripped from <see cref="Description"/>), and sibling operations collapsed onto the same page.
	/// </summary>
	public static OperationEndpoint Build(ApiOperation operation, string? description, IReadOnlyList<ApiOperation> siblings)
	{
		var method = operation.OperationType.Method.ToLowerInvariant();
		// Methods are lowercased, so the record's value equality is the distinctness rule.
		var variants = new List<EndpointVariant> { new(method, operation.Route) };
		variants.AddRange(siblings.Select(static s => new EndpointVariant(s.OperationType.Method.ToLowerInvariant(), s.Route)));

		var stripped = description;
		if (!string.IsNullOrEmpty(description) && PathListing().Match(description) is { Success: true } listing)
		{
			variants.AddRange(ParseBadges(listing.Value));
			stripped = description.Remove(listing.Index, listing.Length).Trim();
		}
		variants = [.. variants.Distinct()];

		// Siblings arrive from the navigation builder only when AreInterchangeable held, and bump.sh lists the
		// variants of one operation, so every variant here behaves the same and the methods merge per route.
		return FromVariants(variants, operation.Route, stripped);
	}

	/// <summary>Applies the display rules to a variant list. <paramref name="specRoute"/> wins ties for the main row.</summary>
	public static OperationEndpoint FromVariants(IReadOnlyList<EndpointVariant> variants, string specRoute, string? description = null)
	{
		// Most segments first; the spec's own route wins ties, then longer text, then ordinal.
		var routes = variants
			.Select(static v => v.Route)
			.Distinct(StringComparer.Ordinal)
			.OrderByDescending(static r => Split(r).Length)
			.ThenByDescending(r => r == specRoute)
			.ThenByDescending(static r => r.Length)
			.ThenBy(static r => r, StringComparer.Ordinal)
			.ToArray();
		var mainSegments = Split(routes[0]);
		var optional = OptionalSegmentIndexes(mainSegments, routes);

		var rows = new List<EndpointRow>();
		foreach (var route in routes)
		{
			var methods = variants
				.Where(v => v.Route == route)
				.Select(static v => v.Method)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(Rank)
				.ToArray();
			var segments = route == routes[0]
				? mainSegments.Select((s, i) => new EndpointSegment(s, optional.Contains(i))).ToArray()
				: Split(route).Select(static s => new EndpointSegment(s, false)).ToArray();

			rows.Add(new EndpointRow(route, methods[0], methods[1..], segments));
		}

		var optionalParameters = optional
			.Select(i => mainSegments[i])
			.Where(IsPlaceholder)
			.Select(static s => s[1..^1])
			.ToHashSet(StringComparer.Ordinal);
		var shortest = routes.OrderBy(static r => Split(r).Length).ThenBy(static r => r.Length).First();
		return new OperationEndpoint(rows, optionalParameters, shortest, description);
	}

	/// <summary>
	/// The operation a collapsed page is built around: the dominant method on the longest route.
	/// Equal-length routes keep spec order.
	/// </summary>
	public static ApiOperation SelectPrimary(IReadOnlyList<ApiOperation> operations)
	{
		var longest = operations.Max(static o => Split(o.Route).Length);
		return operations
			.Where(o => Split(o.Route).Length == longest)
			.GroupBy(static o => o.Route, StringComparer.Ordinal)
			.First()
			.OrderBy(static o => Rank(o.OperationType.Method))
			.First();
	}

	/// <summary>
	/// Separate operations only merge their methods when they are the same call: the same non-path parameters
	/// (name, requiredness and schema), the same request body (requiredness and schema per media type), the
	/// same response schema per status and media type, and the same security requirements.
	/// </summary>
	public static bool AreInterchangeable(IReadOnlyList<ApiOperation> operations)
	{
		var first = Signature(operations[0].Operation);
		return operations.Skip(1).All(o => Signature(o.Operation) == first);
	}

	private static string Signature(OpenApiOperation operation)
	{
		var parameters = (operation.Parameters ?? [])
			.Where(static p => p.In != ParameterLocation.Path)
			.Select(static p => $"{p.In}:{p.Name}:{(p.Required ? "required" : "optional")}:{SchemaKey(p.Schema)}")
			.Order(StringComparer.Ordinal);
		var body = operation.RequestBody is { } requestBody
			? $"{(requestBody.Required ? "required" : "optional")}:{ContentKey(requestBody.Content)}"
			: "";
		var responses = (operation.Responses ?? []).Select(static r => $"{r.Key}:{ContentKey(r.Value?.Content)}").Order(
			StringComparer.Ordinal
		);
		return string.Join('|', parameters) + "#" + body + "#" + string.Join('|', responses) + "#" + SecurityKey(operation.Security);
	}

	/// <summary>
	/// The alternatives a caller may authenticate with, each as its schemes and scopes. Absent means the document's
	/// default applies, which is not the same as an empty list (no authentication), so the two key differently.
	/// </summary>
	private static string SecurityKey(IList<OpenApiSecurityRequirement>? security) =>
		security is null
			? "inherit"
			: string.Join(
				'|',
				security.Select(
					static requirement => string.Join(
						',',
						requirement.Select(static scheme => $"{scheme.Key.Reference.Id}({string.Join(' ', scheme.Value ?? [])})").Order(
							StringComparer.Ordinal
						)
					)
				).Order(StringComparer.Ordinal)
			);

	private static string ContentKey(IDictionary<string, IOpenApiMediaType>? content) =>
		string.Join(
			',',
			(content ?? new Dictionary<string, IOpenApiMediaType>()).Select(static c => $"{c.Key}={SchemaKey(c.Value?.Schema)}").Order(
				StringComparer.Ordinal
			)
		);

	/// <summary>
	/// What a schema is: a reference by name, an inline schema by its whole structure. Only references can
	/// recurse, and they end the descent, so an inline schema is always finite.
	/// </summary>
	private static string SchemaKey(IOpenApiSchema? schema)
	{
		if (schema is null)
			return "";
		if (schema is OpenApiSchemaReference reference)
			return "$" + (reference.Reference.Id ?? "");

		var parts = new List<string> { schema.Type?.ToString() ?? "", schema.Format ?? "" };
		if (schema.Items is not null)
			parts.Add("items=" + SchemaKey(schema.Items));
		foreach (var (name, property) in (schema.Properties ?? new Dictionary<string, IOpenApiSchema>()).OrderBy(
			static p => p.Key,
			StringComparer.Ordinal
		))
			parts.Add($"{name}={SchemaKey(property)}");
		if (schema.Required is { Count: > 0 })
			parts.Add("required=" + string.Join(',', schema.Required.Order(StringComparer.Ordinal)));
		foreach (var (label, options) in new[] { ("allOf", schema.AllOf), ("oneOf", schema.OneOf), ("anyOf", schema.AnyOf) })
		{
			if (options is { Count: > 0 })
				parts.Add(label + "=" + string.Join(',', options.Select(SchemaKey)));
		}
		if (schema.Enum is { Count: > 0 })
			parts.Add("enum=" + string.Join(',', schema.Enum.Select(static e => e?.ToJsonString() ?? "null")));
		return "{" + string.Join(';', parts) + "}";
	}

	private static IEnumerable<EndpointVariant> ParseBadges(string listing)
	{
		foreach (Match match in PathBadge().Matches(listing))
		{
			var method = match.Groups[1].Value.Trim().ToLowerInvariant();
			var route = match.Groups[2].Value.Trim();
			if (method.Length > 0 && route.Length > 0)
				yield return new EndpointVariant(method, route);
		}
	}

	/// <summary>Segments of the main route that a shorter route leaves out as one contiguous run.</summary>
	private static HashSet<int> OptionalSegmentIndexes(string[] main, IEnumerable<string> routes)
	{
		var optional = new HashSet<int>();
		foreach (var route in routes)
		{
			var other = Split(route);
			if (other.Length >= main.Length)
				continue;

			var prefix = 0;
			while (prefix < other.Length && other[prefix] == main[prefix])
				prefix++;
			var suffix = 0;
			while (suffix < other.Length - prefix && other[^(suffix + 1)] == main[^(suffix + 1)])
				suffix++;
			if (prefix + suffix != other.Length)
				continue;

			for (var i = prefix; i < main.Length - suffix; i++)
				_ = optional.Add(i);
		}

		return optional;
	}

	private static int Rank(string method)
	{
		var index = Array.IndexOf(MethodRank, method.ToLowerInvariant());
		return index < 0 ? MethodRank.Length : index;
	}

	private static string[] Split(string route) => route.Split('/', StringSplitOptions.RemoveEmptyEntries);

	private static bool IsPlaceholder(string segment) => segment.Length > 2 && segment[0] == '{' && segment[^1] == '}';

	[GeneratedRegex("""<span class="operation-verb\s+[^"]*">([^<]*)</span>\s*(?:&nbsp;)?\s*<span class="operation-path">([^<]+)</span>""", RegexOptions.IgnoreCase
		| RegexOptions.CultureInvariant)]
	private static partial Regex PathBadge();

	[GeneratedRegex("""\*\*(?:All methods and paths|Spaces method and path) for this operation:\*\*\s*(?:<div\b[^>]*>.*?</div>\s*)+""", RegexOptions.IgnoreCase
		| RegexOptions.Singleline
		| RegexOptions.CultureInvariant)]
	private static partial Regex PathListing();
}
