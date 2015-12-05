using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RawRabbit.Extensions.CleanEverything.Http
{
	public class WebRequester
	{
		private Func<HttpWebRequest> _requestInit;
		private Action<HttpWebRequest> _methodModifier;
		private Action<HttpWebRequest> _credentialModifier;

		public WebRequester WithUrl(string fullUrl)
		{
			_requestInit = () => (HttpWebRequest)WebRequest.Create(fullUrl);
			return this;
		}

		public WebRequester WithMethod(HttpMethod method)
		{
			_methodModifier = req => req.Method = method.ToString().ToUpper();
			return this;
		}

		public WebRequester WithCredentials(ICredentials credential)
		{
			_credentialModifier = req => req.Credentials = credential;
			return this;
		}

		public Task<TPayload> PerformAsync<TPayload>()
		{
			return GetResponseAsync()
				.ContinueWith(t => ReadResponseStreamAsync(t.Result)).Unwrap()
				.ContinueWith(t => JsonConvert.DeserializeObject<TPayload>(t.Result));
		}

		public Task<HttpWebResponse> GetResponseAsync()
		{
			var rawRequest = PrepareRequest();
			return Task.Factory
				.FromAsync(rawRequest.BeginGetResponse, rawRequest.EndGetResponse, rawRequest)
				.ContinueWith(t => (HttpWebResponse)t.Result);
		}

		private async Task<string> ReadResponseStreamAsync(HttpWebResponse response)
		{
			using (var responseStream = response.GetResponseStream())
			{
				if (responseStream == null)
				{
					throw new ArgumentNullException(nameof(response));
				}

				using (var reader = new StreamReader(responseStream))
				{
					return await reader.ReadToEndAsync();
				}
			}
		}

		private HttpWebRequest PrepareRequest()
		{
			var req = _requestInit();
			req.PreAuthenticate = true;
			req.Accept = ContentType.ApplicationJson;
			_credentialModifier(req);
			_methodModifier(req);
			req.UserAgent = "RawRabbit";
			return req;
		}
	}
}
