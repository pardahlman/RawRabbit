using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.Tests.Common
{
	public class ConfigurationParserTests
	{
		private readonly ConfigurationParser _parser;
		private IConfigurationBuilder _cfgBuilder;

		public ConfigurationParserTests()
		{
			_cfgBuilder = new ConfigurationBuilder()
				.AddInMemoryCollection(new List<KeyValuePair<string, string>>
				{
					new KeyValuePair<string, string>("Data:RawRabbit:Brokers:0:Hostname", "localhost"),
					new KeyValuePair<string, string>("Data:RawRabbit:Brokers:0:Password", "guest"),
					new KeyValuePair<string, string>("Data:RawRabbit:Brokers:0:UserName", "guest"),
					new KeyValuePair<string, string>("Data:RawRabbit:Brokers:0:VirtualHost", "/"),
					new KeyValuePair<string, string>("Data:RawRabbit:Brokers:1:Hostname", "production"),
					new KeyValuePair<string, string>("Data:RawRabbit:Brokers:1:Password", "admin"),
					new KeyValuePair<string, string>("Data:RawRabbit:Brokers:1:UserName", "admin"),
					new KeyValuePair<string, string>("Data:RawRabbit:Brokers:1:VirtualHost", "/prod"),
					new KeyValuePair<string, string>("Data:RawRabbit:Exchange:AutoDelete", "True"),
					new KeyValuePair<string, string>("Data:RawRabbit:Exchange:Durable", "True"),
					new KeyValuePair<string, string>("Data:RawRabbit:Exchange:Type", "Topic"),
					new KeyValuePair<string, string>("Data:RawRabbit:Queue:AutoDelete", "True"),
					new KeyValuePair<string, string>("Data:RawRabbit:Queue:Durable", "True"),
					new KeyValuePair<string, string>("Data:RawRabbit:Queue:Exclusive", "True"),
					new KeyValuePair<string, string>("Data:RawRabbit:RequestTimeout", "00:02:00")
				});
			_parser = new ConfigurationParser();
		}

		[Theory]
		[InlineData("01:02:03", 1,2,3)]
		[InlineData("05:30", 0,5,30)]
		public void Should_Be_Able_To_Parse_Request_Timeout(string configValue, int hours, int minutes, int seconds)
		{
			/* Setup */
			var expected = new TimeSpan(hours, minutes, seconds);
			var cfg = new ConfigurationBuilder()
				.AddInMemoryCollection(new List<KeyValuePair<string, string>>
				{
					new KeyValuePair<string, string>("Data:RawRabbit:RequestTimeout", configValue)
				})
				.Build();

			/* Test */
			var config = _parser.Parse(cfg);

			/* Assert */
			Assert.Equal(config.RequestTimeout, expected);
		}

		[Fact]
		public void Should_Be_Able_To_Parse_Queue_Config()
		{
			/* Setup */
			var cfg = new ConfigurationBuilder()
				.AddInMemoryCollection(new List<KeyValuePair<string, string>>
				{
					new KeyValuePair<string, string>("Data:RawRabbit:Queue:AutoDelete", "True"),
					new KeyValuePair<string, string>("Data:RawRabbit:Queue:Durable", "True"),
					new KeyValuePair<string, string>("Data:RawRabbit:Queue:Exclusive", "True"),
				})
				.Build();

			/* Test */
			var config = _parser.Parse(cfg);

			/* Assert */
			Assert.True(config.Queue.Durable);
			Assert.True(config.Queue.AutoDelete);
			Assert.True(config.Queue.Exclusive);
		}

		[Fact]
		public void Should_Be_Able_To_Parse_Exchange_Config()
		{
			/* Setup */
			var cfg = new ConfigurationBuilder()
				.AddInMemoryCollection(new List<KeyValuePair<string, string>>
				{
					new KeyValuePair<string, string>("Data:RawRabbit:Exchange:AutoDelete", "True"),
					new KeyValuePair<string, string>("Data:RawRabbit:Exchange:Durable", "True"),
					new KeyValuePair<string, string>("Data:RawRabbit:Exchange:Type", "Topic"),
				})
				.Build();

			/* Test */
			var config = _parser.Parse(cfg);

			/* Assert */
			Assert.True(config.Exchange.Durable);
			Assert.True(config.Exchange.AutoDelete);
			Assert.Equal(config.Exchange.Type, ExchangeType.Topic);
		}

		[Fact]
		public void Should_Be_Able_To_Parse_Broker_Config()
		{
			/* Setup */
			var cfg = new ConfigurationBuilder()
				.AddInMemoryCollection(new List<KeyValuePair<string, string>>
				{
					new KeyValuePair<string, string>("Data:RawRabbit:Brokers:0:Hostname", "localhost"),
					new KeyValuePair<string, string>("Data:RawRabbit:Brokers:0:Password", "guest"),
					new KeyValuePair<string, string>("Data:RawRabbit:Brokers:0:UserName", "guest"),
					new KeyValuePair<string, string>("Data:RawRabbit:Brokers:0:Port", "5672"),
					new KeyValuePair<string, string>("Data:RawRabbit:Brokers:0:VirtualHost", "/"),
					
					new KeyValuePair<string, string>("Data:RawRabbit:Brokers:1:Hostname", "production"),
					new KeyValuePair<string, string>("Data:RawRabbit:Brokers:1:VirtualHost", "/"),
					new KeyValuePair<string, string>("Data:RawRabbit:Brokers:1:Password", "admin"),
					new KeyValuePair<string, string>("Data:RawRabbit:Brokers:1:UserName", "admin"),
					new KeyValuePair<string, string>("Data:RawRabbit:Brokers:1:Port", "5672"),
				})
				.Build();

			/* Test */
			var config = _parser.Parse(cfg);

			/* Assert */
			Assert.Equal(config.Brokers[0].Hostname, "localhost");
			Assert.Equal(config.Brokers[0].VirtualHost, "/");
			Assert.Equal(config.Brokers[0].Username, "guest");
			Assert.Equal(config.Brokers[0].Password, "guest");
			Assert.Equal(config.Brokers[0].Port, 5672);
			Assert.Equal(config.Brokers[1].Hostname, "production");
			Assert.Equal(config.Brokers[1].VirtualHost, "/");
			Assert.Equal(config.Brokers[1].Username, "admin");
			Assert.Equal(config.Brokers[1].Password, "admin");
			Assert.Equal(config.Brokers[1].Port, 5672);
		}
	}
}
