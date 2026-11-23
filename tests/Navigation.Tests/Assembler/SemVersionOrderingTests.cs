// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using AwesomeAssertions;
using Elastic.Documentation.Versions;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

/// <summary>
/// The order <see cref="SemVersion"/> gives versions today, which the assembler uses to pick the current version of a product. Records
/// what it does, build metadata and prerelease cases included, and fixes nothing.
/// </summary>
public class SemVersionOrderingTests
{
	private static readonly string[] Versions =
	[
		"0.0.0",
		"1.0.0-alpha",
		"1.0.0-alpha.1",
		"1.0.0-alpha.beta",
		"1.0.0-beta",
		"1.0.0-beta.2",
		"1.0.0-beta.11",
		"1.0.0-rc.1",
		"1.0.0",
		"1.0.0+build.1",
		"1.0.0+build.2",
		"1.0.0-rc.1+build.1",
		"1.0.0-rc.1+build.2",
		"1.0.1",
		"1.1.0",
		"2.0.0",
		"9.4.0",
		"9.10.0",
		"10.0.0"
	];

	private static SemVersion Parse(string version) =>
		SemVersion.TryParse(version, out var parsed) ? parsed : throw new InvalidOperationException(version);

	[Test]
	public void CompareTo_EveryPair_MatchesSnapshot()
	{
		var parsed = Versions.Select(Parse).ToArray();
		var width = Versions.Max(v => v.Length);
		var sb = new StringBuilder("row.CompareTo(column): < smaller, = equal, > greater\n");
		_ = sb.Append(' ', width + 2);
		for (var i = 0; i < parsed.Length; i++)
			_ = sb.Append((char)('a' + i));
		_ = sb.Append('\n');
		for (var row = 0; row < parsed.Length; row++)
		{
			_ = sb.Append((char)('a' + row)).Append(' ').Append(Versions[row].PadRight(width)).Append(' ');
			for (var column = 0; column < parsed.Length; column++)
				_ = sb.Append(Math.Sign(parsed[row].CompareTo(parsed[column])) switch
				{
					< 0 => '<',
					0 => '=',
					_ => '>'
				});
			_ = sb.Append('\n');
		}
		sb
			.ToString()
			.Should()
			.MatchInlineSnapshot(
				"""
			row.CompareTo(column): < smaller, = equal, > greater
			                    abcdefghijklmnopqrs
			a 0.0.0              =<<<<<<<<<<<<<<<<<<
			b 1.0.0-alpha        ><<<<<<<<<<<<<<<<<<
			c 1.0.0-alpha.1      >><<<<<<<<<<<<<<<<<
			d 1.0.0-alpha.beta   >>><<<<<<<<<<<<<<<<
			e 1.0.0-beta         >>>><<<<<<<<<<<<<<<
			f 1.0.0-beta.2       >>>>><<<<<<<<<<<<<<
			g 1.0.0-beta.11      >>>>>><<<<<<<<<<<<<
			h 1.0.0-rc.1         >>>>>>><<<<>><<<<<<
			i 1.0.0              >>>>>>>>=>>>><<<<<<
			j 1.0.0+build.1      >>>>>>>>=>>>><<<<<<
			k 1.0.0+build.2      >>>>>>>>=>>>><<<<<<
			l 1.0.0-rc.1+build.1 >>>>>>><<<<>><<<<<<
			m 1.0.0-rc.1+build.2 >>>>>>><<<<>><<<<<<
			n 1.0.1              >>>>>>>>>>>>>=<<<<<
			o 1.1.0              >>>>>>>>>>>>>>=<<<<
			p 2.0.0              >>>>>>>>>>>>>>>=<<<
			q 9.4.0              >>>>>>>>>>>>>>>>=<<
			r 9.10.0             >>>>>>>>>>>>>>>>>=<
			s 10.0.0             >>>>>>>>>>>>>>>>>>=
			"""
			);
	}

	[Test]
	public void Sort_ReversedInput_MatchesSnapshot()
	{
		var sorted = Enumerable.Reverse(Versions).Select(Parse).Order().Select(v => v.ToString());
		string
			.Join("\n", sorted)
			.Should()
			.MatchInlineSnapshot(
				"""
			0.0.0
			1.0.0-alpha
			1.0.0-alpha.1
			1.0.0-alpha.beta
			1.0.0-beta
			1.0.0-beta.2
			1.0.0-beta.11
			1.0.0-rc.1+build.2
			1.0.0-rc.1+build.1
			1.0.0-rc.1
			1.0.0+build.1
			1.0.0+build.2
			1.0.0
			1.0.1
			1.1.0
			2.0.0
			9.4.0
			9.10.0
			10.0.0
			"""
			);
	}

