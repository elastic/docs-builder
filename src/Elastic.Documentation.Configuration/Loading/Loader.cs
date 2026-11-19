// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>
/// How a load treats errors in its findings. <see cref="All"/> is the strict default and the behaviour of every loader that does not name a mode.
/// <see cref="CheckErrors"/> and <see cref="None"/> exist only so a loader can keep a tolerant behaviour that already shipped (a file with a mapping error is
/// still used) until that behaviour is deliberately tightened. They hide errors from the outcome, so every use carries a <c>// TOLERANT LOAD:</c> comment
/// (see "Tolerant loads" in Constraints-Conventions.md).
/// </summary>
internal enum Fatal
{
	/// <summary>
	/// The strict default: any error in the findings, whether made while mapping (<c>context.Error</c>, an error written to <c>context.Collector</c>) or
	/// reported by the constraint check, makes the load <see cref="Unreadable{T}"/>.
	/// </summary>
	All,

	/// <summary>
	/// Only an error reported by <c>T.Constraints</c> makes the load <see cref="Unreadable{T}"/>. An error made while mapping is reported but the record is
	/// kept. Legitimate for a loader whose mapping has always reported and continued while its rules were always fatal.
	/// A reader of the result must not assume <see cref="Loaded{T}"/> means the findings hold no error.
	/// </summary>
	CheckErrors,

	/// <summary>
	/// An error never makes the load <see cref="Unreadable{T}"/>: the record is returned with every finding, errors included (a caller that reports and
	/// continues). Only a map step that gives up (returns null) or throws produces <see cref="Unreadable{T}"/>.
	/// </summary>
	None
}

/// <summary>
/// A map step that may give up: returns the record, or null when it cannot produce one (it has reported why to the <see cref="MapContext"/>).
/// Used with a <see cref="Fatal"/> mode; see <see cref="Loader.For{TDto, T}(Func{string, string, TDto}, TryMap{TDto, T}, Fatal, Func{Exception, string}?)"/>.
/// </summary>
internal delegate T? TryMap<in TDto, T>(TDto dto, MapContext context) where T : class;

/// <summary>
/// A parse step that reports to the load's <see cref="MapContext"/> while it reads, before a DTO exists (a deserializer that reports unknown keys to a
/// collector, for example). Its findings come first, ahead of the map step's and the check's. Used with
/// <see cref="Loader.For{TDto, T}(TryParse{TDto}, TryMap{TDto, T}, Fatal, Func{Exception, string}?)"/>.
/// </summary>
internal delegate TDto TryParse<out TDto>(string text, string file, MapContext context);

/// <summary>
/// A loader, declared once: how a file's text becomes a DTO (<c>parse</c>, the only code that touches the DTO), how the DTO becomes the record
/// (<c>map</c>, which reports its own findings to a <see cref="MapContext"/>), and what a failure means. The record is then checked once against
/// its own <c>T.Constraints</c>; there is no other set to name, so a loader cannot pick its rules.
/// </summary>
internal sealed class Loader<TDto, T> where T : class, Nullean.Entities.IHaveConstraints<T>
{
	private readonly TryParse<TDto> _parse;
	private readonly TryMap<TDto, T> _map;
	private readonly Fatal _fatal;
	private readonly Func<Exception, string> _describe;

	internal Loader(TryParse<TDto> parse, TryMap<TDto, T> map, Fatal fatal, Func<Exception, string> describe)
	{
		_parse = parse;
		_map = map;
		_fatal = fatal;
		_describe = describe;
	}

	/// <summary>
	/// Parses, maps and checks. A step that throws (malformed input, an unresolvable reference) becomes an error finding whose text is the declared
	/// <c>describe</c> of the exception, after the findings the map step made before it threw, and is <see cref="Unreadable{T}"/> in every <see cref="Fatal"/> mode.
	/// A map step that returns null (see <see cref="TryMap{TDto, T}"/>) gives up: <see cref="Unreadable{T}"/> with the findings made so far, and no check.
	/// Otherwise every violation rides in the result, findings made while mapping first and then the check's, and the <see cref="Fatal"/> mode decides whether
	/// an error means <see cref="Unreadable{T}"/>. Cancellation and out-of-memory propagate.
	/// </summary>
	public LoadResult<T> Load(string text, string file)
	{
		var context = new MapContext(file);
		T? value;
		try
		{
			value = _map(_parse(text, file, context), context);
		}
		catch (Exception e) when (e is not (OutOfMemoryException or OperationCanceledException))
		{
			return new Unreadable<T>([
				.. context.Findings,
				new Diagnostic { Severity = Severity.Error, File = file, Message = _describe(e) }
			]);
		}

		if (value is null)
			return new Unreadable<T>([.. context.Findings]);

		var checkFindings = value.Check(file).Findings;
		var findings = (IReadOnlyList<Diagnostic>)[.. context.Findings, .. checkFindings];
		return IsFatal(context.Findings, checkFindings) ? new Unreadable<T>(findings) : new Loaded<T>(value, findings);
	}

