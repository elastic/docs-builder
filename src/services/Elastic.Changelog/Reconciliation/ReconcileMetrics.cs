// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Reconciliation;

/// <summary>
/// Per-invocation counters for the scrubber pipeline (elastic/docs-eng-team#688 Phase 0
/// observability): once the Lambda owns the public registry, these numbers are what gates any
/// SQS/Lambda tuning — first-heal cost, conditional-write contention, steady-state reuse rate.
/// Thread-safe: entry recomputes run under bounded parallelism.
/// </summary>
public sealed class ReconcileMetrics
{
	private int _objectReconciles;
	private int _objectReconcileRetries;
	private int _groupReconciles;
	private int _registryWrites;
	private int _registryDeletes;
	private int _registryUnchanged;
	private int _writeConflicts;
	private int _objectsListed;
	private int _entriesRecomputed;
	private int _failedMessages;

	/// <summary>Object-level reconciles run (one per distinct key in the batch).</summary>
	public int ObjectReconciles => _objectReconciles;

	/// <summary>Object reconciles redone because the private source changed mid-flight (post-write validation).</summary>
	public int ObjectReconcileRetries => _objectReconcileRetries;

	/// <summary>Group-level reconciles run (one per distinct group in the batch).</summary>
	public int GroupReconciles => _groupReconciles;

	/// <summary>Public manifests written.</summary>
	public int RegistryWrites => _registryWrites;

	/// <summary>Public manifests deleted (empty groups).</summary>
	public int RegistryDeletes => _registryDeletes;

	/// <summary>Group reconciles that found the manifest already exact (steady state).</summary>
	public int RegistryUnchanged => _registryUnchanged;

	/// <summary>Conditional writes (PUT or DELETE) lost to a concurrent writer.</summary>
	public int WriteConflicts => _writeConflicts;

	/// <summary>Objects seen across all group listings.</summary>
	public int ObjectsListed => _objectsListed;

	/// <summary>Entries whose metadata was recomputed from a public YAML read (vs. ETag-reused).</summary>
	public int EntriesRecomputed => _entriesRecomputed;

	/// <summary>SQS messages reported as batch-item failures.</summary>
	public int FailedMessages => _failedMessages;

	internal void IncrementObjectReconciles() => Interlocked.Increment(ref _objectReconciles);
	internal void IncrementObjectReconcileRetries() => Interlocked.Increment(ref _objectReconcileRetries);
	internal void IncrementGroupReconciles() => Interlocked.Increment(ref _groupReconciles);
	internal void IncrementRegistryWrites() => Interlocked.Increment(ref _registryWrites);
	internal void IncrementRegistryDeletes() => Interlocked.Increment(ref _registryDeletes);
	internal void IncrementRegistryUnchanged() => Interlocked.Increment(ref _registryUnchanged);
	internal void IncrementWriteConflicts() => Interlocked.Increment(ref _writeConflicts);
	internal void IncrementObjectsListed() => Interlocked.Increment(ref _objectsListed);
	internal void IncrementEntriesRecomputed() => Interlocked.Increment(ref _entriesRecomputed);
	internal void AddFailedMessages(int count) => Interlocked.Add(ref _failedMessages, count);
}
