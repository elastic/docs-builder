// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using EnumFastToStringGenerated;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Elastic.Markdown.Myst.FrontMatter;

[EnumGenerator]
public enum Product
{
	[Display(Name = "apm", Description = "APM")]
	Apm,

	[Display(Name = "apm-dotnet-agent", Description = "APM .NET Agent")]
	ApmDotnetAgent,

	[Display(Name = "apm-android-agent", Description = "APM Android Agent")]
	ApmAndroidAgent,

	[Display(Name = "apm-attacher", Description = "APM Attacher")]
	ApmAttacher,

	[Display(Name = "apm-aws-lambda-extension", Description = "APM AWS Lambda extension")]
	ApmAwsLambdaExtension,

	[Display(Name = "apm-go-agent", Description = "APM Go Agent")]
	ApmGoAgent,

	[Display(Name = "apm-ios-agent", Description = "APM iOS Agent")]
	ApmIosAgent,

	[Display(Name = "apm-java-agent", Description = "APM Java Agent")]
	ApmJavaAgent,

	[Display(Name = "apm-node-agent", Description = "APM Node.js Agent")]
	ApmNodeAgent,

	[Display(Name = "apm-php-agent", Description = "APM PHP Agent")]
	ApmPhpAgent,

	[Display(Name = "apm-python-agent", Description = "APM Python Agent")]
	ApmPythonAgent,

	[Display(Name = "apm-ruby-agent", Description = "APM Ruby Agent")]
	ApmRubyAgent,

	[Display(Name = "apm-rum-agent", Description = "APM RUM Agent")]
	ApmRumAgent,

	[Display(Name = "beats-logging-plugin", Description = "Beats Logging plugin")]
	BeatsLoggingPlugin,

	[Display(Name = "cloud-control-ecctl", Description = "Cloud Control ECCTL")]
	CloudControlEcctl,

	[Display(Name = "cloud-enterprise", Description = "Cloud Enterprise")]
	CloudEnterprise,

	[Display(Name = "cloud-hosted", Description = "Cloud Hosted")]
	CloudHosted,

	[Display(Name = "cloud-kubernetes", Description = "Cloud Kubernetes")]
	CloudKubernetes,

	[Display(Name = "cloud-native-ingest", Description = "Cloud Native Ingest")]
	CloudNativeIngest,

	[Display(Name = "cloud-serverless", Description = "Cloud Serverless")]
	CloudServerless,

	[Display(Name = "cloud-terraform", Description = "Cloud Terraform")]
	CloudTerraform,

	[Display(Name = "ecs-logging", Description = "ECS Logging")]
	EcsLogging,

	[Display(Name = "ecs-logging-dotnet", Description = "ECS Logging .NET")]
	EcsLoggingDotnet,

	[Display(Name = "ecs-logging-go-logrus", Description = "ECS Logging Go Logrus")]
	EcsLoggingGoLogrus,

	[Display(Name = "ecs-logging-go-zap", Description = "ECS Logging Go Zap")]
	EcsLoggingGoZap,

	[Display(Name = "ecs-logging-go-zerolog", Description = "ECS Logging Go Zerolog")]
	EcsLoggingGoZerolog,

	[Display(Name = "ecs-logging-java", Description = "ECS Logging Java")]
	EcsLoggingJava,

	[Display(Name = "ecs-logging-node", Description = "ECS Logging Node.js")]
	EcsLoggingNode,

	[Display(Name = "ecs-logging-php", Description = "ECS Logging PHP")]
	EcsLoggingPhp,

	[Display(Name = "ecs-logging-python", Description = "ECS Logging Python")]
	EcsLoggingPython,

	[Display(Name = "ecs-logging-ruby", Description = "ECS Logging Ruby")]
	EcsLoggingRuby,

	[Display(Name = "elastic-agent", Description = "Elastic Agent")]
	ElasticAgent,

	[Display(Name = "ecs", Description = "Elastic Common Schema (ECS)")]
	Ecs,

	[Display(Name = "elastic-products-platform", Description = "Elastic Products platform")]
	ElasticProductsPlatform,

	[Display(Name = "elastic-stack", Description = "Elastic Stack")]
	ElasticStack,

	[Display(Name = "elasticsearch", Description = "Elasticsearch")]
	Elasticsearch,

	[Display(Name = "elasticsearch-dotnet-client", Description = "Elasticsearch .NET Client")]
	ElasticsearchDotnetClient,

	[Display(Name = "elasticsearch-apache-hadoop", Description = "Elasticsearch Apache Hadoop")]
	ElasticsearchApacheHadoop,

	[Display(Name = "elasticsearch-cloud-hosted-heroku", Description = "Elasticsearch Cloud Hosted Heroku")]
	ElasticsearchCloudHostedHeroku,

	[Display(Name = "elasticsearch-community-clients", Description = "Elasticsearch community clients")]
	ElasticsearchCommunityClients,

	[Display(Name = "elasticsearch-curator", Description = "Elasticsearch Curator")]
	ElasticsearchCurator,

	[Display(Name = "elasticsearch-eland-python-client", Description = "Elasticsearch Eland Python Client")]
	ElasticsearchElandPythonClient,

	[Display(Name = "elasticsearch-go-client", Description = "Elasticsearch Go Client")]
	ElasticsearchGoClient,

	[Display(Name = "elasticsearch-groovy-client", Description = "Elasticsearch Groovy Client")]
	ElasticsearchGroovyClient,

	[Display(Name = "elasticsearch-java-client", Description = "Elasticsearch Java Client")]
	ElasticsearchJavaClient,

