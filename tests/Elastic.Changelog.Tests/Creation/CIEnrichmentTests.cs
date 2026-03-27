// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Creation;
using Elastic.Changelog.Tests.Changelogs;
using Elastic.Documentation.Configuration;
using FakeItEasy;

namespace Elastic.Changelog.Tests.Creation;

public class CIEnrichmentTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	private static CreateChangelogArguments DefaultInput() =>
		new() { Products = [] };

	private static IEnvironmentVariables FakeCIEnv(
		string? prNumber = null,
		string? title = null,
		string? type = null,
		string? owner = null,
		string? repo = null,
		string? products = null)
	{
		var env = A.Fake<IEnvironmentVariables>();
		A.CallTo(() => env.IsRunningOnCI).Returns(true);
		A.CallTo(() => env.GetEnvironmentVariable("CHANGELOG_PR_NUMBER")).Returns(prNumber);
		A.CallTo(() => env.GetEnvironmentVariable("CHANGELOG_TITLE")).Returns(title);
		A.CallTo(() => env.GetEnvironmentVariable("CHANGELOG_TYPE")).Returns(type);
		A.CallTo(() => env.GetEnvironmentVariable("CHANGELOG_OWNER")).Returns(owner);
		A.CallTo(() => env.GetEnvironmentVariable("CHANGELOG_REPO")).Returns(repo);
		A.CallTo(() => env.GetEnvironmentVariable("CHANGELOG_PRODUCTS")).Returns(products);
		return env;
	}

	private static IEnvironmentVariables FakeLocalEnv()
	{
		var env = A.Fake<IEnvironmentVariables>();
		A.CallTo(() => env.IsRunningOnCI).Returns(false);
		return env;
	}

	private ChangelogCreationService CreateServiceWithEnv(IEnvironmentVariables env) =>
		new(LoggerFactory, ConfigurationContext, env: env);

	[Fact]
	public void EnrichFromCI_NotInCI_ReturnsUnchanged()
	{
		var service = CreateServiceWithEnv(FakeLocalEnv());
		var input = DefaultInput() with { Title = "original" };

		var result = service.EnrichFromCI(input);

		result.Should().BeSameAs(input);
	}

	[Fact]
	public void EnrichFromCI_InCI_NoEnvVars_ReturnsUnchanged()
	{
		var service = CreateServiceWithEnv(FakeCIEnv());
		var input = DefaultInput();

		var result = service.EnrichFromCI(input);

		result.Should().BeSameAs(input);
	}

	[Fact]
	public void EnrichFromCI_InCI_AllEnvVars_FillsMissingFields()
	{
		var env = FakeCIEnv(prNumber: "42", title: "Fix bug", type: "bug-fix", owner: "elastic", repo: "kibana");
		var service = CreateServiceWithEnv(env);
		var input = DefaultInput();

		var result = service.EnrichFromCI(input);

		result.Prs.Should().BeEquivalentTo(["42"]);
		result.Title.Should().Be("Fix bug");
		result.Type.Should().Be("bug-fix");
		result.Owner.Should().Be("elastic");
		result.Repo.Should().Be("kibana");
	}

	[Fact]
	public void EnrichFromCI_InCI_ExplicitPrs_CLIWins()
	{
		var env = FakeCIEnv(prNumber: "42", title: "CI title", type: "bug-fix", owner: "elastic", repo: "kibana");
		var service = CreateServiceWithEnv(env);
		var input = DefaultInput() with { Prs = ["99"] };

		var result = service.EnrichFromCI(input);

		result.Prs.Should().BeEquivalentTo(["99"]);
	}

	[Fact]
	public void EnrichFromCI_InCI_ExplicitTitle_CLIWins()
	{
		var env = FakeCIEnv(prNumber: "42", title: "CI title", type: "bug-fix");
		var service = CreateServiceWithEnv(env);
		var input = DefaultInput() with { Title = "My explicit title" };

		var result = service.EnrichFromCI(input);

		result.Title.Should().Be("My explicit title");
		result.Type.Should().Be("bug-fix");
	}

	[Fact]
	public void EnrichFromCI_InCI_ExplicitType_CLIWins()
	{
		var env = FakeCIEnv(prNumber: "42", type: "bug-fix");
		var service = CreateServiceWithEnv(env);
		var input = DefaultInput() with { Type = "enhancement" };

		var result = service.EnrichFromCI(input);

		result.Type.Should().Be("enhancement");
	}

	[Fact]
	public void EnrichFromCI_InCI_ExplicitOwnerRepo_CLIWins()
	{
		var env = FakeCIEnv(prNumber: "42", owner: "ci-owner", repo: "ci-repo");
		var service = CreateServiceWithEnv(env);
		var input = DefaultInput() with { Owner = "my-owner", Repo = "my-repo" };

		var result = service.EnrichFromCI(input);

		result.Owner.Should().Be("my-owner");
		result.Repo.Should().Be("my-repo");
	}

	[Fact]
	public void EnrichFromCI_InCI_PartialEnvVars_OnlyFillsAvailable()
	{
		var env = FakeCIEnv(prNumber: "42");
		var service = CreateServiceWithEnv(env);
		var input = DefaultInput();

		var result = service.EnrichFromCI(input);

		result.Prs.Should().BeEquivalentTo(["42"]);
		result.Title.Should().BeNull();
		result.Type.Should().BeNull();
		result.Owner.Should().BeNull();
		result.Repo.Should().BeNull();
	}

	[Fact]
	public void EnrichFromCI_InCI_TitleOnly_EnrichesWithoutPr()
	{
		var env = FakeCIEnv(title: "CI title");
		var service = CreateServiceWithEnv(env);
		var input = DefaultInput();

		var result = service.EnrichFromCI(input);

		result.Title.Should().Be("CI title");
		result.Prs.Should().BeNull();
	}

	[Fact]
	public void EnrichFromCI_NullEnv_ReturnsUnchanged()
	{
		var service = new ChangelogCreationService(LoggerFactory, ConfigurationContext);
		var input = DefaultInput() with { Title = "original" };

		var result = service.EnrichFromCI(input);

		result.Should().BeSameAs(input);
	}

	[Fact]
	public void EnrichFromCI_InCI_Products_FillsProducts()
	{
		var env = FakeCIEnv(prNumber: "42", title: "Fix", type: "bug-fix", products: "cloud-hosted, cloud-serverless");
		var service = CreateServiceWithEnv(env);
		var input = DefaultInput();

		var result = service.EnrichFromCI(input);

		result.Products.Should().HaveCount(2);
		result.Products[0].Product.Should().Be("cloud-hosted");
		result.Products[1].Product.Should().Be("cloud-serverless");
	}

	[Fact]
	public void EnrichFromCI_InCI_ProductsWithTargetAndLifecycle_ParsesCorrectly()
	{
		var env = FakeCIEnv(prNumber: "42", title: "Fix", type: "bug-fix", products: "elasticsearch 9.2.0 ga, cloud-serverless 2025-06");
		var service = CreateServiceWithEnv(env);
		var input = DefaultInput();

		var result = service.EnrichFromCI(input);

		result.Products.Should().HaveCount(2);
		result.Products[0].Product.Should().Be("elasticsearch");
		result.Products[0].Target.Should().Be("9.2.0");
		result.Products[0].Lifecycle.Should().Be("ga");
		result.Products[1].Product.Should().Be("cloud-serverless");
		result.Products[1].Target.Should().Be("2025-06");
		result.Products[1].Lifecycle.Should().BeNull();
	}

	[Fact]
	public void EnrichFromCI_InCI_ExplicitProducts_CLIWins()
	{
		var env = FakeCIEnv(prNumber: "42", title: "Fix", type: "bug-fix", products: "cloud-hosted, cloud-serverless");
		var service = CreateServiceWithEnv(env);
		var input = DefaultInput() with
		{
			Products = [new ProductArgument { Product = "elasticsearch" }]
		};

		var result = service.EnrichFromCI(input);

		result.Products.Should().HaveCount(1);
		result.Products[0].Product.Should().Be("elasticsearch");
	}

	[Fact]
	public void EnrichFromCI_InCI_NoProducts_RemainsEmpty()
	{
		var env = FakeCIEnv(prNumber: "42", title: "Fix", type: "bug-fix");
		var service = CreateServiceWithEnv(env);
		var input = DefaultInput();

		var result = service.EnrichFromCI(input);

		result.Products.Should().BeEmpty();
	}
}
