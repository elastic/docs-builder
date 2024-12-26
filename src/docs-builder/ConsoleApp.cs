// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;

// this is a temporary workaround for https://github.com/Cysharp/ConsoleAppFramework/issues/154
// it generates code that is not AOT safe, but it's in a code path we don't use.
// therefor I am comfortable running with this band-aid for now

// ReSharper disable once CheckNamespace
namespace ConsoleAppFramework;

[UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode", Justification = "Manually verified")]
[UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL3050:RequiresDynamicCode", Justification = "Manually verified")]
internal static partial class ConsoleApp;
