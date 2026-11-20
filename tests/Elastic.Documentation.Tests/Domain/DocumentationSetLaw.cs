// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Diagnostics;
using Nullean.Entities;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>The laws every stage of a documentation set is held to, written once over any stage record.</summary>
internal static class DocumentationSetLaw
{
	private const string File = "docset.yml";

	/// <summary>The public instance methods of <typeparamref name="T"/> that return it: the operations <c>AutoTest</c> would run against every rule.</summary>
	public static IReadOnlyList<string> OperationsOf<T>() =>
		[
			.. typeof(T)
				.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
				.Where(m => m.ReturnType == typeof(T) && !m.IsSpecialName && m.Name != "<Clone>$")
				.Select(m => m.Name)
		];

	/// <summary>Every finding of a stage, as the collector would see it, in the order it is reported.</summary>
	public static IReadOnlyList<string> Findings<T>(ConstraintSet<T> rules, T stage) =>
		[.. rules.Report(stage).ToDiagnostics(File).Select(d => $"{d.Severity} {d.RuleId} {d.Message}")];

	/// <summary>The findings of a stage as one text, one line each, for a snapshot.</summary>
	public static string Report<T>(ConstraintSet<T> rules, T stage) => string.Join('\n', Findings(rules, stage));

	/// <summary>Runs the rule set over every generated stage and writes its violations as diagnostics: neither may throw.</summary>
	public static void ReportsWithoutThrowing<T>(ConstraintSet<T> rules, Gen<T> stages) =>
		stages.Select(s => new Printable<T>(s)).Sample(printable => _ = Findings(rules, printable.Value), seed: RunSeed.Value, iter: 500);

	/// <summary>
	/// The findings of a stage are the findings of its parts, each checked alone, one after the other in the order of <paramref name="parts"/>:
	/// no part changes what another reports, and the order is the one resolution reaches them in.
	/// </summary>
	public static void ReportsPartsInOrder<T>(ConstraintSet<T> rules, Gen<T> stages, Func<T, bool> applies, params Func<T, T>[] parts) =>
		stages
			.Where(applies)
			.Select(s => new Printable<T>(s))
			.Sample(
				printable => Findings(rules, printable.Value)
					.Should()
					.Equal([.. parts.SelectMany(part => Findings(rules, part(printable.Value)))]),
				seed: RunSeed.Value,
				iter: 500
			);

	/// <summary>
	/// A list reports the entries in front of the first one with no value and nothing after it: cutting the list at that entry changes no finding.
	/// </summary>
	public static void StopsAtTheFirstEntryWithNoValue<T, TEntry>(
		ConstraintSet<T> rules,
		Gen<T> stages,
		Func<T, IReadOnlyList<TEntry?>?> entries,
		Func<T, IReadOnlyList<TEntry?>, T> withEntries
	) where TEntry : class =>
		stages.Select(s => new Printable<T>(s)).Sample(
			printable =>
			{
				var stage = printable.Value;
				var list = entries(stage);
				var firstNull = list is null ? -1 : list.ToList().FindIndex(e => e is null);
				if (firstNull < 0)
					return;
				Findings(rules, stage).Should().Equal(Findings(rules, withEntries(stage, [.. list!.Take(firstNull + 1)])));
			},
			seed: RunSeed.Value,
			iter: 500
		);

	/// <summary>Names a value in a failure without letting its own <c>ToString</c> hide the real exception.</summary>
	private sealed record Printable<T>(T Value)
	{
		public override string ToString()
		{
			try
			{
				return Value?.ToString() ?? "null";
			}
			catch (Exception e) when (e is NullReferenceException or InvalidOperationException)
			{
				return $"{typeof(T).Name} (ToString throws {e.GetType().Name})";
			}
		}
	}
}
