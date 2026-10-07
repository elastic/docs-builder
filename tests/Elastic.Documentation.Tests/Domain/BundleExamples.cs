// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using CsCheck;
using Elastic.Documentation.ReleaseNotes;
using Nullean.Entities;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

internal static class BundleExamples
{
	public static BundledEntry Entry(string file, string checksum = "abc", string title = "Add a thing") =>
		new()
		{
			File = new BundledFile { Name = file, Checksum = checksum },
			Type = ChangelogEntryType.Feature,
			Title = title,
			Products = [new ProductReference { ProductId = "elasticsearch" }]
		};

	public static Bundle Bundle(params BundledEntry[] entries) =>
		new() { Products = [new BundledProduct("elasticsearch", "9.0.0")], Entries = entries };

	public static LoadedBundle Loaded(Bundle data, string version = "9.0.0", string? path = null) =>
		new(version, "elasticsearch", "elastic", data, path ?? $"/bundles/elasticsearch-elasticsearch-{version}.yaml", []);

	// no date, or a date with the text it parses from (the shape a loader gives it)
	private static Gen<Authored<DateOnly?>> ReleaseDates { get; } = Gen.OneOf(Gen.Const(
		Authored.Defaulted<DateOnly?>(null)
	), Gen.Int[0, 3650].Select(days =>
	{
		var date = new DateOnly(2020, 1, 1).AddDays(days);
		return Authored.Of<DateOnly?>(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), date);
	}));

	/// <summary>
	/// Generated bundles that satisfy what the model promises: a changelog file named once, a loaded bundle
	/// named after its repo, first product and target, a version and a file once across the loaded bundles, and an amend that removes what its parent has and adds what it does not.
	/// </summary>
	public static Gen<T> Generated<T>() =>
		GenAuto.Of<T>(
			AutoMode.Plausible,
			o => o
				.Identity<BundledFile, string>(f => f.Name)
				.Use(ReleaseDates)
				.Normalize<LoadedBundle>(
					b => b.Data.Products is [{ Target: { } target } product, ..]
						? b with
						{
							// one file name can only follow the convention for one product, so every product is that one
							Data = b.Data with
							{
								Products = [.. b.Data.Products.Select(p => p with { ProductId = product.ProductId, Target = target })]
							},
							FilePath = $"/bundles/{b.Repo}-{product.ProductId}-{target}.yaml"
						}
						: b
				)
				.Normalize<LoadedBundles>(
					m =>
						m with
						{
							Bundles =
							[
								.. m.Bundles.Select(
									(b, i) =>
										b with
										{
											Version = $"9.{i}.0",
											FilePath = b.FilePath.Replace(".yaml", $"-{i}.yaml", StringComparison.Ordinal),
											Data = b.Data with
											{
												Entries =
												[
													.. b
														.Data
														.Entries
														.Select(
															(e, j) =>
																e with
																{
																	File = new BundledFile
																	{
																		Name = $"b{i}-{j}.yaml",
																		Checksum = e.File?.Checksum ?? ""
																	}
																}
														)
												]
											}
										}
								)
							]
						}
				)
				.Normalize<BundleAmendOfParent>(
					m =>
						m with
						{
							Amend = m.Amend with
							{
								ExcludeEntries =
								[
									.. m.Parent.Entries.Take(m.Amend.ExcludeEntries.Count).Select(e => new BundledEntry { File = e.File })
								],
								Entries =
								[
									.. m
										.Amend
										.Entries
										.Select((e, i) => e with { File = new BundledFile { Name = $"added-{i}.yaml", Checksum = "x" } })
								]
							}
						}
				)
		);
}
