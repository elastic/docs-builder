// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

// index.json at the root of the elastic-docs-openapi-specs bucket, keyed by "org/repo", then by spec
// file basename, then by version moniker ("main", "9", "8", ...). The entry type, the wire format and the
// checked loader (VersionIndexLoader) live in Elastic.Documentation.OpenApiIndex, shared with the publisher.
global using RootVersionIndex = System.Collections.Generic.SortedDictionary<string, System.Collections.Generic.SortedDictionary<string, System.Collections.Generic.SortedDictionary<string, Elastic.Documentation.OpenApiIndex.VersionIndexEntry>>>;
