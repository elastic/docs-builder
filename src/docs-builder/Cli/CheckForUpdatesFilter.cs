// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using ConsoleAppFramework;
using Elastic.Markdown.Helpers;

namespace Documentation.Builder.Cli;

internal sealed class CheckForUpdatesFilter : ConsoleAppFilter
{
	private readonly FileInfo _stateFile;

	public CheckForUpdatesFilter(ConsoleAppFilter next) : base(next)
	{
		// ~/Library/Application\ Support/ on osx
		// XDG_DATA_HOME or home/.local/share on linux
		// %LOCAL_APPLICATION_DATA% windows
		var localPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		var elasticPath = Path.Combine(localPath, "elastic");
		_stateFile = new FileInfo(Path.Combine(elasticPath, "docs-build-check.state"));
	}

	public override async Task InvokeAsync(ConsoleAppContext context, Cancel ctx)
	{
		await Next.InvokeAsync(context, ctx);
		var latestVersionUrl = await GetLatestVersion(ctx);
		if (latestVersionUrl is null)
			ConsoleApp.LogError("Unable to determine latest version");
		else
			CompareWithAssemblyVersion(latestVersionUrl);
	}

	private static void CompareWithAssemblyVersion(Uri latestVersionUrl)
	{
		var versionPath = latestVersionUrl.AbsolutePath.Split('/').Last();
		if (!SemVersion.TryParse(versionPath, out var latestVersion))
		{
			ConsoleApp.LogError($"Unable to parse latest version from {latestVersionUrl}");
			return;
		}

		var assemblyVersion = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyInformationalVersionAttribute>()
			.FirstOrDefault()?.InformationalVersion;
		if (SemVersion.TryParse(assemblyVersion ?? "", out var currentSemVersion))
		{
			if (latestVersion <= currentSemVersion)
				return;
			ConsoleApp.Log("");
			ConsoleApp.Log($"A new version of docs-builder is available: {latestVersion} currently on version {currentSemVersion}");
			ConsoleApp.Log("");
			ConsoleApp.Log($"	{latestVersionUrl}");
			ConsoleApp.Log("");
			ConsoleApp.Log("Read more about updating here:");
			ConsoleApp.Log("	https://elastic.github.io/docs-builder/contribute/locally.html#step-one	");
			ConsoleApp.Log("");
			return;
		}

		ConsoleApp.LogError($"Unable to parse current version from docs-builder binary");
	}

	private async ValueTask<Uri?> GetLatestVersion(Cancel ctx)
	{
		if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")))
			return null;

		// only check for new versions once per hour
		if (_stateFile.Exists && _stateFile.LastWriteTimeUtc >= DateTime.UtcNow.Subtract(TimeSpan.FromHours(1)))
		{
			var url = await File.ReadAllTextAsync(_stateFile.FullName, ctx);
			if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
				return uri;
		}

		try
		{
			var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
			var response = await httpClient.GetAsync("https://github.com/elastic/docs-builder/releases/latest", ctx);
			var redirectUrl = response.Headers.Location;
			if (redirectUrl is not null && _stateFile.Directory is not null)
			{
				// ensure the 'elastic' folder exists.
				if (!Directory.Exists(_stateFile.Directory.FullName))
					_ = Directory.CreateDirectory(_stateFile.Directory.FullName);
				await File.WriteAllTextAsync(_stateFile.FullName, redirectUrl.ToString(), ctx);
			}
			return redirectUrl;
		}
		// ReSharper disable once RedundantEmptyFinallyBlock
		// ignore on purpose
		finally { }
	}
}
