// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#if DEBUG
using Elastic.Documentation.Api.PageFeedback;

namespace Documentation.Builder.Http;

internal sealed class DebugPageFeedbackService : IPageFeedbackService
{
	public Task<bool> UpsertFeedbackAsync(PageFeedbackRecord record, CancellationToken ctx) =>
		Task.FromResult(true);

	public Task<bool> DeleteFeedbackAsync(Guid feedbackId, CancellationToken ctx) =>
		Task.FromResult(true);
}
#endif
