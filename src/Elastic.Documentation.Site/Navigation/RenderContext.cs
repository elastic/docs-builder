// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;

namespace Elastic.Documentation.Site.Navigation;

public interface IRenderContext<out T>
{
	BuildContext BuildContext { get; }
	T Model { get; }
}

public record RenderContext<T>(BuildContext BuildContext, T Model) : IRenderContext<T>;
