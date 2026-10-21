// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>
/// The one way a loader turns a file into a record: parse (the only code that touches the DTO), map (DTO to record), then check the
/// record once against the aggregate's <see cref="Nullean.Entities.ConstraintSet{T}"/> and return a <see cref="Result{T}"/>. A loader is its
/// <c>Parse</c> + <c>Map</c> + a <c>Rules</c> reference; the check cannot be skipped because there is no other public way to produce the record.
/// </summary>
internal static class LoaderPipeline
{
	/// <summary>
	/// Parses, maps and checks. A step that throws (malformed input, an unresolvable reference) becomes an error diagnostic carrying the exception's
	/// message, so a caller that requires the record fails with the message the parser always gave. Every violation rides in the result;
	/// when <paramref name="errorsAreFatal"/> an error means no value.
	/// </summary>
	/// <param name="file">The file the diagnostics are reported against.</param>
	/// <param name="parse">Reads the DTO from the file or text.</param>
	/// <param name="map">Builds the record from the DTO. May add findings to <paramref name="extra"/>.</param>
	/// <param name="rules">The aggregate's constraint set.</param>
	/// <param name="extra">Findings made while mapping (warnings the loader raises itself); read after <paramref name="map"/> ran, so it may be filled by it.</param>
	/// <param name="fileOf">The file to report violations against when the record knows it better than the loader (a legacy file name).</param>
	/// <param name="errorsAreFatal">When false the value is returned even when it has errors, for a caller that decides whether to stop.</param>
	public static Result<T> Run<TDto, T>(
		string file,
		Func<TDto> parse,
		Func<TDto, T> map,
		Nullean.Entities.ConstraintSet<T> rules,
		IReadOnlyList<Diagnostic>? extra = null,
		Func<T, string?>? fileOf = null,
		bool errorsAreFatal = true
	) where T : class
	{
		var built = Build(file, () => map(parse()));
		return built.Value is { } value ? Check(value, fileOf?.Invoke(value) ?? file, rules, extra, errorsAreFatal) : built;
	}

	/// <summary>
	/// Only builds: a step that throws becomes an error diagnostic, nothing is checked. For a part whose rules relate it to another part
	/// (a pair set the loader runs once both exist).
	/// </summary>
	public static Result<T> Build<T>(string file, Func<T> build) where T : class
	{
		try
		{
			return Result.Success(build());
		}
		catch (Exception e) when (e is not OutOfMemoryException)
		{
			return Failed<T>(file, e.Message);
		}
	}

	/// <summary><see cref="Run{TDto,T}"/> for a source whose parse and map are one step.</summary>
	public static Result<T> Run<T>(
		string file,
		Func<T> build,
		Nullean.Entities.ConstraintSet<T> rules,
		IReadOnlyList<Diagnostic>? extra = null,
		Func<T, string?>? fileOf = null,
		bool errorsAreFatal = true
	) where T : class => Run(file, build, static value => value, rules, extra, fileOf, errorsAreFatal);

	/// <summary>Checks a record built elsewhere once, against every set in <paramref name="rules"/>, reporting all violations.</summary>
	public static Result<T> Check<T>(
		T value,
		string file,
		Nullean.Entities.ConstraintSet<T> rules,
		IReadOnlyList<Diagnostic>? extra = null,
		bool errorsAreFatal = true
	) where T : class => Conclude(Result.From(rules.Report(value), file, extra), errorsAreFatal);

	/// <summary>the check for a record two sets apply to (a base set plus a stage's set).</summary>
	public static Result<T> Check<T>(T value, string file, IReadOnlyList<Nullean.Entities.ConstraintSet<T>> rules) where T : class =>
		Conclude(new Result<T>(value, [.. rules.SelectMany(set => set.Report(value).ToDiagnostics(file))]), errorsAreFatal: true);

	/// <summary>An error diagnostic from a step that threw or could not start.</summary>
	public static Result<T> Failed<T>(string file, string message) where T : class =>
		Result.Failure<T>([new Diagnostic { Severity = Severity.Error, File = file, Message = message }]);

	private static Result<T> Conclude<T>(Result<T> result, bool errorsAreFatal) where T : class =>
		errorsAreFatal && result.HasErrors ? Result.Failure<T>(result.Diagnostics) : result;
}
