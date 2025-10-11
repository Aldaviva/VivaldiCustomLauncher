using System.Net.Http.Headers;
using Unfucked;
using Unfucked.HTTP.Filters;

namespace VivaldiCustomLauncher.BuildTrigger;

public class GitHubApiFilter(string accessToken): ClientRequestFilter {

    private const string DOMAIN_LOCK   = "api.github.com";
    private const string VERSION_KEY   = "X-GitHub-Api-Version";
    private const string VERSION_VALUE = "2022-11-28"; // https://docs.github.com/en/rest/about-the-rest-api/api-versions?apiVersion=2022-11-28#supported-api-versions

    private static readonly MediaTypeWithQualityHeaderValue ACCEPT = new("application/vnd.github+json");

    private readonly AuthenticationHeaderValue authentication = new("Bearer", accessToken);

    public Task<HttpRequestMessage> Filter(HttpRequestMessage request, FilterContext _, CancellationToken cancellationToken) {
        if (request.RequestUri?.BelongsToDomain(DOMAIN_LOCK) ?? false) {
            request.Headers.Authorization = authentication;
            request.Headers.Accept.Add(ACCEPT);
            request.Headers.Add(VERSION_KEY, VERSION_VALUE);
        }
        return Task.FromResult(request);
    }

}