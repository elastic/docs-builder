// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Catalog;

/// <summary>
/// Every rule a <see cref="VersioningSystemCatalog"/> must satisfy, in the order they are reported. None yet: <c>versions.yml</c> has only ever failed by
/// throwing while it is parsed, and the warnings a stricter reading could add (base after current, a missing id) are not reported today.
/// </summary>
[Constraints<VersioningSystemCatalog>]
public static partial class VersioningSystemConstraints;
