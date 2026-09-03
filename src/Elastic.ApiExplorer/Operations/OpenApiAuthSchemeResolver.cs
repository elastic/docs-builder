// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

public record AuthSchemeBadge(string Label);

public static class OpenApiAuthSchemeResolver
{
	public static IReadOnlyList<AuthSchemeBadge> Resolve(OpenApiOperation operation, OpenApiDocument document)
	{
		// Omitted operation security is null (inherit). An empty list is an explicit override to none.
		var requirements = operation.Security ?? document.Security;
		if (requirements is not { Count: > 0 })
			return [];

		var seen = new HashSet<string>(StringComparer.Ordinal);
		var badges = new List<AuthSchemeBadge>();
		foreach (var requirement in requirements)
		{
			foreach (var scheme in requirement)
			{
				var label = LabelFor(Target(scheme.Key, document));
				if (label is null || !seen.Add(label))
					continue;
				badges.Add(new AuthSchemeBadge(label));
			}
		}

		return badges;
	}

	private static IOpenApiSecurityScheme? Target(IOpenApiSecurityScheme scheme, OpenApiDocument document)
	{
		if (
			scheme is OpenApiSecuritySchemeReference { Reference.Id: { Length: > 0 } id }
			&& document.Components?.SecuritySchemes?.TryGetValue(id, out var listed) == true
		)
			return listed;
		return scheme;
	}

	private static string? LabelFor(IOpenApiSecurityScheme? scheme) => scheme?.Type switch
	{
		SecuritySchemeType.ApiKey => "Api key",
		SecuritySchemeType.Http when string.Equals(scheme.Scheme, "basic", StringComparison.OrdinalIgnoreCase) => "Basic",
		SecuritySchemeType.Http when string.Equals(scheme.Scheme, "bearer", StringComparison.OrdinalIgnoreCase) => "Bearer",
		_ => null
	};
}
