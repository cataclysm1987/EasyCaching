﻿namespace EasyCaching.UnitTests
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using EasyCaching.ResponseCaching;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.AspNetCore.ResponseCaching;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Net.Http.Headers;
    using Xunit;

    public class ResponseCachingTests
    {
        [Theory]
        [InlineData("GET")]
        //[InlineData("HEAD")]
        public async void ServesCachedContent_IfAvailable(string method)
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching();

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.SendAsync(ResponseCachingTestUtils.CreateRequest(method, ""));
                    var subsequentResponse = await client.SendAsync(ResponseCachingTestUtils.CreateRequest(method, ""));

                    await AssertCachedResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Theory]
        [InlineData("GET")]
        //[InlineData("HEAD")]
        public async void ServesFreshContent_IfNotAvailable(string method)
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching();

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.SendAsync(ResponseCachingTestUtils.CreateRequest(method, ""));
                    var subsequentResponse = await client.SendAsync(ResponseCachingTestUtils.CreateRequest(method, "different"));

                    await AssertFreshResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Fact]
        public async void ServesFreshContent_Post()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching();

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.PostAsync("", new StringContent(string.Empty));
                    var subsequentResponse = await client.PostAsync("", new StringContent(string.Empty));

                    await AssertFreshResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        //[Fact]
        public async void ServesFreshContent_Head_Get()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching();

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var subsequentResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, ""));
                    var initialResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, ""));

                    await AssertFreshResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        //[Fact]
        public async void ServesFreshContent_Get_Head()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching();

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, ""));
                    var subsequentResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, ""));

                    await AssertFreshResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Theory]
        [InlineData("GET")]
        //[InlineData("HEAD")]
        public async void ServesFreshContent_If_CacheControlNoCache(string method)
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching();

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();

                    var initialResponse = await client.SendAsync(ResponseCachingTestUtils.CreateRequest(method, ""));

                    // verify the response is cached
                    var cachedResponse = await client.SendAsync(ResponseCachingTestUtils.CreateRequest(method, ""));
                    await AssertCachedResponseAsync(initialResponse, cachedResponse);

                    // assert cached response no longer served
                    client.DefaultRequestHeaders.CacheControl =
                        new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
                    var subsequentResponse = await client.SendAsync(ResponseCachingTestUtils.CreateRequest(method, ""));

                    await AssertFreshResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Theory]
        [InlineData("GET")]
        //[InlineData("HEAD")]
        public async void ServesFreshContent_If_PragmaNoCache(string method)
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching();

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();

                    var initialResponse = await client.SendAsync(ResponseCachingTestUtils.CreateRequest(method, ""));

                    // verify the response is cached
                    var cachedResponse = await client.SendAsync(ResponseCachingTestUtils.CreateRequest(method, ""));
                    await AssertCachedResponseAsync(initialResponse, cachedResponse);

                    // assert cached response no longer served
                    client.DefaultRequestHeaders.Pragma.Clear();
                    client.DefaultRequestHeaders.Pragma.Add(new System.Net.Http.Headers.NameValueHeaderValue("no-cache"));
                    var subsequentResponse = await client.SendAsync(ResponseCachingTestUtils.CreateRequest(method, ""));

                    await AssertFreshResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Theory]
        [InlineData("GET")]
        //[InlineData("HEAD")]
        public async void ServesCachedContent_If_PathCasingDiffers(string method)
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching();

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.SendAsync(ResponseCachingTestUtils.CreateRequest(method, "path"));
                    var subsequentResponse = await client.SendAsync(ResponseCachingTestUtils.CreateRequest(method, "PATH"));

                    await AssertCachedResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Theory]
        [InlineData("GET")]
        //[InlineData("HEAD")]
        public async void ServesFreshContent_If_ResponseExpired(string method)
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching();

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.SendAsync(ResponseCachingTestUtils.CreateRequest(method, "?Expires=0"));
                    var subsequentResponse = await client.SendAsync(ResponseCachingTestUtils.CreateRequest(method, ""));

                    await AssertFreshResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        public async void ServesFreshContent_If_Authorization_HeaderExists(string method)
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching();

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("abc");
                    var initialResponse = await client.SendAsync(ResponseCachingTestUtils.CreateRequest(method, ""));
                    var subsequentResponse = await client.SendAsync(ResponseCachingTestUtils.CreateRequest(method, ""));

                    await AssertFreshResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        //[Fact]
        public async void ServesCachedContent_IfVaryHeader_Matches()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(contextAction: context => context.Response.Headers[HeaderNames.Vary] = HeaderNames.From);

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    client.DefaultRequestHeaders.From = "user@example.com";
                    var initialResponse = await client.GetAsync("");
                    var subsequentResponse = await client.GetAsync("");

                    await AssertCachedResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Fact]
        public async void ServesFreshContent_IfVaryHeader_Mismatches()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(contextAction: context => context.Response.Headers[HeaderNames.Vary] = HeaderNames.From);

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    client.DefaultRequestHeaders.From = "user@example.com";
                    var initialResponse = await client.GetAsync("");
                    client.DefaultRequestHeaders.From = "user2@example.com";
                    var subsequentResponse = await client.GetAsync("");

                    await AssertFreshResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        //[Fact]
        public async void ServesCachedContent_IfVaryQueryKeys_Matches()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(contextAction: context => context.Features.Get<Microsoft.AspNetCore.ResponseCaching.IResponseCachingFeature>().VaryByQueryKeys = new[] { "query" });

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("?query=value");
                    var subsequentResponse = await client.GetAsync("?query=value");

                    await AssertCachedResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        //[Fact]
        public async void ServesCachedContent_IfVaryQueryKeysExplicit_Matches_QueryKeyCaseInsensitive()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(contextAction: context => context.Features.Get<Microsoft.AspNetCore.ResponseCaching.IResponseCachingFeature>().VaryByQueryKeys = new[] { "QueryA", "queryb" });

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("?querya=valuea&queryb=valueb");
                    var subsequentResponse = await client.GetAsync("?QueryA=valuea&QueryB=valueb");

                    await AssertCachedResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        //[Fact]
        public async void ServesCachedContent_IfVaryQueryKeyStar_Matches_QueryKeyCaseInsensitive()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(contextAction: context => context.Features.Get<IResponseCachingFeature>().VaryByQueryKeys = new[] { "*" });

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("?querya=valuea&queryb=valueb");
                    var subsequentResponse = await client.GetAsync("?QueryA=valuea&QueryB=valueb");

                    await AssertCachedResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        //[Fact]
        public async void ServesCachedContent_IfVaryQueryKeyExplicit_Matches_OrderInsensitive()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(contextAction: context => context.Features.Get<IResponseCachingFeature>().VaryByQueryKeys = new[] { "QueryB", "QueryA" });

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("?QueryA=ValueA&QueryB=ValueB");
                    var subsequentResponse = await client.GetAsync("?QueryB=ValueB&QueryA=ValueA");

                    await AssertCachedResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        //[Fact]
        public async void ServesCachedContent_IfVaryQueryKeyStar_Matches_OrderInsensitive()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(contextAction: context => context.Features.Get<IResponseCachingFeature>().VaryByQueryKeys = new[] { "*" });

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("?QueryA=ValueA&QueryB=ValueB");
                    var subsequentResponse = await client.GetAsync("?QueryB=ValueB&QueryA=ValueA");

                    await AssertCachedResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Fact]
        public async void ServesFreshContent_IfVaryQueryKey_Mismatches()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(contextAction: context => context.Features.Get<IResponseCachingFeature>().VaryByQueryKeys = new[] { "query" });

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("?query=value");
                    var subsequentResponse = await client.GetAsync("?query=value2");

                    await AssertFreshResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Fact]
        public async void ServesFreshContent_IfVaryQueryKeyExplicit_Mismatch_QueryKeyCaseSensitive()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(contextAction: context => context.Features.Get<IResponseCachingFeature>().VaryByQueryKeys = new[] { "QueryA", "QueryB" });

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("?querya=valuea&queryb=valueb");
                    var subsequentResponse = await client.GetAsync("?querya=ValueA&queryb=ValueB");

                    await AssertFreshResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Fact]
        public async void ServesFreshContent_IfVaryQueryKeyStar_Mismatch_QueryKeyValueCaseSensitive()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(contextAction: context => context.Features.Get<IResponseCachingFeature>().VaryByQueryKeys = new[] { "*" });

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("?querya=valuea&queryb=valueb");
                    var subsequentResponse = await client.GetAsync("?querya=ValueA&queryb=ValueB");

                    await AssertFreshResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Fact]
        public async void ServesFreshContent_IfRequestRequirements_NotMet()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching();

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("");
                    client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue()
                    {
                        MaxAge = TimeSpan.FromSeconds(0)
                    };
                    var subsequentResponse = await client.GetAsync("");

                    await AssertFreshResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Fact]
        public async void Serves504_IfOnlyIfCachedHeader_IsSpecified()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching();

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("");
                    client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue()
                    {
                        OnlyIfCached = true
                    };
                    var subsequentResponse = await client.GetAsync("/different");

                    initialResponse.EnsureSuccessStatusCode();
                    Assert.Equal(System.Net.HttpStatusCode.GatewayTimeout, subsequentResponse.StatusCode);
                }
            }
        }

        [Fact]
        public async void ServesFreshContent_IfSetCookie_IsSpecified()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(contextAction: context => context.Response.Headers[HeaderNames.SetCookie] = "cookieName=cookieValue");

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("");
                    var subsequentResponse = await client.GetAsync("");

                    await AssertFreshResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Fact]
        public async void ServesCachedContent_IfIHttpSendFileFeature_NotUsed()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(app =>
            {
                app.Use(async (context, next) =>
                {
                    context.Features.Set<IHttpSendFileFeature>(new DummySendFileFeature());
                    await next.Invoke();
                });
            });

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("");
                    var subsequentResponse = await client.GetAsync("");

                    await AssertCachedResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Fact]
        public async void ServesFreshContent_IfIHttpSendFileFeature_Used()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(
                app =>
                {
                    app.Use(async (context, next) =>
                    {
                        context.Features.Set<IHttpSendFileFeature>(new DummySendFileFeature());
                        await next.Invoke();
                    });
                },
                contextAction: async context => await context.Features.Get<IHttpSendFileFeature>().SendFileAsync("dummy", 0, 0, CancellationToken.None));

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("");
                    var subsequentResponse = await client.GetAsync("");

                    await AssertFreshResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Fact]
        public async void ServesCachedContent_IfSubsequentRequestContainsNoStore()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching();

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("");
                    client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue()
                    {
                        NoStore = true
                    };
                    var subsequentResponse = await client.GetAsync("");

                    await AssertCachedResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Fact]
        public async void ServesFreshContent_IfInitialRequestContainsNoStore()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching();

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue()
                    {
                        NoStore = true
                    };
                    var initialResponse = await client.GetAsync("");
                    var subsequentResponse = await client.GetAsync("");

                    await AssertFreshResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Fact]
        public async void ServesFreshContent_IfInitialResponseContainsNoStore()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(contextAction: context => context.Response.Headers[HeaderNames.CacheControl] = CacheControlHeaderValue.NoStoreString);

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("");
                    var subsequentResponse = await client.GetAsync("");

                    await AssertFreshResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Fact]
        public async void Serves304_IfIfModifiedSince_Satisfied()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching();

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("");
                    client.DefaultRequestHeaders.IfModifiedSince = DateTimeOffset.MaxValue;
                    var subsequentResponse = await client.GetAsync("");

                    initialResponse.EnsureSuccessStatusCode();
                    Assert.Equal(System.Net.HttpStatusCode.NotModified, subsequentResponse.StatusCode);
                }
            }
        }

        [Fact]
        public async void ServesCachedContent_IfIfModifiedSince_NotSatisfied()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching();

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("");
                    client.DefaultRequestHeaders.IfModifiedSince = DateTimeOffset.MinValue;
                    var subsequentResponse = await client.GetAsync("");

                    await AssertCachedResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Fact]
        public async void Serves304_IfIfNoneMatch_Satisfied()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(contextAction: context => context.Response.GetTypedHeaders().ETag = new EntityTagHeaderValue("\"E1\""));

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("");
                    client.DefaultRequestHeaders.IfNoneMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue("\"E1\""));
                    var subsequentResponse = await client.GetAsync("");

                    initialResponse.EnsureSuccessStatusCode();
                    Assert.Equal(System.Net.HttpStatusCode.NotModified, subsequentResponse.StatusCode);
                }
            }
        }

        [Fact]
        public async void ServesCachedContent_IfIfNoneMatch_NotSatisfied()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(contextAction: context => context.Response.GetTypedHeaders().ETag = new EntityTagHeaderValue("\"E1\""));

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("");
                    client.DefaultRequestHeaders.IfNoneMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue("\"E2\""));
                    var subsequentResponse = await client.GetAsync("");

                    await AssertCachedResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Fact]
        public async void ServesCachedContent_IfBodySize_IsCacheable()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(options: new EasyCachingResponseOptions()
            {
                MaximumBodySize = 100
            });

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("");
                    var subsequentResponse = await client.GetAsync("");

                    await AssertCachedResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Fact]
        public async void ServesFreshContent_IfBodySize_IsNotCacheable()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(options: new EasyCachingResponseOptions()
            {
                MaximumBodySize = 1
            });

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("");
                    var subsequentResponse = await client.GetAsync("/different");

                    await AssertFreshResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        //[Fact]
        public async void ServesFreshContent_CaseSensitivePaths_IsNotCacheable()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(options: new EasyCachingResponseOptions()
            {
                UseCaseSensitivePaths = true
            });

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var initialResponse = await client.GetAsync("/path");
                    var subsequentResponse = await client.GetAsync("/Path");

                    await AssertFreshResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        //[Fact]
        public async void ServesCachedContent_WithoutReplacingCachedVaryBy_OnCacheMiss()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(contextAction: context => context.Response.Headers[HeaderNames.Vary] = HeaderNames.From);

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    client.DefaultRequestHeaders.From = "user@example.com";
                    var initialResponse = await client.GetAsync("");
                    client.DefaultRequestHeaders.From = "user2@example.com";
                    var otherResponse = await client.GetAsync("");
                    client.DefaultRequestHeaders.From = "user@example.com";
                    var subsequentResponse = await client.GetAsync("");

                    await AssertCachedResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        [Fact]
        public async void ServesFreshContent_IfCachedVaryByUpdated_OnCacheMiss()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(contextAction: context => context.Response.Headers[HeaderNames.Vary] = context.Request.Headers[HeaderNames.Pragma]);

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    client.DefaultRequestHeaders.From = "user@example.com";
                    client.DefaultRequestHeaders.Pragma.Clear();
                    client.DefaultRequestHeaders.Pragma.Add(new System.Net.Http.Headers.NameValueHeaderValue("From"));
                    client.DefaultRequestHeaders.MaxForwards = 1;
                    var initialResponse = await client.GetAsync("");
                    client.DefaultRequestHeaders.From = "user2@example.com";
                    client.DefaultRequestHeaders.Pragma.Clear();
                    client.DefaultRequestHeaders.Pragma.Add(new System.Net.Http.Headers.NameValueHeaderValue("Max-Forwards"));
                    client.DefaultRequestHeaders.MaxForwards = 2;
                    var otherResponse = await client.GetAsync("");
                    client.DefaultRequestHeaders.From = "user@example.com";
                    client.DefaultRequestHeaders.Pragma.Clear();
                    client.DefaultRequestHeaders.Pragma.Add(new System.Net.Http.Headers.NameValueHeaderValue("From"));
                    client.DefaultRequestHeaders.MaxForwards = 1;
                    var subsequentResponse = await client.GetAsync("");

                    await AssertFreshResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        //[Fact]
        public async void ServesCachedContent_IfCachedVaryByNotUpdated_OnCacheMiss()
        {
            var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(contextAction: context => context.Response.Headers[HeaderNames.Vary] = context.Request.Headers[HeaderNames.Pragma]);

            foreach (var builder in builders)
            {
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    client.DefaultRequestHeaders.From = "user@example.com";
                    client.DefaultRequestHeaders.Pragma.Clear();
                    client.DefaultRequestHeaders.Pragma.Add(new System.Net.Http.Headers.NameValueHeaderValue("From"));
                    client.DefaultRequestHeaders.MaxForwards = 1;
                    var initialResponse = await client.GetAsync("");
                    client.DefaultRequestHeaders.From = "user2@example.com";
                    client.DefaultRequestHeaders.Pragma.Clear();
                    client.DefaultRequestHeaders.Pragma.Add(new System.Net.Http.Headers.NameValueHeaderValue("From"));
                    client.DefaultRequestHeaders.MaxForwards = 2;
                    var otherResponse = await client.GetAsync("");
                    client.DefaultRequestHeaders.From = "user@example.com";
                    client.DefaultRequestHeaders.Pragma.Clear();
                    client.DefaultRequestHeaders.Pragma.Add(new System.Net.Http.Headers.NameValueHeaderValue("From"));
                    client.DefaultRequestHeaders.MaxForwards = 1;
                    var subsequentResponse = await client.GetAsync("");

                    await AssertCachedResponseAsync(initialResponse, subsequentResponse);
                }
            }
        }

        private static async Task AssertCachedResponseAsync(HttpResponseMessage initialResponse, HttpResponseMessage subsequentResponse)
        {
            initialResponse.EnsureSuccessStatusCode();
            subsequentResponse.EnsureSuccessStatusCode();

            foreach (var header in initialResponse.Headers)
            {
                Assert.Equal(initialResponse.Headers.GetValues(header.Key), subsequentResponse.Headers.GetValues(header.Key));
            }
            Assert.True(subsequentResponse.Headers.Contains(HeaderNames.Age));
            Assert.Equal(await initialResponse.Content.ReadAsStringAsync(), await subsequentResponse.Content.ReadAsStringAsync());
        }

        private static async Task AssertFreshResponseAsync(HttpResponseMessage initialResponse, HttpResponseMessage subsequentResponse)
        {
            initialResponse.EnsureSuccessStatusCode();
            subsequentResponse.EnsureSuccessStatusCode();

            Assert.False(subsequentResponse.Headers.Contains(HeaderNames.Age));

            if (initialResponse.RequestMessage.Method == HttpMethod.Head &&
                subsequentResponse.RequestMessage.Method == HttpMethod.Head)
            {
                Assert.True(initialResponse.Headers.Contains("X-Value"));
                Assert.NotEqual(initialResponse.Headers.GetValues("X-Value"), subsequentResponse.Headers.GetValues("X-Value"));
            }
            else
            {
                Assert.NotEqual(await initialResponse.Content.ReadAsStringAsync(), await subsequentResponse.Content.ReadAsStringAsync());
            }
        }

    }
}
