// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;
using System.Xml;

namespace Elastic.Documentation.Journeys.Cli.Commands;

internal static class JUnitReport
{
	public static void Write(string path, string suite, IReadOnlyList<JourneyOutcome> outcomes)
	{
		_ = Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
		var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
		using var writer = XmlWriter.Create(path, settings);
		var failures = outcomes.Count(o => !o.Result.Passed);
		writer.WriteStartDocument();
		writer.WriteStartElement("testsuite");
		writer.WriteAttributeString("name", suite);
		writer.WriteAttributeString("tests", outcomes.Count.ToString(CultureInfo.InvariantCulture));
		writer.WriteAttributeString("failures", failures.ToString(CultureInfo.InvariantCulture));
		writer.WriteAttributeString("time", outcomes.Sum(o => o.Elapsed.TotalSeconds).ToString("0.###", CultureInfo.InvariantCulture));
		foreach (var outcome in outcomes)
		{
			writer.WriteStartElement("testcase");
			writer.WriteAttributeString("classname", outcome.File.Area);
			writer.WriteAttributeString("name", outcome.File.Name);
			writer.WriteAttributeString("time", outcome.Elapsed.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture));
			if (outcome.Result.Failure is { } failure)
			{
				writer.WriteStartElement("failure");
				writer.WriteAttributeString("message", failure.Command);
				writer.WriteString(failure.Describe(outcome.File.Id));
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
		writer.WriteEndDocument();
	}
}
