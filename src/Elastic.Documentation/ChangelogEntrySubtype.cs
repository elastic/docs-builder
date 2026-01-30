// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using NetEscapades.EnumGenerators;

namespace Elastic.Documentation;

/// <summary>
/// Enum representing changelog entry subtypes (only applicable to breaking changes)
/// </summary>
[EnumExtensions]
public enum ChangelogEntrySubtype
{
	/// <summary>A change that breaks an API.</summary>
	[Display(Name = "api")]
	Api,

	/// <summary>A change that breaks the way something works.</summary>
	[Display(Name = "behavioral")]
	Behavioral,

	/// <summary>A change that breaks the configuration.</summary>
	[Display(Name = "configuration")]
	Configuration,

	/// <summary>A change that breaks a dependency, such as a third-party product.</summary>
	[Display(Name = "dependency")]
	Dependency,

	/// <summary>A change that breaks licensing behavior.</summary>
	[Display(Name = "subscription")]
	Subscription,

	/// <summary>A change that breaks a plugin.</summary>
	[Display(Name = "plugin")]
	Plugin,

	/// <summary>A change that breaks authentication, authorization, or permissions.</summary>
	[Display(Name = "security")]
	Security,

	/// <summary>A breaking change that does not fit into any of the other categories.</summary>
	[Display(Name = "other")]
	Other
}
