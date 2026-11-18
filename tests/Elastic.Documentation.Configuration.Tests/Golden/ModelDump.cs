// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>
/// A loaded model as indented text: every public property and field of every record, in declaration order, one
/// line per value. It reads the model by reflection, so a property added to, removed from or renamed on a record
/// changes the dump instead of slipping past a hand-written list of what to check.
/// <para>
/// Dictionaries keep the order they were loaded in. Hashed collections (frozen dictionaries, sets) are written sorted,
/// because their enumeration order is no part of what the loader promises.
/// </para>
/// </summary>
internal static class ModelDump
{
	private const int MaxDepth = 24;

	public static string ToText(object? model, params string[] skipProperties) => ToText(model, null, skipProperties);

	/// <param name="model">The loaded model.</param>
	/// <param name="summarize">Names a value as one line of text instead of dumping its members, or returns <c>null</c> to dump it.</param>
	/// <param name="skipProperties">Property names to leave out wherever they occur.</param>
	public static string ToText(object? model, Func<object, string?>? summarize, params string[] skipProperties)
	{
		var sb = new StringBuilder();
		var context = new Context(summarize, new HashSet<string>(skipProperties, StringComparer.Ordinal));
		WriteValue(sb, "model", model, 0, [], context);
		return sb.ToString();
	}

	private sealed record Context(Func<object, string?>? Summarize, HashSet<string> Skip);

	private static void WriteValue(StringBuilder sb, string label, object? value, int depth, List<object> path, Context context)
	{
		var pad = new string(' ', depth * 2);
		// a union is its case: the dump shows the value it wraps, as it showed the case before the union existed
		if (value?.GetType().GetInterfaces().Any(i => i.FullName == "System.Runtime.CompilerServices.IUnion") == true)
			value = value.GetType().GetProperty("Value")!.GetValue(value);
		if (value is null || IsScalar(value))
		{
			_ = sb.Append(pad).Append(label).Append(": ").Append(Scalar(value)).Append('\n');
			return;
		}

		if (context.Summarize?.Invoke(value) is { } summary)
		{
			_ = sb.Append(pad).Append(label).Append(": ").Append(summary).Append('\n');
			return;
		}

		if (depth > MaxDepth || path.Any(p => ReferenceEquals(p, value)))
		{
			_ = sb.Append(pad).Append(label).Append(": <cycle ").Append(NameOf(value.GetType())).Append(">\n");
			return;
		}

		path.Add(value);
		try
		{
			if (value is IEnumerable enumerable)
				WriteCollection(sb, label, enumerable, depth, path, context);
			else
				WriteObject(sb, label, value, depth, path, context);
		}
		finally
		{
			path.RemoveAt(path.Count - 1);
		}
	}

	private static void WriteCollection(
		StringBuilder sb,
		string label,
		IEnumerable enumerable,
		int depth,
		List<object> path,
		Context context
	)
	{
		var pad = new string(' ', depth * 2);
		var items = enumerable.Cast<object?>().ToList();
		var isMap = items.Count > 0 && items.All(i => i is not null && IsKeyValuePair(i.GetType())) || IsMapType(enumerable.GetType());
		if (isMap)
			items =
			[
				.. items
					.Select(i => (Key: KeyOf(i), Item: i))
					.OrderBy(e => IsHashed(enumerable) ? e.Key : "", StringComparer.Ordinal)
					.Select(e => e.Item)
			];
		else if (IsHashed(enumerable))
			items = [.. items.OrderBy(i => Scalar(i), StringComparer.Ordinal)];

		if (items.Count == 0)
		{
			_ = sb.Append(pad).Append(label).Append(": ").Append(isMap ? "{}" : "[]").Append('\n');
			return;
		}

		_ = sb.Append(pad).Append(label).Append(':').Append('\n');
		foreach (var item in items)
		{
			if (isMap)
			{
				var kvp = item!;
				WriteValue(sb, KeyOf(kvp), kvp.GetType().GetProperty("Value")!.GetValue(kvp), depth + 1, path, context);
			}
			else if (item is null || IsScalar(item))
				_ = sb.Append(pad).Append("  - ").Append(Scalar(item)).Append('\n');
			else
				WriteValue(sb, "-", item, depth + 1, path, context);
		}
	}

