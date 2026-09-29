// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// The domain model's bounded-context namespaces hold data, not machinery: no YAML, no logging, no diagnostics
/// collectors, no raw <c>System.IO</c> handles. Loading lives in <c>Elastic.Documentation.Configuration</c>.
/// Domain objects may expose <c>System.IO.Abstractions</c> file handles (<c>IFileInfo</c>, <c>IDirectoryInfo</c>) by
/// design: a resolved documentation set points at real files, and tests supply a <c>MockFileSystem</c>.
/// </summary>
public class DomainPurityTests
{
	/// <summary>Namespaces introduced by the domain model. Existing infrastructure namespaces are out of scope for now.</summary>
	private static readonly string[] DomainNamespaces =
	[
		"Elastic.Documentation.Catalog",
		"Elastic.Documentation.Sources",
		"Elastic.Documentation.DocumentationSets",
		"Elastic.Documentation.Publishing",
		"Elastic.Documentation.Codex",
		"Elastic.Documentation.Content",
		"Elastic.Documentation.Search",
		"Elastic.Documentation.ReleaseNotes"
	];

	private static readonly string[] ForbiddenNamespacePrefixes =
	[
		"YamlDotNet",
		"Nullean.ScopedFileSystem",
		"Microsoft.Extensions.Logging"
	];

	private static readonly Type[] ForbiddenTypes =
	[
		typeof(IDiagnosticsCollector),
		typeof(FileInfo),
		typeof(DirectoryInfo),
		typeof(Stream)
	];

	public static IEnumerable<Type> DomainTypes() =>
		typeof(Diagnostic)
			.Assembly
			.GetTypes()
			.Where(t => t.Namespace is { } ns && DomainNamespaces.Any(d => ns == d || ns.StartsWith(d + ".", StringComparison.Ordinal)))
			.Where(t => !t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)));

	[Test]
	public void DomainTypes_ReferenceNoInfrastructure()
	{
		var violations = DomainTypes()
			.SelectMany(t => ReferencedTypes(t).Where(IsForbidden).Select(r => $"{t.FullName} references {r.FullName}"))
			.Distinct()
			.ToList();

		violations.Should().BeEmpty();
	}

	private static bool IsForbidden(Type type) =>
		ForbiddenTypes.Contains(type)
			|| (type.Namespace is { } ns && ForbiddenNamespacePrefixes.Any(p => ns.StartsWith(p, StringComparison.Ordinal)));

	private const BindingFlags Members = BindingFlags.Public
		| BindingFlags.NonPublic
		| BindingFlags.Instance
		| BindingFlags.Static
		| BindingFlags.DeclaredOnly;

	private static IEnumerable<Type> ReferencedTypes(Type type) =>
		type
			.GetFields(Members)
			.Select(f => f.FieldType)
			.Concat(type.GetProperties(Members).Select(p => p.PropertyType))
			.Concat(type.GetConstructors(Members).SelectMany(c => c.GetParameters()).Select(p => p.ParameterType))
			.Concat(type.GetMethods(Members).SelectMany(m => m.GetParameters().Select(p => p.ParameterType).Append(m.ReturnType)))
			.SelectMany(Unwrap);

	private static IEnumerable<Type> Unwrap(Type type)
	{
		if (type.HasElementType && type.GetElementType() is { } element)
			return Unwrap(element);
		if (type.IsGenericType)
			return type.GetGenericArguments().SelectMany(Unwrap).Append(type.GetGenericTypeDefinition());
		return [type];
	}
}
