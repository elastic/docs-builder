// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using AwesomeAssertions;
using Elastic.Documentation.Assembler.ApiCrawl;

namespace Elastic.Documentation.Build.Tests.ApiCrawl;

public class ApiCrawlClientTests
{
	/// <summary>A body that sends its headers and then never another byte, like a CDN that stalls mid-response.</summary>
	private sealed class StalledStream : Stream
	{
		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => throw new NotSupportedException();
		public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

		public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		{
			await Task.Delay(Timeout.Infinite, cancellationToken);
			return 0;
		}

		public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
		public override void Flush() { }
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
	}

	private sealed class StalledHandler : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new StalledStream()) });
	}

	[Test]
	public async Task ObserveAsync_BodyStalls_RecordsAnErrorInsteadOfHanging()
	{
		using var client = new ApiCrawlClient(
			new Uri("https://site.test"),
			new StalledHandler(),
			bodyTimeout: TimeSpan.FromMilliseconds(100)
		);

		var observed = await client.ObserveAsync("/docs/api/elasticsearch", CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));

		observed.Status.Should().Be(0);
		observed.Error.Should().NotBeNull();
	}

	[Test]
	public async Task GetTextAsync_BodyStalls_RecordsAnErrorInsteadOfHanging()
	{
		using var client = new ApiCrawlClient(
			new Uri("https://site.test"),
			new StalledHandler(),
			bodyTimeout: TimeSpan.FromMilliseconds(100)
		);

		var fetched = await client.GetTextAsync(new Uri("https://site.test/docs/sitemap.xml"), CancellationToken.None).WaitAsync(
			TimeSpan.FromSeconds(10)
		);

		fetched.IsOk.Should().BeFalse();
		fetched.Error.Should().NotBeNull();
	}
}
