// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;

namespace Elastic.Markdown.Helpers;

internal sealed class ReusableStringWriter : TextWriter
{
	private static UnicodeEncoding? CurrentEncoding;

	private StringBuilder? _sb;

	public override Encoding Encoding => CurrentEncoding ??= new UnicodeEncoding(false, false);

	public void SetStringBuilder(StringBuilder sb) => _sb = sb;

	public void Reset() => _sb = null;

	public override void Write(char value) => _sb?.Append(value);

	public override void Write(char[] buffer, int index, int count)
	{
		ArgumentNullException.ThrowIfNull(buffer);
		ArgumentOutOfRangeException.ThrowIfNegative(index);
		ArgumentOutOfRangeException.ThrowIfNegative(count);

		if (buffer.Length - index < count)
			throw new ArgumentException("Out of range");

		_ = _sb?.Append(buffer, index, count);
	}

	public override void Write(ReadOnlySpan<char> buffer) => _sb?.Append(buffer);

	public override void Write(string? value)
	{
		if (value is not null)
			_ = _sb?.Append(value);
	}

	public override void Write(StringBuilder? value) => _sb?.Append(value);

	public override void WriteLine(ReadOnlySpan<char> buffer)
	{
		_ = _sb?.Append(buffer);
		WriteLine();
	}

	public override void WriteLine(StringBuilder? value)
	{
		_ = _sb?.Append(value);
		WriteLine();
	}

	#region Task based Async APIs

	public override Task WriteAsync(char value)
	{
		Write(value);
		return Task.CompletedTask;
	}

	public override Task WriteAsync(string? value)
	{
		Write(value);
		return Task.CompletedTask;
	}

	public override Task WriteAsync(char[] buffer, int index, int count)
	{
		Write(buffer, index, count);
		return Task.CompletedTask;
	}

	public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return Task.FromCanceled(cancellationToken);

		Write(buffer.Span);
		return Task.CompletedTask;
	}

	public override Task WriteAsync(StringBuilder? value, CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return Task.FromCanceled(cancellationToken);

		_ = _sb?.Append(value);
		return Task.CompletedTask;
	}

	public override Task WriteLineAsync(char value)
	{
		WriteLine(value);
		return Task.CompletedTask;
	}

	public override Task WriteLineAsync(string? value)
	{
		WriteLine(value);
		return Task.CompletedTask;
	}

	public override Task WriteLineAsync(StringBuilder? value, CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return Task.FromCanceled(cancellationToken);

		_ = _sb?.Append(value);
		WriteLine();
		return Task.CompletedTask;
	}

	public override Task WriteLineAsync(char[] buffer, int index, int count)
	{
		WriteLine(buffer, index, count);
		return Task.CompletedTask;
	}

	public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return Task.FromCanceled(cancellationToken);

		WriteLine(buffer.Span);
		return Task.CompletedTask;
	}

	public override Task FlushAsync() => Task.CompletedTask;

	#endregion
}
