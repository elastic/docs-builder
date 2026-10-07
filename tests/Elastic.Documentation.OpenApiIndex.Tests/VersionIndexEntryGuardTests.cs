// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using System.Runtime.CompilerServices;
using AwesomeAssertions;

namespace Elastic.Documentation.OpenApiIndex.Tests;

/// <summary>
/// The compile-time guard for the one aggregate this assembly owns (see <c>Constraints-Conventions.md</c>): <see cref="VersionIndexEntry"/> has
/// <c>required</c> public members and <c>internal init</c> for the rest, so only this project (the loader and the builder) and the tests make one. The setter is internal, and System.Text.Json
/// skips a setter it cannot reach, so the second test proves the entry is still read.
/// </summary>
public class VersionIndexEntryGuardTests
{
	[Test]
	public void VersionIndexEntry_HasOnlyRequiredPublicInitAndNoParameterizedConstructor()
	{
		var type = typeof(VersionIndexEntry);

		type
			.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
			.Where(p => p.SetMethod is { IsPublic: true } && p.GetCustomAttribute<RequiredMemberAttribute>() is null)
			.Should()
			.BeEmpty();
		type.GetConstructors(BindingFlags.Instance | BindingFlags.Public).Where(c => c.GetParameters().Length > 0).Should().BeEmpty();
	}

	[Test]
	public void VersionIndexEntry_IsDeserializedWithItsVersion()
	{
		var load = VersionIndexLoader.Load("""{ "elastic/elasticsearch": { "openapi.json": { "9": { "version": "9.5" } } } }""");

		load.Index.Should().NotBeNull();
		load.Index!["elastic/elasticsearch"]["openapi.json"]["9"].Version.Should().Be("9.5");
	}

	[Test]
	public void VersionIndexEntry_WithoutVersion_IsAParseError()
	{
		var load = VersionIndexLoader.Load("""{ "elastic/elasticsearch": { "openapi.json": { "9": { } } } }""");

		load.Index.Should().BeNull();
		load.Error.Should().NotBeNull();
	}
}
