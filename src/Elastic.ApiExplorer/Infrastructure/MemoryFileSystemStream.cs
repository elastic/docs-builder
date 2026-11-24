// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>A page renderer's stream that stays in memory, so the page can be rewritten before it is written to disk.</summary>
public sealed class MemoryFileSystemStream : FileSystemStream
{
	private readonly MemoryStream _buffer;

	public MemoryFileSystemStream(string path) : this(new MemoryStream(), path) { }

	private MemoryFileSystemStream(MemoryStream buffer, string path) : base(buffer, path, isAsync: false) => _buffer = buffer;

	/// <summary>What was written, decoded from the buffer in place rather than copied out first.</summary>
	public string ReadAsUtf8()
	{
		Flush();
		return Encoding.UTF8.GetString(_buffer.GetBuffer(), 0, (int)_buffer.Length);
	}
}
