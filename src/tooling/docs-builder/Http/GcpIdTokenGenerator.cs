// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Documentation.Builder.Http;

internal readonly record struct ServiceAccountKey(
	string Type,
	string ProjectId,
	string PrivateKeyId,
	string PrivateKey,
	string ClientEmail,
	string ClientId,
	string AuthUri,
	string TokenUri,
	string AuthProviderX509CertUrl,
	string ClientX509CertUrl
);

internal readonly record struct JwtHeader(string Alg, string Typ, string Kid);

internal readonly record struct JwtPayload(
	string Iss,
	string Sub,
	string Aud,
	long Iat,
	long Exp,
	string TargetAudience
);

[JsonSerializable(typeof(ServiceAccountKey))]
[JsonSerializable(typeof(JwtPayload))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
internal sealed partial class GcpJsonContext : JsonSerializerContext { }

[JsonSerializable(typeof(JwtHeader))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class JwtHeaderJsonContext : JsonSerializerContext { }

// This is a custom implementation to create an ID token for GCP.
// Because Google.Api.Auth.OAuth2 is not compatible with AOT
public static class GcpIdTokenGenerator
{

	public static async Task<string> GenerateIdTokenAsync(string serviceAccountKeyPath, string targetAudience, CancellationToken cancellationToken = default)
	{
		// Read and parse service account key file using System.Text.Json source generation (AOT compatible)
		var serviceAccountJson = await File.ReadAllTextAsync(serviceAccountKeyPath, cancellationToken);
		var serviceAccount = JsonSerializer.Deserialize(serviceAccountJson, GcpJsonContext.Default.ServiceAccountKey);

		// Create JWT header
		var header = new JwtHeader("RS256", "JWT", serviceAccount.PrivateKeyId);
		var headerJson = JsonSerializer.Serialize(header, JwtHeaderJsonContext.Default.JwtHeader);
		var headerBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));

		// Create JWT payload
		var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		var payload = new JwtPayload(
			serviceAccount.ClientEmail,
			serviceAccount.ClientEmail,
			"https://oauth2.googleapis.com/token",
			now,
			now + 3600, // 1 hour expiration
			targetAudience
		);

		var payloadJson = JsonSerializer.Serialize(payload, GcpJsonContext.Default.JwtPayload);
		var payloadBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

		// Create signature
		var message = $"{headerBase64}.{payloadBase64}";
		var messageBytes = Encoding.UTF8.GetBytes(message);

		// Parse the private key (removing PEM headers/footers and decoding)
		var privateKeyPem = serviceAccount.PrivateKey
			.Replace("-----BEGIN PRIVATE KEY-----", "")
			.Replace("-----END PRIVATE KEY-----", "")
			.Replace("\n", "")
			.Replace("\r", "");
		var privateKeyBytes = Convert.FromBase64String(privateKeyPem);

		// Create RSA instance and sign
		using var rsa = RSA.Create();
		rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
		var signature = rsa.SignData(messageBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		var signatureBase64 = Base64UrlEncode(signature);

		var jwt = $"{message}.{signatureBase64}";

		// Exchange JWT for ID token
		return await ExchangeJwtForIdToken(jwt, targetAudience, cancellationToken);
	}

	private static async Task<string> ExchangeJwtForIdToken(string jwt, string targetAudience, CancellationToken cancellationToken)
	{
		using var httpClient = new HttpClient();

		var requestContent = new FormUrlEncodedContent([
			new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
			new KeyValuePair<string, string>("assertion", jwt),
			new KeyValuePair<string, string>("target_audience", targetAudience)
		]);

		var response = await httpClient.PostAsync("https://oauth2.googleapis.com/token", requestContent, cancellationToken);
		_ = response.EnsureSuccessStatusCode();

		var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
		using var document = JsonDocument.Parse(responseJson);

		if (document.RootElement.TryGetProperty("id_token", out var idTokenElement))
		{
			return idTokenElement.GetString() ?? throw new InvalidOperationException("ID token is null");
		}

		throw new InvalidOperationException("No id_token found in response");
	}

	private static string Base64UrlEncode(byte[] input)
	{
		var base64 = Convert.ToBase64String(input);
		// Convert base64 to base64url encoding
		return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
	}
}
