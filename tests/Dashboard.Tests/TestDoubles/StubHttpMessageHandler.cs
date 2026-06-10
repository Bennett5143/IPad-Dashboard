using System.Net;

namespace Dashboard.Tests.TestDoubles;

/// <summary>
/// Beantwortet HTTP-Anfragen anhand einer übergebenen Funktion – erlaubt das Testen
/// typed <see cref="HttpClient"/>s ohne echten Netzwerkverkehr.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        _responder = responder;

    public static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
    };

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(_responder(request));
}
