// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Api.Core.AskAi;

public interface IAskAiGateway<T>
{
	Task<T> AskAi(AskAiRequest askAiRequest, Cancel ctx = default);
}