	[Test]
	public void Operators_PrereleaseAndMetadata_MatchSnapshot()
	{
		var sb = new StringBuilder();
		foreach (var (left, right) in new[]
		{
			("1.0.0", "1.0.0+build.1"),
			("1.0.0+build.1", "1.0.0"),
			("1.0.0+build.1", "1.0.0+build.2"),
			("1.0.0+build.2", "1.0.0+build.1"),
			("1.0.0-rc.1", "1.0.0"),
			("1.0.0", "1.0.0-rc.1"),
			("1.0.0-rc.1+build.1", "1.0.0-rc.1+build.2"),
			("1.0.0-rc.1+build.2", "1.0.0-rc.1+build.1"),
			("1.0.0-rc.1", "1.0.0-rc.1+build.2"),
			("9.4.0", "9.10.0")
		})
		{
			var l = Parse(left);
			var r = Parse(right);
			_ = sb
				.Append(left)
				.Append(" vs ")
				.Append(right)
				.Append(": < ")
				.Append(l < r)
				.Append(", <= ")
				.Append(l <= r)
				.Append(", > ")
				.Append(l > r)
				.Append(", >= ")
				.Append(l >= r)
				.Append(", == ")
				.Append(l == r)
				.Append(", CompareTo ")
				.Append(Math.Sign(l.CompareTo(r)))
				.Append('\n');
		}
		sb
			.ToString()
			.Should()
			.MatchInlineSnapshot(
				"""
			1.0.0 vs 1.0.0+build.1: < False, <= False, > True, >= True, == False, CompareTo 1
			1.0.0+build.1 vs 1.0.0: < False, <= False, > False, >= False, == False, CompareTo 0
			1.0.0+build.1 vs 1.0.0+build.2: < False, <= False, > True, >= True, == False, CompareTo 1
			1.0.0+build.2 vs 1.0.0+build.1: < False, <= False, > True, >= True, == False, CompareTo 1
			1.0.0-rc.1 vs 1.0.0: < True, <= True, > False, >= False, == False, CompareTo -1
			1.0.0 vs 1.0.0-rc.1: < False, <= False, > True, >= True, == False, CompareTo 1
			1.0.0-rc.1+build.1 vs 1.0.0-rc.1+build.2: < False, <= False, > True, >= True, == False, CompareTo 1
			1.0.0-rc.1+build.2 vs 1.0.0-rc.1+build.1: < False, <= False, > True, >= True, == False, CompareTo 1
			1.0.0-rc.1 vs 1.0.0-rc.1+build.2: < False, <= False, > True, >= True, == False, CompareTo 1
			9.4.0 vs 9.10.0: < True, <= True, > False, >= False, == False, CompareTo -1
			"""
			);
	}

	[Test]
	public void WellKnownVersions_AgainstTheirNeighbours_MatchSnapshot()
	{
		var sb = new StringBuilder();
		foreach (var version in new[] { "0.0.0", "1.0.0", "99998.9.9", "99999.0.0", "99999.0.1" })
		{
			var parsed = Parse(version);
			_ = sb
				.Append(version)
				.Append(": vs ZeroVersion ")
				.Append(Math.Sign(parsed.CompareTo(ZeroVersion.Instance)))
				.Append(", vs AllVersions ")
				.Append(Math.Sign(parsed.CompareTo(AllVersions.Instance)))
				.Append(", equals ZeroVersion ")
				.Append(parsed.Equals(ZeroVersion.Instance))
				.Append(", equals AllVersions ")
				.Append(parsed.Equals(AllVersions.Instance))
				.Append('\n');
		}
		sb
			.ToString()
			.Should()
			.MatchInlineSnapshot(
				"""
			0.0.0: vs ZeroVersion 0, vs AllVersions -1, equals ZeroVersion True, equals AllVersions False
			1.0.0: vs ZeroVersion 1, vs AllVersions -1, equals ZeroVersion False, equals AllVersions False
			99998.9.9: vs ZeroVersion 1, vs AllVersions -1, equals ZeroVersion False, equals AllVersions False
			99999.0.0: vs ZeroVersion 1, vs AllVersions 0, equals ZeroVersion False, equals AllVersions True
			99999.0.1: vs ZeroVersion 1, vs AllVersions 1, equals ZeroVersion False, equals AllVersions False
			"""
			);
	}

	[Test]
	public void TryParse_Inputs_MatchSnapshot()
	{
		var sb = new StringBuilder();
		foreach (var input in new[]
		{
			"1.2.3",
			"1.2.3-alpha.1+meta",
			"01.2.3",
			"1.2",
			"1",
			"v1.2.3",
			" 1.2.3",
			"1.2.3 ",
			"1.2.3-",
			"1.2.3+",
			"1.2.3-01",
			"1.2.3-0a",
			"1.2.3+01",
			"9.0.0-SNAPSHOT",
			"9.x.0",
			"2147483648.0.0",
			"",
			"1.2.3.4"
		})
		{
			var ok = SemVersion.TryParse(input, out var version);
			_ = sb
				.Append('[')
				.Append(input)
				.Append("] => ")
				.Append(ok ? $"{version} (pre='{version!.Prerelease}', meta='{version.Metadata}')" : "false")
				.Append('\n');
		}
		sb
			.ToString()
			.Should()
			.MatchInlineSnapshot(
				"""
			[1.2.3] => 1.2.3 (pre='', meta='')
			[1.2.3-alpha.1+meta] => 1.2.3-alpha.1+meta (pre='alpha.1', meta='meta')
			[01.2.3] => false
			[1.2] => false
			[1] => false
			[v1.2.3] => false
			[ 1.2.3] => false
			[1.2.3 ] => false
			[1.2.3-] => false
			[1.2.3+] => false
			[1.2.3-01] => false
			[1.2.3-0a] => 1.2.3-0a (pre='0a', meta='')
			[1.2.3+01] => 1.2.3+01 (pre='', meta='01')
			[9.0.0-SNAPSHOT] => 9.0.0-SNAPSHOT (pre='SNAPSHOT', meta='')
			[9.x.0] => false
			[2147483648.0.0] => false
			[] => false
			[1.2.3.4] => false
			"""
			);
	}
}
