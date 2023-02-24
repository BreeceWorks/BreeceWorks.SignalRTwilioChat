using IdentityModel.Client;

namespace BreeceWorks.TwilioSignalRApp.Services
{
    public class TokenManager
    {
        private readonly TokenProvider _tokenProvider;
        private readonly IHttpClientFactory _httpClientFactory;

        public TokenManager(TokenProvider tokenProvider,
            IHttpClientFactory httpClientFactory)
        {
            _tokenProvider = tokenProvider ??
                throw new ArgumentNullException(nameof(tokenProvider));
            _httpClientFactory = httpClientFactory ??
                throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task<string> RetrieveAccessTokenAsync()
        {
            // should we refresh? 

            if ((_tokenProvider.ExpiresAt.AddSeconds(-60)).ToUniversalTime()
                    > DateTime.UtcNow)
            {
                // no need to refresh, return the access token
                return _tokenProvider.AccessToken;
            }

            // refresh
            var idpClient = _httpClientFactory.CreateClient();

            var discoveryReponse = await idpClient
                .GetDiscoveryDocumentAsync("https://demo.duendesoftware.com");

            var refreshResponse = await idpClient.RequestRefreshTokenAsync(
               new RefreshTokenRequest
               {
                   Address = discoveryReponse.TokenEndpoint,
                   ClientId = "interactive.confidential",
                   ClientSecret = "secret",
                   RefreshToken = _tokenProvider.RefreshToken
               });

            _tokenProvider.AccessToken = refreshResponse.AccessToken;
            _tokenProvider.RefreshToken = refreshResponse.RefreshToken;
            _tokenProvider.ExpiresAt = DateTime.UtcNow.AddSeconds(refreshResponse.ExpiresIn);

            return _tokenProvider.AccessToken;
        }
    }
}
