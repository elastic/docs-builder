// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;

namespace Elastic.Documentation.Extensions;

public static class IFileInfoExtensions
{
	public static string ReadToEnd(this IFileInfo fileInfo)
	{
		if (!fileInfo.Exists)
			return string.Empty;

		using var stream = fileInfo.OpenRead();
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}
}
