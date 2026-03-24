// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site;

/// <summary>Provides HTMX attributes for links and layout. Implementations customize swap behavior per build type.</summary>
public interface IHtmxAttributeProvider
{
	/// <summary>Gets the root path for HTMX navigation (e.g. data-root-path).</summary>
	string GetRootPath();

	/// <summary>Gets the hx-select-oob value based on whether the target is in the same top-level group.</summary>
	string GetHxSelectOob(bool hasSameTopLevelGroup);

	/// <summary>Gets HTMX attributes for a link.</summary>
	string GetHxAttributes(bool hasSameTopLevelGroup = false, string? preload = "mousedown", string? hxSwapOob = null);

	/// <summary>Gets HTMX attributes for navigation links.</summary>
	string GetNavHxAttributes(bool hasSameTopLevelGroup = false, string? preload = "mousedown");
}
