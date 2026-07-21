// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Backfill;

/// <summary>
/// Thrown when a backfill document cannot be safely read or written: the file is not
/// valid JSON, it is not the kind of document the caller asked for, it was written
/// with a schema version this code does not support, or a required field is missing
/// or invalid. The message always says what is wrong in plain terms and, where
/// possible, what to do about it — callers should surface it rather than swallow it,
/// because a half-understood document must never flow further down the pipeline.
/// </summary>
public sealed class BackfillDocumentException : Exception
{
	/// <summary>Creates the exception with a plain-English description of the problem.</summary>
	public BackfillDocumentException(string message) : base(message) { }

	/// <summary>Creates the exception, keeping the lower-level parse error as the inner exception.</summary>
	public BackfillDocumentException(string message, Exception innerException) : base(message, innerException) { }
}
