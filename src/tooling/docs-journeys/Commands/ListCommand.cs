// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Argh;

namespace Elastic.Documentation.Journeys.Cli.Commands;

internal sealed class ListCommand
{
	/// <summary>Lists the journeys, one per line, as <c>area/name</c>.</summary>
	/// <param name="journeys">-j, The folder of journeys. Defaults to the <c>journeys</c> folder next to the binary.</param>
	/// <param name="area">-a, Only these areas, comma separated.</param>
	public Task List(string? journeys = null, string? area = null)
	{
		foreach (var file in JourneyFile.Find(journeys, area))
			Console.Out.WriteLine(file.Id);
		return Task.CompletedTask;
	}
}
