using System;
using System.Collections.Generic;
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

public class PoiSearchClientCacheTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var json = "{\"results\":[{\"id\":\"1\",\"poi\":{\"name\":\"Cafe\",\"classifications\":[{\"code\":\"CAFE_PUB\"}]},\"position\":{\"lat\":44.8,\"lon\":20.5},\"dist\":100.0}]}";
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
            => Task.FromResult<(IReadOnlyList<string> competitors, IReadOnlyList<string> complements)>((competitors: new[] { "CAFE_PUB" }, complements: new[] { "OPEN_PARKING_AREA" }));
    }

    [Fact]
    public async Task SearchAsync_UsesCache_OnSecondCall()
    {
        var handler = new FakeHandler();
        var http = new HttpClient(handler);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = new PoiSearchClient(http, new FakeCat(), new FakeAuth(), cache, NullLogger<PoiSearchClient>.Instance);

        var input = new AnalysisInput(BusinessType.Coffee, 44.787, 20.449, 2.0, "sr-Latn");
        var r1 = await client.SearchAsync(input);
        var callsAfterFirst = handler.Calls;
        var r2 = await client.SearchAsync(input);

        Assert.True(r1.Success);
        Assert.True(r2.Success);
        Assert.Equal(callsAfterFirst, handler.Calls); // no additional HTTP calls on cached request
    }
}
