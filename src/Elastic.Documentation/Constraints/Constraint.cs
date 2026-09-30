// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Constraints;

/// <summary>
/// A rule a model must satisfy. Constraints are checked on the model, so they hold however it was built: by a loader,
/// a test or a property-test generator. Loaders only reject input they cannot build a model from.
/// </summary>
/// <param name="Id">Stable, kebab-case identifier, unique within its aggregate's constraint list.</param>
/// <param name="Description">What the rule requires, in one sentence.</param>
/// <param name="Check">Every violation of the rule in the model; empty when the model satisfies it.</param>
public sealed record Constraint<TModel>(string Id, string Description, Func<TModel, IEnumerable<Finding>> Check);

/// <summary>
/// One way a model breaks a constraint, located where the problem was declared when that is known. <see cref="File"/>
/// is set when the problem sits in another file than the aggregate's own, such as a nested <c>toc.yml</c>.
/// </summary>
public readonly record struct Finding(Severity Severity, string Message, int? Line = null, int? Column = null, string? File = null)
{
	public static Finding Error(string message, int? line = null, int? column = null) => new(Severity.Error, message, line, column);

	public static Finding Warning(string message, int? line = null, int? column = null) => new(Severity.Warning, message, line, column);

	public static Finding Hint(string message, int? line = null, int? column = null) => new(Severity.Hint, message, line, column);

	public Finding In(string file) => this with { File = file };
}

/// <summary>A <see cref="Finding"/> attributed to the constraint that produced it.</summary>
public readonly record struct ConstraintViolation(string ConstraintId, Finding Finding)
{
	public Severity Severity => Finding.Severity;

	public string Message => Finding.Message;

	/// <summary>As a diagnostic against <paramref name="file"/>, the file the model was declared in.</summary>
	public Diagnostic ToDiagnostic(string file) =>
		new() { Severity = Finding.Severity, File = file, Message = Finding.Message, Line = Finding.Line, Column = Finding.Column };
}

public static class ConstraintExtensions
{
	/// <summary>Checks every constraint, in order, and returns all violations in the order they were found.</summary>
	public static IReadOnlyList<ConstraintViolation> CheckAll<TModel>(this IEnumerable<Constraint<TModel>> constraints, TModel model) =>
		[.. constraints.SelectMany(c => c.Check(model).Select(f => new ConstraintViolation(c.Id, f)))];
}
