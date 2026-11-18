// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>A page renderer's stream that stays in memory, so the page can be rewritten before it is written to disk.</summary>
public sealed class MemoryFileSystemStream(string path) : FileSystemStream(new MemoryStream(), path, isAsync: false)
{
	public byte[] ToArray()
	{
		Flush();
		var buffer = new MemoryStream();
		var position = Position;
		Position = 0;
		CopyTo(buffer);
		Position = position;
		return buffer.ToArray();
	}
}
