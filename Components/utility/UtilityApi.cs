using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;

namespace EEE.Components.Utility;

internal static class UtilityApi
{
    private static readonly CookieContainer Cookies = new();
    private static readonly HttpClient HttpClient = new(new HttpClientHandler
    {
        CookieContainer = Cookies,
        UseCookies = true,
    });
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static Uri CreateUri(string baseUrl, string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute)
            && (absolute.Scheme == Uri.UriSchemeHttps || absolute.Scheme == Uri.UriSchemeHttp))
            return absolute;

        if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp))
            throw new InvalidOperationException("Set ApiBaseUrl to an absolute HTTP(S) URL before calling the API.");

        return new Uri(baseUri, path.TrimStart('/'));
    }

    public static async Task<HttpResponseMessage> SendAsync<T>(
        HttpMethod method,
        Uri uri,
        T? body,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, uri);
        if (body is not null)
            request.Content = JsonContent.Create(body);

        var response = await HttpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
            return response;

        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = string.IsNullOrWhiteSpace(detail) ? response.ReasonPhrase : detail;
        var statusCode = response.StatusCode;
        response.Dispose();
        throw new HttpRequestException($"API request failed ({(int)statusCode}): {message}", null, statusCode);
    }

    public static async Task<HttpResponseMessage> SendFileAsync(
        HttpMethod method,
        Uri uri,
        FileResult file,
        IReadOnlyDictionary<string, string> fields,
        CancellationToken cancellationToken = default)
    {
        await using var fileStream = await file.OpenReadAsync();
        using var content = new MultipartFormDataContent();
        foreach (var field in fields)
            content.Add(new StringContent(field.Value), field.Key);

        var fileContent = new StreamContent(fileStream);
        if (MediaTypeHeaderValue.TryParse(file.ContentType, out var mediaType))
            fileContent.Headers.ContentType = mediaType;
        content.Add(fileContent, "file", file.FileName);
        using var request = new HttpRequestMessage(method, uri) { Content = content };
        var response = await HttpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
            return response;

        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = string.IsNullOrWhiteSpace(detail) ? response.ReasonPhrase : detail;
        var statusCode = response.StatusCode;
        response.Dispose();
        throw new HttpRequestException($"API request failed ({(int)statusCode}): {message}", null, statusCode);
    }

    public static async Task<CommerceChatUser> SignInAsync(string baseUrl, string email, string password, CancellationToken cancellationToken = default)
    {
        var csrfUri = CreateUri(baseUrl, "api/auth/csrf");
        using var csrfResponse = await HttpClient.GetAsync(csrfUri, cancellationToken);
        csrfResponse.EnsureSuccessStatusCode();
        var csrf = await csrfResponse.Content.ReadFromJsonAsync<CsrfResponse>(JsonOptions, cancellationToken)
            ?? throw new JsonException("The sign-in service did not return a CSRF token.");

        var callbackUri = CreateUri(baseUrl, "api/auth/callback/credentials");
        using var callbackContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["csrfToken"] = csrf.CsrfToken,
            ["email"] = email,
            ["password"] = password,
            ["callbackUrl"] = baseUrl,
            ["json"] = "true"
        });
        using var callbackRequest = new HttpRequestMessage(HttpMethod.Post, callbackUri) { Content = callbackContent };
        callbackRequest.Headers.Referrer = CreateUri(baseUrl, string.Empty);
        callbackRequest.Headers.Add("X-Auth-Return-Redirect", "1");
        using var callbackResponse = await HttpClient.SendAsync(callbackRequest, cancellationToken);
        if (!callbackResponse.IsSuccessStatusCode)
        {
            var detail = await callbackResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Sign-in failed ({(int)callbackResponse.StatusCode}): {detail}", null, callbackResponse.StatusCode);
        }

        var sessionUri = CreateUri(baseUrl, "api/auth/session");
        using var sessionResponse = await HttpClient.GetAsync(sessionUri, cancellationToken);
        if (!sessionResponse.IsSuccessStatusCode)
            throw new HttpRequestException($"Unable to load the signed-in session ({(int)sessionResponse.StatusCode}).", null, sessionResponse.StatusCode);
        var session = await sessionResponse.Content.ReadFromJsonAsync<AuthSession>(JsonOptions, cancellationToken)
            ?? throw new JsonException("The sign-in service returned an invalid session.");
        if (string.IsNullOrWhiteSpace(session.User?.Id))
            throw new HttpRequestException("The credentials were not accepted by the sign-in service.");
        return session.User;
    }

    private sealed class CsrfResponse
    {
        public string CsrfToken { get; set; } = string.Empty;
    }

    private sealed class AuthSession
    {
        public CommerceChatUser? User { get; set; }
    }
}

public sealed class AccountUpgradeRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = "pending";
    public string ProfessionalType { get; set; } = string.Empty;
    public string RegNumber { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public string FacilityAddress { get; set; } = string.Empty;
    public string FacilityRegNumber { get; set; } = string.Empty;
}

public sealed class ShippingAddress
{
    public string UserId { get; set; } = string.Empty;
    public string Country { get; set; } = "Nigeria";
    public string State { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public sealed class AdminUserRecord
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "customer";
    public string VerificationStatus { get; set; } = "unverified";
    public string AvatarUrl { get; set; } = string.Empty;
    public string ProfessionalType { get; set; } = string.Empty;
    public string RegNumber { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public string FacilityRegNumber { get; set; } = string.Empty;
}

public sealed class BulkImportResult
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public List<string> Errors { get; set; } = [];
}

public sealed class AdvertText
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool Active { get; set; } = true;
}
