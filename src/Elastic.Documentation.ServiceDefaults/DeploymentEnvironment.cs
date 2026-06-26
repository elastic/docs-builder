// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Hosting;

namespace Elastic.Documentation.ServiceDefaults;

/// <summary>
/// Maps our deployment environment names to the .NET hosting environment names.
/// </summary>
/// <remarks>
/// Our infra injects ENVIRONMENT as one of: dev, edge, staging, prod.
/// .NET uses: Development, Staging, Production.
/// edge maps to Staging so that IsDevelopment/IsProduction/IsStaging work correctly
/// while the raw ENVIRONMENT value is preserved for domain concerns (ES indices, telemetry).
/// </remarks>
public static class DeploymentEnvironment
{
	/// <summary>
	/// Maps a deployment ENVIRONMENT value (dev/edge/staging/prod) to the corresponding
	/// .NET hosting environment name so <see cref="IHostEnvironment.IsDevelopment"/>,
	/// <see cref="IHostEnvironment.IsStaging"/> and <see cref="IHostEnvironment.IsProduction"/> work.
	/// Returns <c>null</c> when the value is absent or unrecognized — callers should skip
	/// setting <see cref="IHostEnvironment.EnvironmentName"/> in that case.
	/// </summary>
	public static string? ToDotnetEnvironment(string? environment) =>
		environment?.Trim().ToLowerInvariant() switch
		{
			"prod" => Environments.Production,
			"staging" or "edge" => Environments.Staging,
			"dev" => Environments.Development,
			_ => null
		};
}
