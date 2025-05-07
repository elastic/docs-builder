// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Elastic.Markdown.Myst.FrontMatter;

public enum Product
{
	[Display(Name = "APM")]
	[EnumMember(Value = "apm")]
	Apm,

	[Display(Name = "APM .NET Agent")]
	[EnumMember(Value = "apm-dotnet-agent")]
	ApmDotnetAgent,

	[Display(Name = "APM Android Agent")]
	[EnumMember(Value = "apm-android-agent")]
	ApmAndroidAgent,

	[Display(Name = "APM Attacher")]
	[EnumMember(Value = "apm-attacher")]
	ApmAttacher,

	[Display(Name = "APM AWS Lambda extension")]
	[EnumMember(Value = "apm-aws-lambda-extension")]
	ApmAwsLambdaExtension,

	[Display(Name = "APM Go Agent")]
	[EnumMember(Value = "apm-go-agent")]
	ApmGoAgent,

	[Display(Name = "APM iOS Agent")]
	[EnumMember(Value = "apm-ios-agent")]
	ApmIosAgent,

	[Display(Name = "APM Java Agent")]
	[EnumMember(Value = "apm-java-agent")]
	ApmJavaAgent,

	[Display(Name = "APM Node.js Agent")]
	[EnumMember(Value = "apm-node-agent")]
	ApmNodeAgent,

	[Display(Name = "APM PHP Agent")]
	[EnumMember(Value = "apm-php-agent")]
	ApmPhpAgent,

	[Display(Name = "APM Python Agent")]
	[EnumMember(Value = "apm-python-agent")]
	ApmPythonAgent,

	[Display(Name = "APM Ruby Agent")]
	[EnumMember(Value = "apm-ruby-agent")]
	ApmRubyAgent,

	[Display(Name = "APM RUM Agent")]
	[EnumMember(Value = "apm-rum-agent")]
	ApmRumAgent,

	[Display(Name = "Beats Logging plugin")]
	[EnumMember(Value = "beats-logging-plugin")]
	BeatsLoggingPlugin,

	[Display(Name = "Cloud Control ECCTL")]
	[EnumMember(Value = "cloud-control-ecctl")]
	CloudControlEcctl,

	[Display(Name = "Cloud Enterprise")]
	[EnumMember(Value = "cloud-enterprise")]
	CloudEnterprise,

	[Display(Name = "Cloud Hosted")]
	[EnumMember(Value = "cloud-hosted")]
	CloudHosted,

	[Display(Name = "Cloud Kubernetes")]
	[EnumMember(Value = "cloud-kubernetes")]
	CloudKubernetes,

	[Display(Name = "Cloud Native Ingest")]
	[EnumMember(Value = "cloud-native-ingest")]
	CloudNativeIngest,

	[Display(Name = "Cloud Serverless")]
	[EnumMember(Value = "cloud-serverless")]
	CloudServerless,

	[Display(Name = "Cloud Terraform")]
	[EnumMember(Value = "cloud-terraform")]
	CloudTerraform,

	[Display(Name = "ECS Logging")]
	[EnumMember(Value = "ecs-logging")]
	EcsLogging,

	[Display(Name = "ECS Logging .NET")]
	[EnumMember(Value = "ecs-logging-dotnet")]
	EcsLoggingDotnet,

	[Display(Name = "ECS Logging Go Logrus")]
	[EnumMember(Value = "ecs-logging-go-logrus")]
	EcsLoggingGoLogrus,

	[Display(Name = "ECS Logging Go Zap")]
	[EnumMember(Value = "ecs-logging-go-zap")]
	EcsLoggingGoZap,

	[Display(Name = "ECS Logging Go Zerolog")]
	[EnumMember(Value = "ecs-logging-go-zerolog")]
	EcsLoggingGoZerolog,

	[Display(Name = "ECS Logging Java")]
	[EnumMember(Value = "ecs-logging-java")]
	EcsLoggingJava,

	[Display(Name = "ECS Logging Node.js")]
	[EnumMember(Value = "ecs-logging-node")]
	EcsLoggingNode,

	[Display(Name = "ECS Logging PHP")]
	[EnumMember(Value = "ecs-logging-php")]
	EcsLoggingPhp,

	[Display(Name = "ECS Logging Python")]
	[EnumMember(Value = "ecs-logging-python")]
	EcsLoggingPython,

