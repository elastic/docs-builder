// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Slugify;

namespace Elastic.Markdown.Helpers;

public static class SlugExtensions
{
	private static readonly SlugHelper Instance = InitSlugHelper();

	private static SlugHelper InitSlugHelper()
	{
		var config = new SlugHelperConfiguration();
		_ = config.AllowedChars.Remove('.');
		return new SlugHelper(config);
	}

	public static string Slugify(this string? text) => Instance.GenerateSlug(text);
}
