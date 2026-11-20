// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Export;

namespace Elastic.ApiExplorer.Tests;

public class ApiSearchWordsTests
{
	[Test]
	[Arguments("ml-flush-job", "/_ml/anomaly_detectors/{job_id}/_flush", "ml flush job anomaly detectors")]
	[Arguments("indices.get", "/{index}", "indices get")]
	[Arguments("runScriptAction", "/api/endpoint/action/runscript", "run script action api endpoint runscript")]
	[Arguments("resync-deployment", "/deployments/{deployment_id}/_resync", "resync deployment deployments")]
	[Arguments("_bulk", "/_bulk", "bulk")]
	public void Of_OperationIdAndPath_ReturnsLowercaseDistinctWords(string operationId, string path, string words) =>
		string.Join(' ', ApiSearchWords.Of(operationId, path)).Should().Be(words);

	[Test]
	[Arguments("cluster.health", "cluster health")]
	[Arguments("esql-async-query-stop", "esql async query stop")]
	[Arguments("_bulk", "bulk")]
	public void OfOperationId_OperationId_ReturnsIdWordsOnly(string operationId, string words) =>
		string.Join(' ', ApiSearchWords.OfOperationId(operationId)).Should().Be(words);
}