	private bool IsFatal(IReadOnlyList<Diagnostic> mapFindings, IReadOnlyList<Diagnostic> checkFindings) => _fatal switch
	{
		Fatal.All => HasError(mapFindings) || HasError(checkFindings),
		Fatal.CheckErrors => HasError(checkFindings),
		_ => false
	};

	private static bool HasError(IReadOnlyList<Diagnostic> findings) => findings.Any(d => d.Severity == Severity.Error);
}

internal static class Loader
{
	/// <summary>
	/// Declares a strict or fully tolerant loader. <paramref name="errorsAreFatal"/> <c>true</c> (the default) is <see cref="Fatal.All"/>; <c>false</c> is
	/// <see cref="Fatal.None"/>. Unchanged from before <see cref="Fatal"/> existed. <paramref name="describe"/> defaults to the exception's message,
	/// so a loader keeps the text its parser always gave.
	/// </summary>
	public static Loader<TDto, T> For<TDto, T>(
		Func<string, string, TDto> parse,
		Func<TDto, MapContext, T> map,
		bool errorsAreFatal = true,
		Func<Exception, string>? describe = null
	) where T : class, Nullean.Entities.IHaveConstraints<T> =>
		new(
			(text, file, _) => parse(text, file),
			(dto, context) => map(dto, context),
			errorsAreFatal ? Fatal.All : Fatal.None,
			describe ?? (static e => e.Message)
		);

	/// <summary>
	/// Declares a loader with an explicit <see cref="Fatal"/> mode and a map step that may give up by returning null (<see cref="TryMap{TDto, T}"/>).
	/// What differs from the strict default depends on the mode. <see cref="Fatal.None"/>: a record built despite any error, mapping or constraint, is
	/// returned as <see cref="Loaded{T}"/> whose findings contain the errors. <see cref="Fatal.CheckErrors"/>: only a record built despite a mapping error is
	/// kept that way; an error from <c>T.Constraints</c> still makes the load <see cref="Unreadable{T}"/> and the record is discarded, so a caller that must
	/// retain a record that violates a rule needs <see cref="Fatal.None"/>. In both, a null from <paramref name="map"/> is <see cref="Unreadable{T}"/>
	/// without running the check. It exists for loaders that have always tolerated mapping errors and that a behaviour-preserving refactor must not tighten.
	/// The mode is required here on purpose: the strict default is the overload above. Every use of a non-default mode carries a <c>// TOLERANT LOAD:</c> comment.
	/// <paramref name="parse"/> receives only text and file; use the overload taking a <see cref="TryParse{TDto}"/> when parsing reports findings.
	/// </summary>
	public static Loader<TDto, T> For<TDto, T>(
		Func<string, string, TDto> parse,
		TryMap<TDto, T> map,
		Fatal fatal,
		Func<Exception, string>? describe = null
	) where T : class, Nullean.Entities.IHaveConstraints<T> =>
		new((text, file, _) => parse(text, file), map, fatal, describe ?? (static e => e.Message));

	/// <summary>
	/// As the overload taking a plain <c>parse</c>, but <paramref name="parse"/> also receives the per-load <see cref="MapContext"/>, so a deserializer that
	/// reports while it reads (an unknown YAML key, before any DTO exists) writes to <see cref="MapContext.Collector"/> instead of throwing. Findings are
	/// ordered: those made by <paramref name="parse"/>, then by <paramref name="map"/>, then the check's. They are kept when a later step throws, and an error
	/// written by <paramref name="parse"/> follows the <see cref="Fatal"/> mode exactly like one written by <paramref name="map"/>.
	/// </summary>
	public static Loader<TDto, T> For<TDto, T>(
		TryParse<TDto> parse,
		TryMap<TDto, T> map,
		Fatal fatal,
		Func<Exception, string>? describe = null
	) where T : class, Nullean.Entities.IHaveConstraints<T> => new(parse, map, fatal, describe ?? (static e => e.Message));
}
