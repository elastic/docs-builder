// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Documentation.Assembler.Configuration;

public record GoogleTagManager
{
	[YamlMember(Alias = "enabled")]
	public bool Enabled { get; set; }

	private string _id = string.Empty;

	[YamlMember(Alias = "id")]
	public string Id
	{
		get => _id;
		set
		{
			if (Enabled && string.IsNullOrEmpty(value))
				throw new ArgumentException("Id is required when Enabled is true.");
			_id = value;
		}
	}

	[YamlMember(Alias = "auth")]
	public string? Auth { get; set; }

	[YamlMember(Alias = "preview")]
	public string? Preview { get; set; }

	[YamlMember(Alias = "cookies_win")]
	public string? CookiesWin { get; set; }
}
