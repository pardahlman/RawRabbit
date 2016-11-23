using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RawRabbit.Configuration;
using RawRabbit.Extensions.TopologyUpdater.Core.Abstraction;
using RawRabbit.Extensions.TopologyUpdater.Model;

namespace RawRabbit.Extensions.TopologyUpdater.Core
{
    public class BindingProvider : IBindingProvider
    {
        private readonly HttpClient _httpClient;
        private const string UriEncodedDefaultVhost = "%2f";
        private const string DefaultVhost = "/";
        private const string BindingsPath = "bindings/source";

        public BindingProvider(RawRabbitConfiguration config)
        {
            var vHost = string.Equals(config.VirtualHost, DefaultVhost)
                    ? UriEncodedDefaultVhost
                    : config.VirtualHost;

            _httpClient = new HttpClient(new HttpClientHandler
            {
                Credentials = new NetworkCredential(config.Username, config.Password)
            })
            {
                BaseAddress = new Uri($"http://{config.Hostnames.FirstOrDefault()}:15672/api/exchanges/{vHost}/")
            };
        }

        public Task<List<Binding>> GetBindingsAsync(string exchangeName)
        {
            return _httpClient
                .GetAsync($"{exchangeName}/{BindingsPath}")
                .ContinueWith(async tResponse =>
                {
                    if (!tResponse.IsCompleted || !tResponse.Result.IsSuccessStatusCode)
                    {
                        return new List<Binding>();
                    }

                    var responseStr = await tResponse.Result.Content.ReadAsStringAsync();
                    var bindings = JsonConvert.DeserializeObject<List<Binding>>(responseStr);
                    return bindings;
                })
                .Unwrap();
        }
    }
}