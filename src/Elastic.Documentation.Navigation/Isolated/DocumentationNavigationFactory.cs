// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Navigation.Isolated.Leaf;
using Elastic.Documentation.Navigation.Isolated.Node;

namespace Elastic.Documentation.Navigation.Isolated;

/// <summary>
/// Factory interface for creating documentation file models from file system paths.
/// </summary>
/// <typeparam name="TModel">The type of documentation file to create</typeparam>
public interface IDocumentationFileFactory<out TModel> where TModel : IDocumentationFile
{
	/// <summary>
	/// Attempts to create a documentation file model from the given file path.
	/// </summary>
	/// <param name="path">The file path to create a model for</param>
	/// <param name="readFileSystem">The file system to read from</param>
	/// <returns>A documentation file model, or null if creation failed</returns>
	TModel? TryCreateDocumentationFile(IFileInfo path, IFileSystem readFileSystem);
}

/// <summary>
/// Factory for creating navigation items from documentation files.
/// </summary>
public static class DocumentationNavigationFactory
{
	/// <summary>
	/// Creates a file navigation leaf from a documentation file model.
	/// </summary>
	public static ILeafNavigationItem<TModel> CreateFileNavigationLeaf<TModel>(TModel model, IFileInfo fileInfo, FileNavigationArgs args)
		where TModel : IDocumentationFile =>
		new FileNavigationLeaf<TModel>(model, fileInfo, args) { NavigationIndex = args.NavigationIndex };

	/// <summary>
	/// Creates a virtual file navigation node from a documentation file model.
	/// </summary>
	public static VirtualFileNavigation<TModel> CreateVirtualFileNavigation<TModel>(TModel model, IFileInfo fileInfo, VirtualFileNavigationArgs args)
		where TModel : IDocumentationFile =>
		new(model, fileInfo, args) { NavigationIndex = args.NavigationIndex };
}