	[Display(Name = "ECS Logging Ruby")]
	[EnumMember(Value = "ecs-logging-ruby")]
	EcsLoggingRuby,

	[Display(Name = "Elastic Agent")]
	[EnumMember(Value = "elastic-agent")]
	ElasticAgent,

	[Display(Name = "Elastic Common Schema (ECS)")]
	[EnumMember(Value = "ecs")]
	Ecs,

	[Display(Name = "Elastic Products platform")]
	[EnumMember(Value = "elastic-products-platform")]
	ElasticProductsPlatform,

	[Display(Name = "Elastic Stack")]
	[EnumMember(Value = "elastic-stack")]
	ElasticStack,

	[Display(Name = "Elasticsearch")]
	[EnumMember(Value = "elasticsearch")]
	Elasticsearch,

	[Display(Name = "Elasticsearch .NET Client")]
	[EnumMember(Value = "elasticsearch-dotnet-client")]
	ElasticsearchDotnetClient,

	[Display(Name = "Elasticsearch Apache Hadoop")]
	[EnumMember(Value = "elasticsearch-apache-hadoop")]
	ElasticsearchApacheHadoop,

	[Display(Name = "Elasticsearch Cloud Hosted Heroku")]
	[EnumMember(Value = "elasticsearch-cloud-hosted-heroku")]
	ElasticsearchCloudHostedHeroku,

	[Display(Name = "Elasticsearch community clients")]
	[EnumMember(Value = "elasticsearch-community-clients")]
	ElasticsearchCommunityClients,

	[Display(Name = "Elasticsearch Curator")]
	[EnumMember(Value = "elasticsearch-curator")]
	ElasticsearchCurator,

	[Display(Name = "Elasticsearch Eland Python Client")]
	[EnumMember(Value = "elasticsearch-eland-python-client")]
	ElasticsearchElandPythonClient,

	[Display(Name = "Elasticsearch Go Client")]
	[EnumMember(Value = "elasticsearch-go-client")]
	ElasticsearchGoClient,

	[Display(Name = "Elasticsearch Groovy Client")]
	[EnumMember(Value = "elasticsearch-groovy-client")]
	ElasticsearchGroovyClient,

	[Display(Name = "Elasticsearch Java Client")]
	[EnumMember(Value = "elasticsearch-java-client")]
	ElasticsearchJavaClient,

	[Display(Name = "Elasticsearch JavaScript Client")]
	[EnumMember(Value = "elasticsearch-java-script-client")]
	ElasticsearchJavaScriptClient,

	[Display(Name = "Elasticsearch Painless scripting language")]
	[EnumMember(Value = "elasticsearch-painless-scripting-language")]
	ElasticsearchPainlessScriptingLanguage,

	[Display(Name = "Elasticsearch Perl Client")]
	[EnumMember(Value = "elasticsearch-perl-client")]
	ElasticsearchPerlClient,

	[Display(Name = "Elasticsearch PHP Client")]
	[EnumMember(Value = "elasticsearch-php-client")]
	ElasticsearchPhpClient,

	[Display(Name = "Elasticsearch plugins")]
	[EnumMember(Value = "elasticsearch-plugins")]
	ElasticsearchPlugins,

	[Display(Name = "Elasticsearch Python Client")]
	[EnumMember(Value = "elasticsearch-python-client")]
	ElasticsearchPythonClient,

	[Display(Name = "Elasticsearch Resiliency Status")]
	[EnumMember(Value = "elasticsearch-resiliency-status")]
	ElasticsearchResiliencyStatus,

	[Display(Name = "Elasticsearch Ruby Client")]
	[EnumMember(Value = "elasticsearch-ruby-client")]
	ElasticsearchRubyClient,

	[Display(Name = "Elasticsearch Rust Client")]
	[EnumMember(Value = "elasticsearch-rust-client")]
	ElasticsearchRustClient,

	[Display(Name = "Fleet")]
	[EnumMember(Value = "fleet")]
	Fleet,

	[Display(Name = "Ingest")]
	[EnumMember(Value = "ingest")]
	Ingest,

	[Display(Name = "Integrations")]
	[EnumMember(Value = "integrations")]
	Integrations,

	[Display(Name = "Kibana")]
	[EnumMember(Value = "kibana")]
	Kibana,