	private static void WriteObject(StringBuilder sb, string label, object value, int depth, List<object> path, Context context)
	{
		var pad = new string(' ', depth * 2);
		var type = value.GetType();
		_ = sb.Append(pad).Append(label).Append(" <").Append(NameOf(type)).Append(">:").Append('\n');
		foreach (var member in Members(type))
		{
			if (context.Skip.Contains(member.Name))
				continue;
			object? memberValue;
			try
			{
				memberValue = member switch
				{
					PropertyInfo p => p.GetValue(value),
					FieldInfo f => f.GetValue(value),
					_ => null
				};
			}
			catch (TargetInvocationException e)
			{
				_ = sb
					.Append(pad)
					.Append("  ")
					.Append(member.Name)
					.Append(": <throws ")
					.Append(e.InnerException?.GetType().Name)
					.Append(">\n");
				continue;
			}
			WriteValue(sb, member.Name, memberValue, depth + 1, path, context);
		}
	}

	private static IEnumerable<MemberInfo> Members(Type type)
	{
		var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(
			p => p.GetIndexParameters().Length == 0 && p.GetMethod is not null && p.Name != "EqualityContract"
		);
		var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
		// base type members first, the way a record declares them
		return fields.Cast<MemberInfo>().Concat(properties.OrderBy(p => Depth(p.DeclaringType!)).ThenBy(p => p.MetadataToken));
	}

	private static int Depth(Type t)
	{
		var d = 0;
		for (var b = t.BaseType; b is not null; b = b.BaseType)
			d++;
		return d;
	}

	private static bool IsScalar(object value) =>
		value is string or bool or char or Enum or Uri or DateTime or DateTimeOffset or TimeSpan or Guid or Version or DateOnly
			|| value.GetType().IsPrimitive
			|| value is decimal
			|| value.GetType().FullName == "Elastic.Documentation.Versions.SemVersion"
			|| value is Type or Delegate;

	private static string Scalar(object? value) => value switch
	{
		null => "null",
		string s => "\"" + s.Replace("\"", "\\\"", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal) + "\"",
		Enum e => e.ToString(),
		Type t => "<type " + t.Name + ">",
		Delegate => "<delegate>",
		IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
		_ => value.ToString() ?? string.Empty
	};

	// generic types are matched by name: the unbound form `typeof(X<,>)` is formatted differently by different curb versions
	/// <summary>
	/// The verified snapshots name these records as they were called when the snapshots were taken: the dump stays byte-identical
	/// across the rename to the catalog types.
	/// </summary>
	private static string NameOf(Type type) => type.Name switch
	{
		"ProductCatalog" => "ProductsConfiguration",
		"VersioningSystemCatalog" => "VersionsConfiguration",
		"LegacyUrlMappingCatalog" => "LegacyUrlMappingConfiguration",
		"RelatedLearningCatalog" => "RelatedLearningConfiguration",
		"SiteNavigationDefinition" => "SiteNavigationFile",
		var name => name
	};

	private static bool IsKeyValuePair(Type t) => t.IsGenericType && t.GetGenericTypeDefinition().Name == "KeyValuePair`2";

	private static bool IsMapType(Type t) =>
		t
			.GetInterfaces()
			.Concat([t])
			.Any(i => i.IsGenericType && i.GetGenericTypeDefinition().Name is "IDictionary`2" or "IReadOnlyDictionary`2");

	private static bool IsHashed(IEnumerable e)
	{
		var t = e.GetType();
		return t.Name.Contains("Frozen", StringComparison.Ordinal)
			|| t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition().Name is "ISet`1" or "IReadOnlySet`1");
	}

	private static string KeyOf(object? kvp) => Scalar(kvp!.GetType().GetProperty("Key")!.GetValue(kvp)).Trim('"');
}
