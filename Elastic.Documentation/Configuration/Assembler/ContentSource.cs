// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using NetEscapades.EnumGenerators;

namespace Elastic.Documentation.Model.Configuration.Assembler;

[EnumExtensions]
public enum ContentSource
{
	[Display(Name = "next")]
	[JsonStringEnumMemberName("next")]
	Next,

	[JsonStringEnumMemberName("current")]
	[Display(Name = "current")]
	Current
}
