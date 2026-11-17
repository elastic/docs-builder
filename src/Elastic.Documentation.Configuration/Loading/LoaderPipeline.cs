// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>
/// The one way a loader turns a file into a record: parse (the only code that touches the DTO), map (DTO to record), then check the
/// record once against the aggregate's constraint set (<c>T.Constraints</c>, unless <see cref="LoadOptions{T}.Rules"/> names another) and return a
/// <see cref="Result{T}"/>. A loader is its <c>Parse</c> + <c>Map</c>; the pipeline is internal to this assembly, so loaders here use it
/// instead of checking by hand, and a check made anywhere else is a second report of the same finding.
/// </summary>
internal static class LoaderPipeline
{
	/// <summary>
	/// Parses, maps and checks. A step that throws (malformed input, an unresolvable reference) becomes an error diagnostic whose text is
	/// <see cref="LoadOptions{T}.Describe"/> of the exception, so a caller that requires the record fails with the message the parser always gave.
	/// Every violation rides in the result; when <see cref="LoadOptions{T}.ErrorsAreFatal"/> an error means no value.
	/// </summary>
	/// <param name="file">The file the diagnostics are reported against.</param>
	/// <param name="parse">Reads the DTO from the file or text.</param>
	/// <param name="map">Builds the record from the DTO. May add findings to <see cref="LoadOptions{T}.Extra"/>.</param>
	/// <param name="options">What differs from the defaults.</param>
	public static Result<T> Run<TDto, T>(
		string file,
		Func<TDto> parse,
		Func<TDto, T> map,
		LoadOptions<T>? options = null
	) where T : class, Nullean.Entities.IHaveConstraints<T>
	{
		options ??= new LoadOptions<T>();
		var built = Attempt(file, () => map(parse()), options);
		return built.Value is { } value ? Check(value, file, options) : built;
	}

	/// <summary><see cref="Run{TDto,T}"/> for a source whose parse and map are one step.</summary>
	public static Result<T> Run<T>(
		string file,
		Func<T> build,
		LoadOptions<T>? options = null
	) where T : class, Nullean.Entities.IHaveConstraints<T> => Run(file, build, static value => value, options);

	/// <summary>
	/// Only builds: a step that throws becomes an error diagnostic, nothing is checked. For a part whose rules relate it to another part
	/// (a pair set the loader runs once both exist). Honours <see cref="LoadOptions{T}.Extra"/> and <see cref="LoadOptions{T}.Describe"/>.
	/// </summary>
	public static Result<T> Build<T>(string file, Func<T> build, LoadOptions<T>? options = null) where T : class
	{
		options ??= new LoadOptions<T>();
		var built = Attempt(file, build, options);
		return built.Value is { } value ? Result.Success(value, options.Extra) : built;
	}

	/// <summary>Checks a record built elsewhere once and reports every violation.</summary>
	public static Result<T> Check<T>(
		T value,
		string file,
		LoadOptions<T>? options = null
	) where T : class, Nullean.Entities.IHaveConstraints<T>
	{
		options ??= new LoadOptions<T>();
		var rules = options.Rules ?? T.Constraints;
		var checkedFile = options.FileOf?.Invoke(value) ?? file;
		var result = Result.From(rules.Report(value), checkedFile, options.Extra);
		return options.ErrorsAreFatal && result.HasErrors ? Result.Failure<T>(result.Diagnostics) : result;
	}

	/// <summary>An error diagnostic from a step that threw or could not start.</summary>
	public static Result<T> Failed<T>(string file, string message) where T : class => Result.Failure<T>([ErrorOn(file, message)]);

	// The value is carried bare: the caller adds Extra once, on success after checking, on failure here.
	private static Result<T> Attempt<T>(string file, Func<T> build, LoadOptions<T> options) where T : class
	{
		try
		{
			return Result.Success(build());
		}
		catch (Exception e) when (e is not (OutOfMemoryException or OperationCanceledException))
		{
			return Result.Failure<T>([.. options.Extra, ErrorOn(file, options.Describe(e))]);
		}
	}

	private static Diagnostic ErrorOn(string file, string message) => new() { Severity = Severity.Error, File = file, Message = message };
}
