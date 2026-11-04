// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Api.Tests.Fixtures;

/// <summary>
/// A service whose implementation each test chooses, so one <see cref="ApiWebApplicationFactory"/> can serve all the tests of a class.
/// Starting a host costs about 0.75 s, which was nearly all the time these tests took.
/// The tests must not run in parallel with each other, because they share the current implementation:
/// mark the class with <c>[NotInParallel(...)]</c>.
/// </summary>
/// <typeparam name="TService">The service the host resolves.</typeparam>
public sealed class SwappableService<TService> where TService : class
{
	private volatile TService? _current;

	public TService Current
	{
		get => _current ?? throw new InvalidOperationException($"No {typeof(TService).Name} was installed for this test.");
		set => _current = value;
	}
}
