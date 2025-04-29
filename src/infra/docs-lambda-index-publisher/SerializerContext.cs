// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Amazon.Lambda.SQSEvents;
using Amazon.S3.Util;

namespace Elastic.Documentation.Lambda.LinkIndexUploader;

[JsonSerializable(typeof(SQSEvent))]
[JsonSerializable(typeof(S3EventNotification))]
[JsonSerializable(typeof(SQSBatchResponse))]
public partial class SerializerContext : JsonSerializerContext;
