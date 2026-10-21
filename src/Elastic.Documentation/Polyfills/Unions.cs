// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#if !NET11_0_OR_GREATER
// The polyfill must live in the BCL namespace the compiler looks up, not the folder's namespace.
#pragma warning disable IDE0130
namespace System.Runtime.CompilerServices;

#pragma warning restore IDE0130

/// <summary>
/// Polyfill for the .NET 11 union marker, so C# <c>union</c> declarations compile on <c>net10.0</c>
/// (the SDK's compiler supports unions under <c>LangVersion=preview</c>; only these BCL types are missing).
/// Public so assemblies that consume the domain's unions resolve the same types. Removed once the domain targets net11.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class UnionAttribute : Attribute;

/// <summary>Polyfill for the .NET 11 union interface: the boxed value of the current case, or <c>null</c> for <c>default</c>.</summary>
public interface IUnion
{
	object? Value { get; }
}
#endif
