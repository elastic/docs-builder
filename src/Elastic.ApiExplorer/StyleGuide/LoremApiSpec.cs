// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.StyleGuide;

/// <summary>A small made-up API for the style guide's previews: one operation with request and response examples, and two without.</summary>
internal static class LoremApiSpec
{
	public const string FileName = "lorem-api.json";

	public const string Json =
		"""
		{
		  "openapi": "3.0.3",
		  "info": { "title": "Lorem API", "version": "1.0.0", "description": "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua." },
		  "servers": [ { "url": "https://lorem.example.com" } ],
		  "tags": [
		    { "name": "search", "description": "Lorem ipsum search operations." },
		    { "name": "document", "description": "Dolor sit amet document operations." }
		  ],
		  "paths": {
		    "/{index}/_search": {
		      "post": {
		        "tags": [ "search" ],
		        "summary": "Run a search",
		        "operationId": "search",
		        "description": "Returns hits that match the query defined in the request. Ut enim ad minim veniam, quis nostrud exercitation ullamco.",
		        "parameters": [
		          { "name": "index", "in": "path", "required": true, "description": "The index to search.", "schema": { "type": "string" } },
		          { "name": "size", "in": "query", "required": false, "description": "How many hits to return.", "schema": { "type": "integer", "default": 10 } }
		        ],
		        "requestBody": {
		          "content": {
		            "application/json": {
		              "schema": { "$ref": "#/components/schemas/SearchRequest" },
		              "examples": {
		                "simple": { "summary": "A simple match", "value": { "query": { "match": { "title": "lorem" } }, "size": 10 } }
		              }
		            }
		          }
		        },
		        "responses": {
		          "200": {
		            "description": "The matching hits.",
		            "content": {
		              "application/json": {
		                "schema": { "$ref": "#/components/schemas/SearchResponse" },
		                "examples": {
		                  "hits": { "summary": "One hit", "value": { "took": 4, "total": 1, "hits": [ { "id": "1", "title": "Lorem ipsum" } ] } }
		                }
		              }
		            }
		          }
		        }
		      }
		    },
		    "/{index}/_doc/{id}": {
		      "get": {
		        "tags": [ "document" ],
		        "summary": "Get a document",
		        "operationId": "get",
		        "description": "Retrieves a document by its identifier. Duis aute irure dolor in reprehenderit.",
		        "parameters": [
		          { "name": "index", "in": "path", "required": true, "description": "The index the document is in.", "schema": { "type": "string" } },
		          { "name": "id", "in": "path", "required": true, "description": "The document identifier.", "schema": { "type": "string" } }
		        ],
		        "responses": {
		          "200": {
		            "description": "The document.",
		            "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Document" } } }
		          }
		        }
		      }
		    },
		    "/{index}/_count": {
		      "get": {
		        "tags": [ "search" ],
		        "summary": "Count documents",
		        "operationId": "count",
		        "description": "Counts the documents in an index. Excepteur sint occaecat cupidatat non proident.",
		        "parameters": [
		          { "name": "index", "in": "path", "required": true, "description": "The index to count.", "schema": { "type": "string" } }
		        ],
		        "responses": {
		          "200": {
		            "description": "The count.",
		            "content": { "application/json": { "schema": { "type": "object", "properties": { "count": { "type": "integer", "description": "How many documents." } } } } }
		          }
		        }
		      }
		    }
		  },
		  "components": {
		    "schemas": {
		      "SearchRequest": {
		        "type": "object",
		        "properties": {
		          "query": { "type": "object", "description": "The query to run." },
		          "size": { "type": "integer", "description": "How many hits to return." }
		        }
		      },
		      "SearchResponse": {
		        "type": "object",
		        "properties": {
		          "took": { "type": "integer", "description": "How long the search took in milliseconds." },
		          "total": { "type": "integer", "description": "How many hits matched." },
		          "hits": { "type": "array", "items": { "$ref": "#/components/schemas/Document" } }
		        }
		      },
		      "Document": {
		        "type": "object",
		        "properties": {
		          "id": { "type": "string", "description": "The document identifier." },
		          "title": { "type": "string", "description": "The document title." }
		        }
		      }
		    },
		    "securitySchemes": { "apiKey": { "type": "apiKey", "in": "header", "name": "Authorization" } }
		  },
		  "security": [ { "apiKey": [] } ]
		}
		""";
}
