// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Elastic.SiteSearch.Cli.ContentStack;

internal sealed class StateManager
{
	private const string AppName = "elastic-site-search-sourcing";

	public string CacheFolder { get; }

	public StateManager(string? cacheFolderOverride = null)
	{
		CacheFolder = cacheFolderOverride
			?? Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				AppName);
		_ = Directory.CreateDirectory(CacheFolder);
	}

	public T? Load<T>(string fileName, JsonTypeInfo<T> typeInfo)
	{
		var path = ResolvePath(fileName);
		if (!File.Exists(path))
			return default;

		var bytes = File.ReadAllBytes(path);
		return JsonSerializer.Deserialize(bytes, typeInfo);
	}

	public void Save<T>(string fileName, T state, JsonTypeInfo<T> typeInfo)
	{
		var path = ResolvePath(fileName);
		var bytes = JsonSerializer.SerializeToUtf8Bytes(state, typeInfo);
		File.WriteAllBytes(path, bytes);
	}

	public void Delete(string fileName)
	{
		var path = ResolvePath(fileName);
		if (File.Exists(path))
			File.Delete(path);
	}

	private string ResolvePath(string fileName) => Path.Combine(CacheFolder, fileName);
}
