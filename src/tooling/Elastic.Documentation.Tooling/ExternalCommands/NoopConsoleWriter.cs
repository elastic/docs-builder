// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ProcNet.Std;

namespace Elastic.Documentation.Tooling.ExternalCommands;

public class NoopConsoleWriter : IConsoleOutWriter
{
	public static readonly NoopConsoleWriter Instance = new();

	public void Write(Exception e) { }

	public void Write(ConsoleOut consoleOut) { }
}
