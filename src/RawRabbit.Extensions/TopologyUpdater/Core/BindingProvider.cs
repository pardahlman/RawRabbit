using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RawRabbit.Configuration;
using RawRabbit.Extensions.TopologyUpdater.Core.Abstraction;
using RawRabbit.Extensions.TopologyUpdater.Model;

namespace RawRabbit.Extensions.TopologyUpdater.Core
{
	public class BindingProvider : IBindingProvider
	{
		private readonly RawRabbitConfiguration _config;
		private readonly string _baseUrl;
		private const string UriEncodedDefaultVhost = "%2f";
		private const string HttpGet = "GET";
		private const string ApplicationJson = "application/json";
		private const string DefaultVhost = "/";
		private const string BindingsPath = "bindings/source";

		public BindingProvider(RawRabbitConfiguration config)
		{
			_config = config;
			var vHost = string.Equals(config.VirtualHost, DefaultVhost)
				? UriEncodedDefaultVhost
				: config.VirtualHost;
			_baseUrl = $"http://{config.Hostnames.FirstOrDefault()}:15672/api/exchanges/{vHost}";
		}

		public Task<List<Binding>> GetBindingsAsync(string exchangeName)
		{
			var requestUrl = $"{_baseUrl}/{exchangeName}/{BindingsPath}";
			var request = CreateRequest(requestUrl);
			return Task.Factory
				.FromAsync(request.BeginGetResponse, request.EndGetResponse, request)
				.ContinueWith(tResponse =>
				{
					var response = (HttpWebResponse)tResponse.Result;
					using (var responseStream = response.GetResponseStream())
					{
						if (responseStream == null)
						{
							throw new ArgumentNullException(nameof(response));
						}

						using (var reader = new StreamReader(responseStream))
						{
							return reader
								.ReadToEndAsync()
								.ContinueWith(tStr =>
								{
									var bindings = JsonConvert.DeserializeObject<List<Binding>>(tStr.Result);
									return bindings;
								});
						}
					}
				})
				.Unwrap();
		}

		private HttpWebRequest CreateRequest(string requestUrl)
		{
			var request = (HttpWebRequest)WebRequest.Create(requestUrl);
			request.Method = HttpGet;
			request.PreAuthenticate = true;
			request.Credentials = new NetworkCredential(_config.Username, _config.Password);
			request.Accept = ApplicationJson;
			return request;
		}
	}
}