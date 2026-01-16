// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation;

namespace Elastic.Portal.Navigation;

/// <summary>
/// Context interface for portal navigation creation and error reporting.
/// </summary>
public interface IPortalDocumentationContext : IDocumentationContext
{
	/// <summary>
	/// Emits an error during portal navigation construction.
	/// </summary>
	void EmitError(string message);
}
