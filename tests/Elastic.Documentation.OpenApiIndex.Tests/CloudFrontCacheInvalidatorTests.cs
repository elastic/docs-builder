// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using AwesomeAssertions;
using FakeItEasy;

namespace Elastic.Documentation.OpenApiIndex.Tests;

public class CloudFrontCacheInvalidatorTests
{
	private const string DistributionId = "E1234567890ABC";

	private readonly IAmazonCloudFront _cloudFrontClient = A.Fake<IAmazonCloudFront>();
	private readonly List<CreateInvalidationRequest> _requests = [];

	public CloudFrontCacheInvalidatorTests() =>
		A
			.CallTo(() => _cloudFrontClient.CreateInvalidationAsync(A<CreateInvalidationRequest>._, A<Cancel>._))
			.Invokes((CreateInvalidationRequest request, Cancel _) => _requests.Add(request))
			.Returns(new CreateInvalidationResponse());

	private CloudFrontCacheInvalidator CreateInvalidator() => new(_cloudFrontClient, DistributionId);

	[Fact]
	public async Task InvalidateAsync_SendsExpectedPathsAndCallerReference()
	{
		var invalidator = CreateInvalidator();
		var paths = new[] { "/index.json", "/elastic/elasticsearch/8.16/openapi.json" };

		await invalidator.InvalidateAsync(paths, "request-id-1", TestContext.Current.CancellationToken);

		var request = _requests.Should().ContainSingle().Subject;
		request.DistributionId.Should().Be(DistributionId);
		request.InvalidationBatch.CallerReference.Should().Be("request-id-1");
		request.InvalidationBatch.Paths.Quantity.Should().Be(2);
		request.InvalidationBatch.Paths.Items.Should().BeEquivalentTo(paths);
	}

	[Fact]
	public async Task InvalidateAsync_EmptyPaths_DoesNotCallCloudFront()
	{
		var invalidator = CreateInvalidator();

		await invalidator.InvalidateAsync([], "request-id-1", TestContext.Current.CancellationToken);

		A.CallTo(() => _cloudFrontClient.CreateInvalidationAsync(A<CreateInvalidationRequest>._, A<Cancel>._)).MustNotHaveHappened();
	}

	[Fact]
	public async Task InvalidateAsync_CloudFrontFailure_PropagatesException()
	{
		A.CallTo(() => _cloudFrontClient.CreateInvalidationAsync(A<CreateInvalidationRequest>._, A<Cancel>._)).Throws(
			new AmazonCloudFrontException("Access denied")
		);

		var act = () => CreateInvalidator().InvalidateAsync(["/index.json"], "request-id-1", TestContext.Current.CancellationToken);

		await act.Should().ThrowAsync<AmazonCloudFrontException>();
	}
}
