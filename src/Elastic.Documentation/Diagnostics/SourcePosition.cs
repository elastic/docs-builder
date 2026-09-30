// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Diagnostics;

/// <summary>Where a declaration sits in its YAML file, so diagnostics about it can point at it after parsing.</summary>
public readonly record struct SourcePosition(int Line, int Column, int EndLine, int EndColumn);
