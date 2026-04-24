// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder;

/// <summary>
/// Global CLI options available to every command via argh's first-parameter injection.
/// </summary>
public class GlobalCliOptions
{
	/// <summary>-l,--log-level, Log verbosity level: trace, debug, information, warning, error, critical. Default: information</summary>
	public LogLevel LogLevel { get; set; } = LogLevel.Information;

	/// <summary>-c,--config-source, Override the configuration source: local, remote</summary>
	//public ConfigurationSource? ConfigurationSource { get; set; }

	/// <summary>Skip cloning private repositories</summary>
	public bool SkipPrivateRepositories { get; set; }
}
