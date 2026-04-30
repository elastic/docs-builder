// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Reflection;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Microsoft.Extensions.Logging;
using Nullean.Argh.Middleware;

namespace Documentation.Builder.Middleware;

internal sealed class CheckForUpdatesMiddleware(ILogger<CheckForUpdatesMiddleware> logger) : ICommandMiddleware
{
	// Only accesses ApplicationData — no workspace access needed
	private static readonly IFileSystem Fs = FileSystemFactory.AppData;
	private readonly IFileInfo _stateFile = Fs.FileInfo.New(Path.Join(Paths.ApplicationData.FullName, "docs-build-check.state"));
	private readonly ILogger<CheckForUpdatesMiddleware> _logger = logger;

	public async ValueTask InvokeAsync(CommandContext context, CommandMiddlewareDelegate next)
	{
		await next(context);
		if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")))
			return;

		var latestVersionUrl = await GetLatestVersion(context.CancellationToken);
		if (latestVersionUrl is null)
			_logger.LogWarning("Unable to determine latest version");
		else
			CompareWithAssemblyVersion(latestVersionUrl);
	}

	private void CompareWithAssemblyVersion(Uri latestVersionUrl)
	{
		var versionPath = latestVersionUrl.AbsolutePath.Split('/').Last();
		if (!SemVersion.TryParse(versionPath, out var latestVersion))
		{
			_logger.LogWarning("Unable to parse latest version from {LatestVersionUrl}", latestVersionUrl);
			return;
		}

		var assemblyVersion = Assembly.GetExecutingAssembly()
			.GetCustomAttributes<AssemblyInformationalVersionAttribute>()
			.FirstOrDefault()?.InformationalVersion;

		if (!SemVersion.TryParse(assemblyVersion ?? "", out var currentSemVersion))
		{
			_logger.LogWarning("Unable to parse current version from docs-builder binary");
			return;
		}

		var currentVersion = new SemVersion(currentSemVersion.Major, currentSemVersion.Minor, currentSemVersion.Patch);
		if (latestVersion <= currentVersion)
			return;

		_logger.LogInformation("");
		_logger.LogInformation("A new version of docs-builder is available: {Latest} (currently on {Current})", latestVersion, currentSemVersion);
		_logger.LogInformation("  {LatestVersionUrl}", latestVersionUrl);
		_logger.LogInformation("Read more about updating: https://elastic.github.io/docs-builder/contribute/locally#step-one");
	}

	private async ValueTask<Uri?> GetLatestVersion(CancellationToken ct)
	{
		// only check for new versions once per hour
		if (_stateFile.Exists && _stateFile.LastWriteTimeUtc >= DateTime.UtcNow.Subtract(TimeSpan.FromHours(1)))
		{
			var url = await Fs.File.ReadAllTextAsync(_stateFile.FullName, ct);
			if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
				return uri;
		}

		try
		{
			var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
			var response = await httpClient.GetAsync("https://github.com/elastic/docs-builder/releases/latest", ct);
			var redirectUrl = response.Headers.Location;
			if (redirectUrl is not null && _stateFile.Directory is not null)
			{
				if (!Fs.Directory.Exists(_stateFile.Directory.FullName))
					_ = Fs.Directory.CreateDirectory(_stateFile.Directory.FullName);
				await Fs.File.WriteAllTextAsync(_stateFile.FullName, redirectUrl.ToString(), ct);
			}
			return redirectUrl;
		}
		finally { }
	}
}
