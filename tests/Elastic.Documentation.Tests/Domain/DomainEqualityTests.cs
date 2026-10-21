// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.CodeDom.Compiler;
using System.Collections;
using System.IO.Abstractions;
using System.Reflection;
using AwesomeAssertions;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// Domain records must have structural (value) equality, so that loading the same input twice yields equal aggregates.
/// C# records already compare scalars, enums, value types and nested records by value; only collections and file
/// handles compare by reference. So exactly the records holding those need Generator.Equals' <c>[Equatable]</c> (whose
/// analyzers then require an equality attribute on each such member), and no other record should carry it.
/// </summary>
public class DomainEqualityTests
{
	private static bool IsRecord(Type t) => t.GetMethod("<Clone>$") is not null;

	/// <summary>
	/// Generator.Equals' attributes are <c>[Conditional]</c> and vanish from metadata, so detect its output instead:
	/// an <c>Equals(T)</c> marked <c>[GeneratedCode("Generator.Equals")]</c> rather than the compiler's record equality.
	/// </summary>
	private static bool HasGeneratedEquality(Type t) =>
		t.GetMethod(
			"Equals",
			BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
			[t]
		)?.GetCustomAttribute<GeneratedCodeAttribute>()?.Tool
			== "Generator.Equals";

	private static bool BreaksRecordEquality(Type type) =>
		type != typeof(string) && (typeof(IEnumerable).IsAssignableFrom(type) || typeof(IFileSystemInfo).IsAssignableFrom(type));

	private static IEnumerable<PropertyInfo> ReferenceComparedProperties(Type type) =>
		type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(
			p => BreaksRecordEquality(p.PropertyType)
		);

	private static IEnumerable<Type> DomainRecords() => DomainPurityTests.DomainTypes().Where(t => t.IsClass && IsRecord(t));

	[Test]
	public void RecordsWithCollectionsOrFileHandles_AreEquatable()
	{
		var missing = DomainRecords()
			.Where(t => ReferenceComparedProperties(t).Any() && !HasGeneratedEquality(t))
			.Select(t => $"{t.FullName} ({string.Join(", ", ReferenceComparedProperties(t).Select(p => p.Name))})");

		missing.Should().BeEmpty("records holding collections or file handles need [Equatable] for value equality");
	}

	[Test]
	public void RecordsWithOnlyValueMembers_AreNotAnnotated()
	{
		var unnecessary = DomainRecords()
			.Where(t => HasGeneratedEquality(t) && !ReferenceComparedProperties(t).Any())
			.Select(t => t.FullName);

		unnecessary.Should().BeEmpty("record equality is already structural for these; annotate only what breaks it");
	}
}
