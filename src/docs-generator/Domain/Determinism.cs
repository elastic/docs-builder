// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Bogus;

namespace Documentation.Generator.Domain;

public record Determinism
{
	public Determinism(int? seedFileSystem, int? seedContent)
	{
		var randomizer = new Randomizer();
		SeedFileSystem = seedFileSystem ?? randomizer.Int(1, int.MaxValue);
		SeedContent = seedContent ?? randomizer.Int(1, int.MaxValue);
		FileSystem = new Randomizer(SeedFileSystem);
		Contents = new Randomizer(SeedContent);

		ContentProbability = Contents.Float(0.001f, Contents.Float(0.1f));
		FileProbability = FileSystem.Float(0.001f, Contents.Float(0.1f));
	}

	public int SeedFileSystem { get; }
	public int SeedContent { get; }


	public Randomizer FileSystem { get; }
	public Randomizer Contents { get; }

	public float ContentProbability { get; }
	public float FileProbability { get; }

	public static Determinism Random { get; set; } = new(null, null);
}