	[Display(Name = "Logstash")]
	[EnumMember(Value = "logstash")]
	Logstash,

	[Display(Name = "Machine Learning")]
	[EnumMember(Value = "machine-learning")]
	MachineLearning,

	[Display(Name = "Observability")]
	[EnumMember(Value = "observability")]
	Observability,

	[Display(Name = "Reference Architectures")]
	[EnumMember(Value = "reference-architectures")]
	ReferenceArchitectures,

	[Display(Name = "Search UI")]
	[EnumMember(Value = "search-ui")]
	SearchUi,

	[Display(Name = "Security")]
	[EnumMember(Value = "security")]
	Security,

	[Display(Name = "Elastic Distribution of OpenTelemetry Collector")]
	[EnumMember(Value = "edot-collector")]
	EdotCollector,

	[Display(Name = "Elastic Distribution of OpenTelemetry Java")]
	[EnumMember(Value = "edot-java")]
	EdotJava,

	[Display(Name = "Elastic Distribution of OpenTelemetry .NET")]
	[EnumMember(Value = "edot-dotnet")]
	EdotDotnet,

	[Display(Name = "Elastic Distribution of OpenTelemetry Node.js")]
	[EnumMember(Value = "edot-nodejs")]
	EdotNodeJs,

	[Display(Name = "Elastic Distribution of OpenTelemetry PHP")]
	[EnumMember(Value = "edot-php")]
	EdotPhp,

	[Display(Name = "Elastic Distribution of OpenTelemetry Python")]
	[EnumMember(Value = "edot-python")]
	EdotPython,

	[Display(Name = "Elastic Distribution of OpenTelemetry Android")]
	[EnumMember(Value = "edot-android")]
	EdotAndroid,

	[Display(Name = "Elastic Distribution of OpenTelemetry iOS")]
	[EnumMember(Value = "edot-ios")]
	EdotIos,
}

public class ProductConverter : IYamlTypeConverter
{
	public bool Accepts(Type type) => type == typeof(Product);

	public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		var value = parser.Consume<Scalar>();
		if (string.IsNullOrWhiteSpace(value.Value))
			throw new InvalidProductException("empty value");

		var product = Enum.GetValues<Product>()
			.FirstOrDefault(p =>
			{
				var enumMemberAttr = typeof(Product)
					.GetField(p.ToString())
					?.GetCustomAttributes(typeof(EnumMemberAttribute), false)
					.FirstOrDefault() as EnumMemberAttribute;
				return enumMemberAttr?.Value?.Equals(value.Value, StringComparison.OrdinalIgnoreCase) ?? false;
			});

		if (product != default)
			return product;

		throw new InvalidProductException(value.Value);
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
	{
		if (value == null)
			return;
		var product = (Product)value;
		var enumMemberAttr = typeof(Product)
			.GetField(product.ToString())
			?.GetCustomAttributes(typeof(EnumMemberAttribute), false)
			.FirstOrDefault() as EnumMemberAttribute;

		emitter.Emit(new Scalar(enumMemberAttr?.Value ?? product.ToString().ToLowerInvariant()));
	}
}

public class InvalidProductException(string invalidValue)
	: Exception(
		$"Invalid products frontmatter value: \"{invalidValue}\". Did you mean \"{ProductExtensions.Suggestion(invalidValue)}\"?\nYou can find the full list at https://docs-v3-preview.elastic.dev/elastic/docs-builder/tree/main/syntax/frontmatter#products.");

public static class ProductExtensions
{
	public static string GetProductDisplayName(this Product product)
	{
		var displayAttr = typeof(Product)
			.GetField(product.ToString())
			?.GetCustomAttributes(typeof(DisplayAttribute), false)
			.FirstOrDefault() as DisplayAttribute;
		return displayAttr?.Name ?? product.ToString();
	}


	private static List<string> GetEnumMemberValues() => Enum.GetValues<Product>()
		.Select(p =>
		{
			var enumMemberAttr = typeof(Product)
				.GetField(p.ToString())
				?.GetCustomAttributes(typeof(EnumMemberAttribute), false)
				.FirstOrDefault() as EnumMemberAttribute;

			return enumMemberAttr?.Value;
		})
		.Where(value => value != null)
		.Select(value => value!)
		.ToList();

	public static string Suggestion(string input) =>
		GetEnumMemberValues()
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
