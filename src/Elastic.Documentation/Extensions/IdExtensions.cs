// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text;

namespace Elastic.Documentation.Extensions;

public static class ShortId
{
	public static string Create(params string[] components) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("", components))))[..8];
}
