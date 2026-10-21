// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.Assembler;

public record GoogleTagManager
{
	[YamlPropertyName("enabled")]
	public bool Enabled { get; set; }

	[YamlPropertyName("id")]
	public string? Id
	{
		get;
		set
		{
			if (Enabled && string.IsNullOrEmpty(value))
				throw new ArgumentException("Id is required when Enabled is true.");
			field = value;
		}
	}

	[YamlPropertyName("auth")]
	public string? Auth { get; set; }

	[YamlPropertyName("preview")]
	public string? Preview { get; set; }

	[YamlPropertyName("cookies_win")]
	public string? CookiesWin { get; set; }
}
