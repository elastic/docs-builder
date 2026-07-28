// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Contract;

#pragma warning disable IDE0130 // 'Query' subfolder would shadow the ES client's Query type
namespace Elastic.Documentation.Search;

/// <summary>Canonical Elasticsearch field names used by the shared query builder.</summary>
public static class QueryFieldNames
{
	private static DocumentationMappingContext.DocumentationDocumentResolver Doc { get; } = DocumentationMappingContext.DocumentationDocument;

	public static string ContentType { get; } = Doc.Fields.ContentType;
	public static string Section { get; } = Doc.Fields.Section;
	public static string NavigationDepth { get; } = $"{Doc.Fields.Navigation}.depth";
	public static string NavigationTableOfContents { get; } = $"{Doc.Fields.Navigation}.table_of_contents";
	public static string PathKeyword { get; } = $"{Doc.Fields.Path}.keyword";
	public static string PathMatch { get; } = $"{Doc.Fields.Path}.match";
	public static string TitleKeyword { get; } = $"{Doc.Fields.Title}.keyword";
	public static string TitleStartsWith { get; } = $"{Doc.Fields.Title}.starts_with";
	public static string Title { get; } = Doc.Fields.Title;
	public static string SearchTitle { get; } = Doc.Fields.SearchTitle;
	public static string SearchTitleCompletion { get; } = $"{Doc.Fields.SearchTitle}.completion";
	public static string SearchTitleCompletion2Gram { get; } = $"{Doc.Fields.SearchTitle}.completion._2gram";
	public static string SearchTitleCompletion3Gram { get; } = $"{Doc.Fields.SearchTitle}.completion._3gram";
	public static string Body { get; } = Doc.Fields.Body;
	public static string Hidden { get; } = Doc.Fields.Hidden;
	public static string LastUpdated { get; } = Doc.Fields.LastUpdated;
	public static string TitleSemanticText { get; } = $"{Doc.Fields.Title}.semantic_text";
	public static string SummarySemanticText { get; } = $"{Doc.Fields.Summary}.semantic_text";
	public static string AiRagSummarySemanticText { get; } = $"{Doc.Fields.AiRagOptimizedSummary}.semantic_text";
	public static string AiQuestionsSemanticText { get; } = $"{Doc.Fields.AiQuestions}.semantic_text";
	public static string RelatedProductsId { get; } = $"{Doc.Fields.RelatedProducts}.id";
}
