// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using CsCheck;
using DotNet.Globbing;
using Elastic.Documentation.Deploying.Synchronization;
using Elastic.Documentation.Tests.Domain;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Deploying.Tests;

/// <summary>
/// Service-algorithm laws of the sync planner (<see cref="SyncPlanner.Compute"/>, the decision <c>AwsS3SyncPlanStrategy</c> makes once it has listed the
/// bucket and hashed the files) and of the delete threshold, with a fixed seed.
/// </summary>
public class SyncPlannerProperties
{
	/// <summary>One path of the universe: where it exists (local, remote with an ETag, a local symlink the planner cannot list) and the local ETag.</summary>
	private sealed record Node(string Path, bool Local, string? RemoteETag, string LocalETag, bool Symlink);

	private static Gen<string> Path { get; } = Gen.Select(
		Gen.OneOfConst("a", "b", "docs", "x/y", "_preview/pr-1", "tmp"),
		Gen.Int[0, 12],
		Gen.OneOfConst("html", "tmp", "css"),
		(d, i, ext) => $"{d}/{i}.{ext}"
	);

	private static Gen<string[]> Excludes { get; } = Gen
		.OneOfConst("*.tmp", "_preview/*", "tmp/*", "docs/1*")
		.Array[0, 2]
		.Select(a => a.Distinct().ToArray());

	private static Gen<Node[]> Nodes { get; } = Gen
		.Select(
			Path,
			Gen.Int[0, 5],
			Gen.Int[0, 1],
			Gen.Bool,
			(p, state, etag, symlink) => new Node(
				p,
				state is 0 or 2 or 3 or 5,
				state is 1 or 2 or 3 ? "r" + etag : null,
				state == 3 ? "r" + etag : "l" + etag,
				state is 1 or 4 && symlink
			)
		)
		.Array[0, 40]
		.Select(a => a.GroupBy(n => n.Path).Select(g => g.First()).ToArray());

	private static Glob[] Globs(string[] excludes) => excludes.Select(p => Glob.Parse(SyncPlanner.NormalizePattern(p))).ToArray();

	private static (List<DeleteRequest> Deletes, List<AddRequest> Adds, List<UpdateRequest> Updates, List<SkipRequest> Skips) Plan(
		IEnumerable<Node> nodes,
		Glob[] globs
	)
	{
		var nodeList = nodes.ToList();
		var local = nodeList.Where(n => n.Local).ToList();
		var etags = local.ToDictionary(n => $"/out/{n.Path}", n => n.LocalETag);
		var exists = nodeList.Where(n => n.Local || n.Symlink).Select(n => n.Path).ToHashSet();
		var remote = nodeList.Where(n => n.RemoteETag is not null).ToDictionary(n => n.Path, n => n.RemoteETag!);
		return SyncPlanner.Compute(local.Select(n => ($"/out/{n.Path}", n.Path)), remote, p => etags[p], exists.Contains, globs);
	}

	/// <summary>
	/// Law: add, update, skip and delete partition the union of the inputs. Every non-excluded local file is in exactly one of add (not remote), update
	/// (remote, other ETag) or skip (remote, same ETag); every non-excluded remote key without a local file is a delete and no other is; no destination
	/// is in two lists of the three upload lists; an excluded path is in none.
	/// Consequence if broken: a deploy loses a file, uploads one twice or deletes a file that exists locally.
	/// </summary>
	[Test]
	public void Compute_PartitionsTheInputs() =>
		Gen.Select(Nodes, Excludes).Check(x =>
		{
			var (nodes, excludes) = x;
			var globs = Globs(excludes);

			var (deletes, adds, updates, skips) = Plan(nodes, globs);

			var included = nodes.Where(n => !SyncPlanner.IsExcluded(n.Path, globs)).ToList();
			adds
				.Select(a => a.DestinationPath)
				.Should()
				.BeEquivalentTo(included.Where(n => n.Local && n.RemoteETag is null).Select(n => n.Path));
			updates
				.Select(a => a.DestinationPath)
				.Should()
				.BeEquivalentTo(included.Where(n => n.Local && n.RemoteETag is not null && n.RemoteETag != n.LocalETag).Select(n => n.Path));
			skips
				.Select(a => a.DestinationPath)
				.Should()
				.BeEquivalentTo(included.Where(n => n.Local && n.RemoteETag is not null && n.RemoteETag == n.LocalETag).Select(n => n.Path));
			deletes
				.Select(a => a.DestinationPath)
				.Should()
				.BeEquivalentTo(included.Where(n => !n.Local && !n.Symlink && n.RemoteETag is not null).Select(n => n.Path));

			var uploads = adds
				.Select(a => a.DestinationPath)
				.Concat(updates.Select(a => a.DestinationPath))
				.Concat(skips.Select(a => a.DestinationPath))
				.ToList();
			uploads.Should().OnlyHaveUniqueItems();
			uploads.Concat(deletes.Select(d => d.DestinationPath)).Where(p => SyncPlanner.IsExcluded(p, globs)).Should().BeEmpty();
			uploads.Count.Should().Be(included.Count(n => n.Local));
			deletes.Select(d => d.DestinationPath).Intersect(uploads).Should().BeEmpty("a file to delete is never a file to upload");
		});

