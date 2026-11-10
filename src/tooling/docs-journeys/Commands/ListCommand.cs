// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.Json;
using Nullean.Argh;
using Nullean.Cdp.Journeys;

namespace Elastic.Documentation.Journeys.Cli.Commands;

internal sealed class ListCommand
{
	/// <summary>Lists the journeys with what their header says, or as JSON. Exits with 1 when one cannot be parsed.</summary>
	/// <param name="journeys">-j, A folder of journeys to list instead of the ones compiled into this binary.</param>
	/// <param name="area">-a, Only these areas, comma separated.</param>
	/// <param name="json">Write a JSON array of the journeys and their metadata.</param>
	public Task<int> List(string? journeys = null, string? area = null, bool json = false)
	{
		var loaded = JourneyFile.Find(JourneySource.Folder(journeys), area).Select(LoadedJourney.Load).ToList();
		if (json)
			WriteJson(loaded);
		else
			foreach (var item in loaded)
				Console.Out.WriteLine(Line(item));
		return Task.FromResult(loaded.Any(l => l.Error is not null) ? 1 : 0);
	}

	private static string Line(LoadedJourney item)
	{
		if (item.Error is { } error)
			return $"{item.File.Id}  ERROR {error.ReplaceLineEndings(" ")}";
		var m = item.Journey!.Metadata;
		var parts = new List<string>();
		if (m.Tags.Count > 0)
			parts.Add($"tags={string.Join(',', m.Tags)}");
		if (m.Requires.Count > 0)
			parts.Add($"requires={string.Join(' ', m.Requires)}");
		if (m.Monitor is { } monitor)
			parts.Add(
				$"monitor=schedule:{monitor.Schedule?.ToString() ?? "default"}/locations:{(monitor.Locations.Count == 0 ? "default" : string.Join(',', monitor.Locations))}"
			);
		if (m.Alerts.Count > 0)
			parts.Add($"alert={string.Join(',', m.Alerts)}");
		if (m.Mutates)
			parts.Add("mutates");
		return parts.Count == 0 ? item.File.Id : $"{item.File.Id}  {string.Join(' ', parts)}";
	}

	private static void WriteJson(IReadOnlyList<LoadedJourney> loaded)
	{
		using var stream = new MemoryStream();
		using (var w = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
		{
			w.WriteStartArray();
			foreach (var item in loaded)
			{
				w.WriteStartObject();
				w.WriteString("id", item.File.Id);
				w.WriteString("area", item.File.Area);
				w.WriteString("name", item.File.Name);
				if (item.Error is { } error)
					w.WriteString("error", error);
				else
				{
					var m = item.Journey!.Metadata;
					WriteStrings(w, "tags", m.Tags);
					WriteStrings(w, "requires", m.Requires.Select(r => r.ToString()).ToList());
					WriteStrings(w, "alerts", m.Alerts);
					w.WriteBoolean("mutates", m.Mutates);
					if (m.Monitor is { } monitor)
					{
						w.WriteStartObject("monitor");
						if (monitor.Schedule is { } schedule)
							w.WriteNumber("schedule", schedule);
						WriteStrings(w, "locations", monitor.Locations);
						w.WriteEndObject();
					}
				}
				w.WriteEndObject();
			}
			w.WriteEndArray();
		}
		Console.Out.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));
	}

	private static void WriteStrings(Utf8JsonWriter w, string name, IReadOnlyList<string> values)
	{
		w.WriteStartArray(name);
		foreach (var value in values)
			w.WriteStringValue(value);
		w.WriteEndArray();
	}
}
