// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>
/// A loader, declared once: how a file's text becomes a DTO (<c>parse</c>, the only code that touches the DTO), how the DTO becomes the record
/// (<c>map</c>, which reports its own findings to a <see cref="MapContext"/>), and what a failure means. The record is then checked once against
/// its own <c>T.Constraints</c>; there is no other set to name, so a loader cannot pick its rules.
/// </summary>
internal sealed class Loader<TDto, T> where T : class, Nullean.Entities.IHaveConstraints<T>
{
	private readonly Func<string, string, TDto> _parse;
	private readonly Func<TDto, MapContext, T> _map;
	private readonly bool _errorsAreFatal;
	private readonly Func<Exception, string> _describe;

	internal Loader(Func<string, string, TDto> parse, Func<TDto, MapContext, T> map, bool errorsAreFatal, Func<Exception, string> describe)
	{
		_parse = parse;
		_map = map;
		_errorsAreFatal = errorsAreFatal;
		_describe = describe;
	}

	/// <summary>
	/// Parses, maps and checks. A step that throws (malformed input, an unresolvable reference) becomes an error finding whose text is the declared
	/// <c>describe</c> of the exception, after the findings the map step made before it threw. Every violation rides in the result; when the loader is
	/// declared with <c>errorsAreFatal</c> an error means <see cref="Unreadable{T}"/>. Cancellation and out-of-memory propagate.
	/// </summary>
	public LoadResult<T> Load(string text, string file)
	{
		var context = new MapContext(file);
		T value;
		try
		{
			value = _map(_parse(text, file), context);
		}
		catch (Exception e) when (e is not (OutOfMemoryException or OperationCanceledException))
		{
			return new Unreadable<T>([
				.. context.Findings,
				new Diagnostic { Severity = Severity.Error, File = file, Message = _describe(e) }
			]);
		}

		var findings = (IReadOnlyList<Diagnostic>)[.. context.Findings, .. value.Check(file).Findings];
		return _errorsAreFatal && findings.Any(d => d.Severity == Severity.Error)
			? new Unreadable<T>(findings)
			: new Loaded<T>(value, findings);
	}
}

internal static class Loader
{
	/// <summary>Declares a loader. <paramref name="describe"/> defaults to the exception's message, so a loader keeps the text its parser always gave.</summary>
	public static Loader<TDto, T> For<TDto, T>(
		Func<string, string, TDto> parse,
		Func<TDto, MapContext, T> map,
		bool errorsAreFatal = true,
		Func<Exception, string>? describe = null
	) where T : class, Nullean.Entities.IHaveConstraints<T> => new(parse, map, errorsAreFatal, describe ?? (static e => e.Message));
}
