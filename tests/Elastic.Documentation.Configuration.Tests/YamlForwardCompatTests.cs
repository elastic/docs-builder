// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Microsoft.Extensions.Logging.Abstractions;
using SharpYaml;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>
/// Property-based tests ensuring that a released binary is not broken by a newer config/products.yml
/// that contains keys or values the binary does not yet know about (forward-compatibility invariant).
/// Each test documents the law it enforces and why it matters.
/// </summary>
public class YamlForwardCompatTests
{
	private static readonly Gen<string> AlphaNumericString = Gen.Char.AlphaNumeric.Array[1, 20].Select(chars => new string(chars));

	/// <summary>
	/// Law: an unknown top-level product property fails loading with an error that names the key.
	/// Why: a typo'd or renamed key must never be silently ignored. When a key is intentionally
	/// retired, handle it explicitly through a <see cref="YamlUnknownKeyPolicy"/> instead of
	/// tolerating every unknown key.
	/// Consequence: adding a new key to config/products.yml requires releasing a binary that knows it.
	/// </summary>
	[Test]
	public void UnknownTopLevelProductProperty_Throws_NamingTheKey()
	{
		// Prefix ensures no collision with any real ProductDto property name.
		var unknownKeyGen = AlphaNumericString.Select(key => $"__unknown_{key}__");
		unknownKeyGen.Sample(unknownKey =>
		{
			var act =
				() => ParseProducts(
					$"""
				products:
				  widget:
				    display: 'Widget'
				    versioning: 'stack'
				    {unknownKey}: some_value
				"""
				);
			_ = act
				.Should()
				.Throw<YamlException>($"unknown product property '{unknownKey}' must not be silently ignored")
				.WithMessage($"*Unknown key '{unknownKey}'*");
		});
	}

	/// <summary>
	/// Law: any unrecognised string value for release-notes must not throw; the result must be OnRelease.
	/// Why: if a new release-notes value (e.g. "dra") is added to products.yml and the running binary
	/// does not recognise it, it must fall back to OnRelease rather than crashing.
	/// Consequence: an older binary silently treats the product as on-release instead of the new
	/// path — acceptable degradation while a new release catches up.
	/// </summary>
	[Test]
	public void UnknownReleaseNotesValue_DoesNotThrow_AndFallsBackToOnRelease()
	{
		var knownValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "true", "false", "prestage", "dra", "on-release" };

		// "unknown_" prefix ensures we never accidentally generate a known value.
		var unknownValueGen = AlphaNumericString.Select(s => "unknown_" + s).Where(s => !knownValues.Contains(s));

		unknownValueGen.Sample(value =>
		{
			ProductsConfiguration config = default!;
			var act =
				() => config = ParseProducts(
					$"""
				products:
				  widget:
				    display: 'Widget'
				    versioning: 'stack'
				    features:
				      release-notes: {value}
				"""
				);
			act.Should().NotThrow($"unknown release-notes value '{value}' must not crash an older binary");
			config
				.Products["widget"]
				.Features
				.ReleaseNotes
				.Should()
				.Be(ReleaseNotesPath.OnRelease, $"unknown release-notes value '{value}' must fall back to OnRelease");
		});
	}

	private static ProductsConfiguration ParseProducts(string yaml)
	{
		var provider = new ConfigurationFileProvider(new NullLoggerFactory(), new ConfigurationFileSystem());
		var versionsConfig = provider.CreateVersionConfiguration();
		using var reader = new StringReader(yaml);
		return ProductExtensions.CreateProducts(reader, versionsConfig);
	}
}
