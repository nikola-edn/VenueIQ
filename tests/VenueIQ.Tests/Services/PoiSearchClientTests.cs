using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using VenueIQ.Core.Models;
using VenueIQ.Core.Services;
using Xunit;

namespace VenueIQ.Tests.Services;

public class PoiSearchClientTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }
        public List<Uri> Requests { get; } = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            if (request.RequestUri is not null) Requests.Add(request.RequestUri);
            var json = "{\"results\":[{\"id\":\"1\",\"poi\":{\"name\":\"Cafe\",\"classifications\":[{\"code\":\"POI_CAFE\"}]},\"position\":{\"lat\":44.8,\"lon\":20.5},\"dist\":100.0}]}";
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };
            return Task.FromResult(resp);
        }
    }

    private sealed class FakeAuth : IAzureMapsAuthProvider
    {
        public Task<string> GetSubscriptionKeyAsync(CancellationToken ct = default) => Task.FromResult("test-key");
    }

    private sealed class FakeCat : IPoiCategoryMapProvider
    {
        public Task<(IReadOnlyList<string> competitors, IReadOnlyList<string> complements)> GetCategoriesAsync(BusinessType business, CancellationToken ct = default)
            => Task.FromResult<(IReadOnlyList<string> competitors, IReadOnlyList<string> complements)>((competitors: new[] { "POI_CAFE" }, complements: new[] { "POI_PARKING" }));
    }

    [Fact]
    public async Task BuildsExpectedQuery_AndParsesResults()
    {
        var handler = new FakeHandler();
        var http = new HttpClient(handler);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = new PoiSearchClient(http, new FakeCat(), new FakeAuth(), cache, NullLogger<PoiSearchClient>.Instance);

        var input = new AnalysisInput(BusinessType.Coffee, 44.787, 20.449, 2.0, "sr-Latn");
        var result = await client.SearchAsync(input);

        Assert.True(result.Success);
        Assert.True(result.Meta.CompetitorCount >= 1);
        Assert.NotNull(handler.LastRequestUri);
        var uris = handler.Requests.Select(u => u.ToString()).ToList();
        Assert.Contains(uris, s => s.Contains("categorySet=POI_CAFE"));
        var last = handler.LastRequestUri!.ToString();
        Assert.Contains("language=sr-Latn", last);
        Assert.Contains("lat=44.787", last);
        Assert.Contains("lon=20.449", last);
        Assert.Contains("radius=2000", last);
    }
}

