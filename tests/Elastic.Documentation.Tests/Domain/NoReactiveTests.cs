// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using AwesomeAssertions;
using Assembly = System.Reflection.Assembly;

namespace Elastic.Documentation.Tests.Domain;

public class NoReactiveTests
{
	/// <summary>
	/// Law: nothing the domain tests load, directly or transitively, references System.Reactive or Nullean.Entities.Reactive.
	/// Consequence if broken: Rx enters the build through the test library, and the opt-in Nullean.Entities.CsCheck.Reactive package stops being the only way to get it.
	/// </summary>
	[Test]
	public void The_domain_and_its_tests_reference_no_reactive_assembly_anywhere_in_their_closure()
	{
		var seen = new HashSet<string>();
		var pending = new Stack<Assembly>([typeof(DomainGen).Assembly, typeof(Elastic.Documentation.Links.LinkRegistry).Assembly]);
		while (pending.Count > 0)
		{
			foreach (var reference in pending.Pop().GetReferencedAssemblies().Where(r => seen.Add(r.Name!)))
			{
				if (TryLoad(reference) is { } loaded)
					pending.Push(loaded);
			}
		}

		seen.Should().Contain("Nullean.Entities.CsCheck", "the walk must reach the test library, or the next assertion proves nothing");
		seen.Where(n => n.Contains("Reactive", StringComparison.Ordinal)).Should().BeEmpty();
	}

	private static Assembly? TryLoad(AssemblyName name)
	{
		try
		{
			return Assembly.Load(name);
		}
		catch (Exception e) when (e is FileNotFoundException or FileLoadException or BadImageFormatException)
		{
			return null;
		}
	}
}
