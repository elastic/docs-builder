// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
namespace Documentation.Generator.Cli;

/// <summary>
/// This exists temporarily for .NET 8.
/// The container builds prepends `dotnet [app].dll` as arguments
/// Fixed in .NET 9: https://github.com/dotnet/sdk-container-builds/issues/559
/// </summary>
public class Arguments
{
	public required string[] Args { get; init; }
	public required bool IsHelp { get; init; }

	public static Arguments Filter(string[] args) =>
		new Arguments { Args = Enumerate(args).ToArray(), IsHelp = args.Contains("-h") || args.Contains("--help") };

	private static IEnumerable<string> Enumerate(string[] args)
	{
		for (var i = 0; i < args.Length; i++)
		{
			switch (i)
			{
				case 0 when args[i] == "dotnet":
				case 1 when args[i].EndsWith(".dll"):
					continue;
				default:
					yield return args[i];
					break;
			}
		}
	}
}