	[Display(Name = "elasticsearch-java-script-client", Description = "Elasticsearch JavaScript Client")]
	ElasticsearchJavaScriptClient,

	[Display(Name = "elasticsearch-painless-scripting-language", Description = "Elasticsearch Painless scripting language")]
	ElasticsearchPainlessScriptingLanguage,

	[Display(Name = "elasticsearch-perl-client", Description = "Elasticsearch Perl Client")]
	ElasticsearchPerlClient,

	[Display(Name = "elasticsearch-php-client", Description = "Elasticsearch PHP Client")]
	ElasticsearchPhpClient,

	[Display(Name = "elasticsearch-plugins", Description = "Elasticsearch plugins")]
	ElasticsearchPlugins,

	[Display(Name = "elasticsearch-python-client", Description = "Elasticsearch Python Client")]
	ElasticsearchPythonClient,

	[Display(Name = "elasticsearch-resiliency-status", Description = "Elasticsearch Resiliency Status")]
	ElasticsearchResiliencyStatus,

	[Display(Name = "elasticsearch-ruby-client", Description = "Elasticsearch Ruby Client")]
	ElasticsearchRubyClient,

	[Display(Name = "elasticsearch-rust-client", Description = "Elasticsearch Rust Client")]
	ElasticsearchRustClient,

	[Display(Name = "fleet", Description = "Fleet")]
	Fleet,

	[Display(Name = "ingest", Description = "Ingest")]
	Ingest,

	[Display(Name = "integrations", Description = "Integrations")]
	Integrations,

	[Display(Name = "kibana", Description = "Kibana")]
	Kibana,

	[Display(Name = "logstash", Description = "Logstash")]
	Logstash,

	[Display(Name = "machine-learning", Description = "Machine Learning")]
	MachineLearning,

	[Display(Name = "observability", Description = "Observability")]
	Observability,

	[Display(Name = "reference-architectures", Description = "Reference Architectures")]
	ReferenceArchitectures,

	[Display(Name = "search-ui", Description = "Search UI")]
	SearchUi,

	[Display(Name = "security", Description = "Security")]
	Security,

	[Display(Name = "edot-collector", Description = "Elastic Distribution of OpenTelemetry Collector")]
	EdotCollector,

	[Display(Name = "edot-java", Description = "Elastic Distribution of OpenTelemetry Java")]
	EdotJava,

	[Display(Name = "edot-dotnet", Description = "Elastic Distribution of OpenTelemetry .NET")]
	EdotDotnet,

	[Display(Name = "edot-nodejs", Description = "Elastic Distribution of OpenTelemetry Node.js")]
	EdotNodeJs,

	[Display(Name = "edot-php", Description = "Elastic Distribution of OpenTelemetry PHP")]
	EdotPhp,

	[Display(Name = "edot-python", Description = "Elastic Distribution of OpenTelemetry Python")]
	EdotPython,

	[Display(Name = "edot-android", Description = "Elastic Distribution of OpenTelemetry Android")]
	EdotAndroid,

	[Display(Name = "edot-ios", Description = "Elastic Distribution of OpenTelemetry iOS")]
	EdotIos,
}

public class ProductConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(Product);

	public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		var value = parser.Consume<Scalar>();
		if (string.IsNullOrWhiteSpace(value.Value))
			throw new InvalidProductException("");

		var product = Enum.GetValues<Product>()
			.FirstOrDefault(p => p.ToDisplayFast()?.Equals(value.Value, StringComparison.Ordinal) ?? false);

		if (ProductEnumExtensions.IsDefinedFast(product) && product.ToDisplayFast()?.Equals(value.Value) == true)
			return product;

		throw new InvalidProductException(value.Value);
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) => serializer.Invoke(value, type);
}

public class InvalidProductException(string invalidValue)
	: Exception(
		$"Invalid products frontmatter value: \"{invalidValue}\"." +
		(!string.IsNullOrWhiteSpace(invalidValue) ? $" Did you mean \"{ProductExtensions.Suggestion(invalidValue)}\"?" : "") +
		"\nYou can find the full list at https://docs-v3-preview.elastic.dev/elastic/docs-builder/tree/main/syntax/frontmatter#products.");

public static class ProductExtensions
{
	private static IReadOnlyCollection<string> GetProductIds() =>
		ProductEnumExtensions.GetValuesFast()
			.Select(p => p.ToDisplayFast()).ToList();

	public static string Suggestion(string input) =>
		GetProductIds()
			.OrderBy(p => LevenshteinDistance(input, p))
			.First();

	// Based on https://rosettacode.org/wiki/Levenshtein_distance#C#
	private static int LevenshteinDistance(string input, string product)
	{
		if (string.IsNullOrEmpty(product))
			return int.MaxValue;

		var inputLength = input.Length;
		var productLength = product.Length;

		if (inputLength == 0)
			return productLength;

		if (productLength == 0)
			return inputLength;

		var distance = new int[inputLength + 1, productLength + 1];

		for (var i = 0; i <= inputLength; i++)
			distance[i, 0] = i;

		for (var j = 0; j <= productLength; j++)
			distance[0, j] = j;

		for (var i = 1; i <= inputLength; i++)
		{
			for (var j = 1; j <= productLength; j++)
			{
				var cost = (input[i - 1] == product[j - 1]) ? 0 : 1;

				distance[i, j] = Math.Min(
					Math.Min(
						distance[i - 1, j] + 1,
						distance[i, j - 1] + 1),
					distance[i - 1, j - 1] + cost);
			}
		}

		return distance[inputLength, productLength];
	}
}
