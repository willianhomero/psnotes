using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace PSNotes.Services
{
    public static class GitHubAuthenticationExtensions
    {
        public static void AddGitHub(this AuthenticationBuilder app, string clientId, string clientSecret, string signInScheme)
        {
            app.AddOAuth(Constants.GitHubAuthenticationScheme, "GitHub", o =>
            {
                o.ClientId = clientId;
                o.ClientSecret = clientSecret;
                o.CallbackPath = new PathString("/signin-github");
                o.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                o.TokenEndpoint = "https://github.com/login/oauth/access_token";
                o.UserInformationEndpoint = "https://api.github.com/user";
                o.ClaimsIssuer = "OAuth2-Github";
                o.SaveTokens = true;
                o.SignInScheme = signInScheme;

                // Retrieving user information is unique to each provider.
                o.Events = new OAuthEvents
                {
                    OnCreatingTicket = async context => { await CreatingGitHubAuthTicket(context); },
                };
            });
        }

        private static async Task CreatingGitHubAuthTicket(OAuthCreatingTicketContext context)
        {
            // Get the GitHub user
            var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
            response.EnsureSuccessStatusCode();

            var user = JObject.Parse(await response.Content.ReadAsStringAsync());

            AddClaims(context, user);
        }

        private static void AddClaims(OAuthCreatingTicketContext context, JObject user)
        {
            var identifier = user.Value<string>("id");
            if (!string.IsNullOrEmpty(identifier))
            {
                context.Identity.AddClaim(new Claim(
                    ClaimTypes.NameIdentifier, identifier,
                    ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }

            var userName = user.Value<string>("login");
            if (!string.IsNullOrEmpty(userName))
            {
                context.Identity.AddClaim(new Claim(
                    ClaimsIdentity.DefaultNameClaimType, userName,
                    ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }

            var name = user.Value<string>("name");
            if (!string.IsNullOrEmpty(name))
            {
                context.Identity.AddClaim(new Claim(
                    "urn:github:name", name,
                    ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }

            var link = user.Value<string>("url");
            if (!string.IsNullOrEmpty(link))
            {
                context.Identity.AddClaim(new Claim(
                    "urn:github:url", link,
                    ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }
        }
    }
}
