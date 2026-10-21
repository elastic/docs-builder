// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using AwesomeAssertions;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// Every rule in the domain assembly with the severity it is declared at, kept in <c>ConstraintSeverities.txt</c>.
/// </summary>
/// <remarks>
/// A rule that rejects a build is an error, and a configuration that fails today must keep failing: weakening a rule is a decision, not a side effect of a
/// refactor. The ledger turns that decision into a diff someone has to read. A rule is recorded as <c>Error</c>, <c>Warning</c> or <c>Hint</c> when it is
/// declared with <c>WithSeverity</c>, and as <c>own</c> when each violation carries its own severity (an error unless the rule's body says otherwise).
/// The ledger does not see a severity changed inside a rule's body, and it does not cover the four sets outside this assembly (OpenApiIndex, Api, Deploying).
/// To accept an intended change, rewrite the ledger with <c>GOLDEN_UPDATE=1</c> and explain the change in the commit.
/// </remarks>
public class ConstraintSeverityLedgerTests
{
	private static string LedgerPath([CallerFilePath] string callerFile = "") =>
		Path.Join(Path.GetDirectoryName(callerFile), "ConstraintSeverities.txt");

	/// <summary>
	/// Law: the declared severity of every rule matches the ledger, and every rule is in it.
	/// Consequence if broken: a rule that used to fail the build now only warns (or a new rule starts life as an error), and nobody decided that.
	/// </summary>
	[Test]
	public async Task DeclaredSeverities_MatchTheLedger()
	{
		var actual = Describe();
		if (Environment.GetEnvironmentVariable("GOLDEN_UPDATE") == "1")
		{
			await File.WriteAllLinesAsync(LedgerPath(), actual);
			return;
		}

		File.Exists(LedgerPath()).Should().BeTrue("the ledger must exist; run with GOLDEN_UPDATE=1 to create it");
		var expected = (await File.ReadAllLinesAsync(LedgerPath())).Where(l => l.Length > 0).ToList();
		var removed = expected.Except(actual).Order().ToList();
		var added = actual.Except(expected).Order().ToList();

		(removed.Select(l => $"- {l}").Concat(added.Select(l => $"+ {l}"))).Should().BeEmpty(
			"a rule's declared severity or presence changed; if intended, regenerate the ledger with GOLDEN_UPDATE=1 and say why in the commit"
		);
	}

	private static List<string> Describe()
	{
		var lines = new List<string>();
		foreach (var type in typeof(Elastic.Documentation.Publishing.AssemblyConstraints)
			.Assembly
			.GetTypes()
			.Where(t => t is { IsPublic: true, IsAbstract: true, IsSealed: true }))
		{
			foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
			{
				if (
					!property.PropertyType.IsGenericType
					|| !property.PropertyType.Name.StartsWith("ConstraintSet`", StringComparison.Ordinal)
				)
					continue;
				var set = property.GetValue(null)!;
				var rules = (IEnumerable)set.GetType().GetMethod("AsConstraints")!.Invoke(set, null)!;
				foreach (var rule in rules)
				{
					var ruleType = rule.GetType();
					var name = (string?)ruleType.GetProperty("Name")!.GetValue(rule) ?? "unnamed";
					var severity = ruleType.GetProperty("SeverityOverride")!.GetValue(rule)?.ToString() ?? "own";
					lines.Add($"{type.Name}.{property.Name} :: {name} = {severity}");
				}
			}
		}

		return [.. lines.Distinct().Order(StringComparer.Ordinal)];
	}
}
