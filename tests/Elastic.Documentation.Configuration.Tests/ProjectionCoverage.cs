// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using AwesomeAssertions;
using TUnit.Assertions.Exceptions;

namespace Elastic.Documentation.Configuration.Tests;

internal readonly record struct IgnoredProjectionMember(string Name, string Reason);

internal sealed class ProjectionCoverage<TSource>
{
	private readonly List<string> _mapped = [];
	private readonly List<IgnoredProjectionMember> _ignored = [];

	public ProjectionCoverage<TSource> Maps(params string[] members)
	{
		_mapped.AddRange(members);
		return this;
	}

	public ProjectionCoverage<TSource> Ignores(params IgnoredProjectionMember[] members)
	{
		_ignored.AddRange(members);
		return this;
	}

	public void ShouldBeComplete()
	{
		var sourceMembers = typeof(TSource)
			.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			.Where(p => p.SetMethod is not null && p.GetIndexParameters().Length == 0)
			.Select(p => p.Name)
			.ToHashSet(StringComparer.Ordinal);
		var mapped = _mapped.ToHashSet(StringComparer.Ordinal);
		var ignored = _ignored.Select(m => m.Name).ToHashSet(StringComparer.Ordinal);
		var accountedFor = mapped.Concat(ignored).ToHashSet(StringComparer.Ordinal);
		var issues = new List<string>();

		issues.AddRange(sourceMembers.Except(accountedFor).Order().Select(m => $"unaccounted source member: {m}"));
		issues.AddRange(accountedFor.Except(sourceMembers).Order().Select(m => $"unknown source member: {m}"));
		issues.AddRange(mapped.Intersect(ignored).Order().Select(m => $"both mapped and ignored: {m}"));
		issues.AddRange(Duplicates(_mapped).Select(m => $"mapped more than once: {m}"));
		issues.AddRange(Duplicates(_ignored.Select(m => m.Name)).Select(m => $"ignored more than once: {m}"));
		issues.AddRange(_ignored.Where(m => string.IsNullOrWhiteSpace(m.Reason)).Select(m => $"ignored without a reason: {m.Name}"));

		issues.Should().BeEmpty($"every settable member on {typeof(TSource).Name} must be mapped or deliberately ignored");
	}

	private static IEnumerable<string> Duplicates(IEnumerable<string> members) =>
		members.GroupBy(m => m, StringComparer.Ordinal).Where(g => g.Count() > 1).Select(g => g.Key).Order();
}

internal static class ProjectionCoverage
{
	public static ProjectionCoverage<TSource> For<TSource>() => new();

	public static IgnoredProjectionMember Ignore(string member, string reason) => new(member, reason);
}

public class ProjectionCoverageTests
{
	private sealed class ExampleDto
	{
		public string Mapped { get; set; } = string.Empty;
		public string DeliberatelyIgnored { get; set; } = string.Empty;
	}

	[Test]
	public void ShouldBeComplete_AllMembersAccountedFor_Passes() =>
		ProjectionCoverage
			.For<ExampleDto>()
			.Maps(nameof(ExampleDto.Mapped))
			.Ignores(ProjectionCoverage.Ignore(nameof(ExampleDto.DeliberatelyIgnored), "Only accepted for compatibility."))
			.ShouldBeComplete();

	[Test]
	public void ShouldBeComplete_MemberForgotten_ReportsItsName()
	{
		var act = () => ProjectionCoverage.For<ExampleDto>().Maps(nameof(ExampleDto.Mapped)).ShouldBeComplete();

		act.Should().Throw<AssertionException>().WithMessage("*unaccounted source member: DeliberatelyIgnored*");
	}
}
