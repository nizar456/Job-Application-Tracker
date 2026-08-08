using System.Net.Http.Headers;

namespace Frontend.Services;

public sealed class AccessTokenHandler(TokenStore tokenStore) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (tokenStore.Token is { } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}