	/// <summary>
	/// Law: applying the plan to the remote listing (add and update store the local ETag, delete removes the key) and planning again gives an empty
	/// plan: nothing to add, update or delete, every non-excluded local file skipped.
	/// Consequence if broken: a deploy is never finished; the second run keeps re-uploading or deleting the same files.
	/// </summary>
	[Test]
	public void Compute_ThenApply_ThenCompute_IsEmpty() =>
		Gen.Select(Nodes, Excludes).Check(x =>
		{
			var (nodes, excludes) = x;
			var globs = Globs(excludes);
			var (deletes, adds, updates, skips) = Plan(nodes, globs);
			var deleted = deletes.Select(d => d.DestinationPath).ToHashSet();
			var written = adds.Select(a => a.DestinationPath).Concat(updates.Select(u => u.DestinationPath)).ToHashSet();

			var applied = nodes
				.Where(n => !deleted.Contains(n.Path))
				.Select(n => written.Contains(n.Path) ? n with { RemoteETag = n.LocalETag } : n)
				.ToList();
			var (deletes2, adds2, updates2, skips2) = Plan(applied, globs);

			deletes2.Should().BeEmpty();
			adds2.Should().BeEmpty();
			updates2.Should().BeEmpty();
			skips2.Count.Should().Be(adds.Count + updates.Count + skips.Count);
		});

	/// <summary>
	/// Law: a delete is only ever a remote key with no local file that no exclude pattern matches; a local symlink counts as a local file.
	/// Consequence if broken: a deploy deletes a page that is still built, or one the operator told it to leave alone.
	/// </summary>
	[Test]
	public void Deletes_AreRemoteOnly_AndNotExcluded() =>
		Gen.Select(Nodes, Excludes).Check(x =>
		{
			var (nodes, excludes) = x;
			var globs = Globs(excludes);

			var (deletes, _, _, _) = Plan(nodes, globs);

			foreach (var d in deletes)
			{
				var node = nodes.Single(n => n.Path == d.DestinationPath);
				node.RemoteETag.Should().NotBeNull();
				node.Local.Should().BeFalse();
				node.Symlink.Should().BeFalse();
				SyncPlanner.IsExcluded(d.DestinationPath, globs).Should().BeFalse();
			}
		});

	/// <summary>
	/// Law: the plan does not depend on the order the local files were listed in (the file system lists them in any order, and the planner once
	/// collected them concurrently).
	/// Consequence if broken: two runs over the same build and the same bucket plan different work.
	/// </summary>
	[Test]
	public void Compute_IsIndependentOfListingOrder() =>
		Gen.Select(Nodes, Excludes, Gen.Int).Check(x =>
		{
			var (nodes, excludes, salt) = x;
			var globs = Globs(excludes);
			var shuffled = nodes.OrderBy(n => (n.Path.GetHashCode() ^ salt) & 0xFFFF).ThenBy(n => n.Path, StringComparer.Ordinal).ToArray();

			var a = Plan(nodes, globs);
			var b = Plan(shuffled, globs);

			Paths(a).Should().BeEquivalentTo(Paths(b));

			static (string[], string[], string[], string[]) Paths(
				(List<DeleteRequest> D, List<AddRequest> A, List<UpdateRequest> U, List<SkipRequest> S) p
			) =>
				(p.D.Select(r => r.DestinationPath).Order().ToArray(), p.A.Select(r => r.DestinationPath).Order().ToArray(), p
					.U
					.Select(r => r.DestinationPath)
					.Order()
					.ToArray(), p.S.Select(r => r.DestinationPath).Order().ToArray());
		});

	/// <summary>
	/// Law: the delete threshold is never below the user's (or the default) one, is 0 for an empty bucket, at least 0.8 up to 100 remote files and at
	/// least 0.5 up to 1,000; a plan is within it exactly when its delete ratio does not exceed it, and the validator's verdict is that
	/// the listing was read to completion, there are source files and the ratio is within the threshold.
	/// Consequence if broken: a runaway deletion is let through, or a young bucket can never change.
	/// </summary>
	[Test]
	public void DeleteThreshold_FollowsTheRatioRule() =>
		Gen.Select(
			Gen.Bool,
			Gen.OneOfConst<float?>(null, 0f, 0.1f, 0.2f, 0.9f, 1f),
			Gen.Int[0, 3000],
			Gen.Int[0, 5000],
			Gen.Int[0, 3000]
		).Check(x =>
		{
			var (completed, threshold, remote, source, deleteCount) = x;
			var deletes = Enumerable.Range(0, deleteCount).Select(i => new DeleteRequest { DestinationPath = $"d/{i}.html" }).ToList();
			var plan = SyncPlanLoader.Planned(completed, threshold, [], remote, source, deletes, [], [], []).Value!;
			var user = threshold ?? SyncPlan.DefaultDeleteThreshold;

			var effective = plan.DeleteThreshold;

			if (remote == 0)
				effective.Should().Be(0f);
			else
			{
				effective.Should().BeGreaterThanOrEqualTo(user);
				if (remote <= 100)
					effective.Should().BeGreaterThanOrEqualTo(0.8f);
				else if (remote <= 1000)
					effective.Should().BeGreaterThanOrEqualTo(0.5f);
				else
					effective.Should().Be(user);
			}

			var errors = SyncPlanLoader
				.Check(plan)
				.Diagnostics
				.Where(d => d.Severity == Elastic.Documentation.Diagnostics.Severity.Error)
				.ToList();
			var ratioRule = errors.Any(e => e.Message.StartsWith("Delete ratio", StringComparison.Ordinal));
			ratioRule.Should().Be(plan.DeleteRatio > effective);

			var verdict = new DocsSyncPlanValidator(NullLoggerFactory.Instance).Validate(plan);
			verdict.Valid.Should().Be(completed && source > 0 && !(plan.DeleteRatio > effective));
		});
}
