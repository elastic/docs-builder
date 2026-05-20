// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.Json.Serialization;

namespace Elastic.Documentation.AppliesTo;

public record ProductApplicability
{
	[JsonPropertyName("ecctl")]
	public AppliesCollection? Ecctl { get; set; }

	[JsonPropertyName("curator")]
	public AppliesCollection? Curator { get; set; }

	[JsonPropertyName("apm-agent-android")]
	public AppliesCollection? ApmAgentAndroid { get; set; }

	[JsonPropertyName("apm-agent-dotnet")]
	public AppliesCollection? ApmAgentDotnet { get; set; }

	[JsonPropertyName("apm-agent-go")]
	public AppliesCollection? ApmAgentGo { get; set; }

	[JsonPropertyName("apm-agent-ios")]
	public AppliesCollection? ApmAgentIos { get; set; }

	[JsonPropertyName("apm-agent-java")]
	public AppliesCollection? ApmAgentJava { get; set; }

	[JsonPropertyName("apm-agent-node")]
	public AppliesCollection? ApmAgentNode { get; set; }

	[JsonPropertyName("apm-agent-php")]
	public AppliesCollection? ApmAgentPhp { get; set; }

	[JsonPropertyName("apm-agent-python")]
	public AppliesCollection? ApmAgentPython { get; set; }

	[JsonPropertyName("apm-agent-ruby")]
	public AppliesCollection? ApmAgentRuby { get; set; }

	[JsonPropertyName("apm-agent-rum-js")]
	public AppliesCollection? ApmAgentRumJs { get; set; }

	[JsonPropertyName("edot-ios")]
	public AppliesCollection? EdotIos { get; set; }

	[JsonPropertyName("edot-android")]
	public AppliesCollection? EdotAndroid { get; set; }

	[JsonPropertyName("edot-dotnet")]
	public AppliesCollection? EdotDotnet { get; set; }

	[JsonPropertyName("edot-java")]
	public AppliesCollection? EdotJava { get; set; }

	[JsonPropertyName("edot-node")]
	public AppliesCollection? EdotNode { get; set; }

	[JsonPropertyName("edot-browser")]
	public AppliesCollection? EdotBrowser { get; set; }

	[JsonPropertyName("edot-php")]
	public AppliesCollection? EdotPhp { get; set; }

	[JsonPropertyName("edot-python")]
	public AppliesCollection? EdotPython { get; set; }

	[JsonPropertyName("edot-cf-aws")]
	public AppliesCollection? EdotCfAws { get; set; }

	[JsonPropertyName("edot-cf-azure")]
	public AppliesCollection? EdotCfAzure { get; set; }

	[JsonPropertyName("edot-cf-gcp")]
	public AppliesCollection? EdotCfGcp { get; set; }

	[JsonPropertyName("edot-collector")]
	public AppliesCollection? EdotCollector { get; set; }

	/// <inheritdoc />
	public override string ToString()
	{
		var sb = new StringBuilder();
		var hasContent = false;

		void AppendProduct(string name, AppliesCollection? value)
		{
			if (value is null)
				return;
			if (hasContent)
				_ = sb.Append(": not null } => ");
			_ = sb.Append(name).Append('=').Append(value);
			hasContent = true;
		}

		AppendProduct("ecctl", Ecctl);
		AppendProduct("curator", Curator);
		AppendProduct("apm-agent-android", ApmAgentAndroid);
		AppendProduct("apm-agent-dotnet", ApmAgentDotnet);
		AppendProduct("apm-agent-go", ApmAgentGo);
		AppendProduct("apm-agent-ios", ApmAgentIos);
		AppendProduct("apm-agent-java", ApmAgentJava);
		AppendProduct("apm-agent-node", ApmAgentNode);
		AppendProduct("apm-agent-php", ApmAgentPhp);
		AppendProduct("apm-agent-python", ApmAgentPython);
		AppendProduct("apm-agent-ruby", ApmAgentRuby);
		AppendProduct("apm-agent-rum-js", ApmAgentRumJs);
		AppendProduct("edot-ios", EdotIos);
		AppendProduct("edot-android", EdotAndroid);
		AppendProduct("edot-dotnet", EdotDotnet);
		AppendProduct("edot-java", EdotJava);
		AppendProduct("edot-node", EdotNode);
		AppendProduct("edot-browser", EdotBrowser);
		AppendProduct("edot-php", EdotPhp);
		AppendProduct("edot-python", EdotPython);
		AppendProduct("edot-cf-aws", EdotCfAws);
		AppendProduct("edot-cf-azure", EdotCfAzure);
		AppendProduct("edot-cf-gcp", EdotCfGcp);
		AppendProduct("edot-collector", EdotCollector);

		return sb.ToString();
	}
}

public static class ProductApplicabilityConversion
{
	public static string? ProductApplicabilityToProductId(ProductApplicability p) => p switch
	{
		{ Ecctl: not null } => "cloud-control-ecctl",
		{ Curator: not null } => "curator",
		{ ApmAgentAndroid: not null } => "edot-android",
		{ ApmAgentDotnet: not null } => "apm-agent-dotnet",
		{ ApmAgentGo: not null } => "apm-agent-go",
		{ ApmAgentIos: not null } => "edot-ios",
		{ ApmAgentJava: not null } => "apm-agent-java",
		{ ApmAgentNode: not null } => "apm-agent-node",
		{ ApmAgentPhp: not null } => "apm-agent-php",
		{ ApmAgentPython: not null } => "apm-agent-python",
		{ ApmAgentRuby: not null } => "apm-agent-ruby",
		{ ApmAgentRumJs: not null } => "apm-agent-rum-js",
		{ EdotIos: not null } => "edot-ios",
		{ EdotAndroid: not null } => "edot-android",
		{ EdotDotnet: not null } => "edot-dotnet",
		{ EdotJava: not null } => "edot-java",
		{ EdotNode: not null } => "edot-node",
		{ EdotBrowser: not null } => "edot-browser",
		{ EdotPhp: not null } => "edot-php",
		{ EdotPython: not null } => "edot-python",
		{ EdotCfAws: not null } => "edot-cf-aws",
		{ EdotCfAzure: not null } => "edot-cf-azure",
		{ EdotCfGcp: not null } => "edot-cf-gcp",
		{ EdotCollector: not null } => "edot-collector",
		_ => null
	};
}
