// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Assembler.ApiCrawl;
using Elastic.Documentation.Links;

namespace Elastic.Documentation.Build.Tests.ApiCrawl;

/// <summary>Two APIs: elasticsearch with an older major, a current major and its numbered copy, and main; cloud with one tree.</summary>
internal static class ApiCrawlFixture
{
	public const string LinksJson =
		"""
		{
		  "origin": { "branch": "main", "remote": "elastic/docs-content", "ref": "abc", "name": "docs-content" },
		  "url_path_prefix": "",
		  "links": {},
		  "cross_links": [],
		  "floating": {
		    "api/elasticsearch": {
		      "versions": [
		        { "name": "8", "root": "api/elasticsearch/v8", "hidden": true },
		        { "name": "9", "root": "api/elasticsearch", "mirror_root": "api/elasticsearch/v9", "current": true },
		        { "name": "main", "root": "api/elasticsearch/next", "hidden": true }
		      ],
		      "pages": { "": 7, "operation/operation-search": 7, "operation/operation-new": 4 }
		    },
		    "api/cloud": {
		      "versions": [ { "name": "main", "root": "api/cloud", "current": true } ],
		      "pages": { "": 1, "operation/operation-list": 1 }
		    }
		  }
		}
		""";

	public static ApiUrlModel Model { get; } = ApiUrlModel.Create(RepositoryLinks.Deserialize(LinksJson));
}
