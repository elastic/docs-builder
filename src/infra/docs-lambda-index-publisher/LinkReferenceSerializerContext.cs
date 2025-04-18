// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Markdown.IO.State;
using Elastic.Markdown.Links.CrossLinks;

namespace Elastic.Documentation.Lambda.LinkIndexUploader;

[JsonSerializable(typeof(LinkReference))]
[JsonSerializable(typeof(LinkIndex))]
[JsonSerializable(typeof(LinkIndexEntry))]
public partial class LinkReferenceSerializerContext : JsonSerializerContext;
