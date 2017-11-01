﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Domain.Prices;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Services.Settings;
using Newtonsoft.Json;

namespace Lykke.Service.CandlesHistory.Services.Candles
{
    public class CandlesSubscriber : ICandlesSubscriber
    {
        private class CandleMessage : ICandle
        {
            [JsonProperty("a")]
            public string AssetPairId { get; set; }

            [JsonProperty("p")]
            public PriceType PriceType { get; set; }

            [JsonProperty("i")]
            public TimeInterval TimeInterval { get; set; }

            [JsonProperty("t")]
            public DateTime Timestamp { get; set; }

            [JsonProperty("o")]
            public double Open { get; set; }

            [JsonProperty("c")]
            public double Close { get; set; }

            [JsonProperty("h")]
            public double High { get; set; }

            [JsonProperty("l")]
            public double Low { get; set; }

            public string Tag => "Candles Producer";
        }

        private readonly ILog _log;
        private readonly ICandlesManager _candlesManager;
        private readonly RabbitEndpointSettings _settings;

        private RabbitMqSubscriber<CandleMessage> _subscriber;

        public CandlesSubscriber(ILog log, ICandlesManager candlesManager, RabbitEndpointSettings settings)
        {
            _log = log;
            _candlesManager = candlesManager;
            _settings = settings;
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .CreateForSubscriber(_settings.ConnectionString, _settings.Namespace, "candles", _settings.Namespace, "candleshistory")
                .MakeDurable();

            try
            {
                _subscriber = new RabbitMqSubscriber<CandleMessage>(settings,
                        new ResilientErrorHandlingStrategy(_log, settings,
                            retryTimeout: TimeSpan.FromSeconds(10),
                            retryNum: 10,
                            next: new DeadQueueErrorHandlingStrategy(_log, settings)))
                    .SetMessageDeserializer(new JsonMessageDeserializer<CandleMessage>())
                    .SetMessageReadStrategy(new MessageReadQueueStrategy())
                    .Subscribe(ProcessCandleAsync)
                    .CreateDefaultBinding()
                    .SetLogger(_log)
                    .Start();
            }
            catch (Exception ex)
            {
                _log.WriteErrorAsync(nameof(CandlesSubscriber), nameof(Start), null, ex).Wait();
                throw;
            }
        }

        public void Stop()
        {
            _subscriber?.Stop();
        }

        private async Task ProcessCandleAsync(CandleMessage candle)
        {
            try
            {
                var validationErrors = ValidateQuote(candle);
                if (validationErrors.Any())
                {
                    var message = string.Join("\r\n", validationErrors);
                    await _log.WriteWarningAsync(nameof(CandlesSubscriber), nameof(ProcessCandleAsync), candle.ToJson(), message);

                    return;
                }

                _candlesManager.ProcessCandle(candle);
            }
            catch (Exception)
            {
                await _log.WriteWarningAsync(nameof(CandlesSubscriber), nameof(ProcessCandleAsync), candle.ToJson(), "Failed to process candle");
                throw;
            }
        }

        private static IReadOnlyCollection<string> ValidateQuote(CandleMessage candle)
        {
            var errors = new List<string>();

            if (candle == null)
            {
                errors.Add("Argument 'Order' is null.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(candle.AssetPairId))
                {
                    errors.Add("Empty 'AssetPair'");
                }
                if (candle.Timestamp.Kind != DateTimeKind.Utc)
                {
                    errors.Add($"Invalid 'Timestamp' Kind (UTC is required): '{candle.Timestamp.Kind}'");
                }
            }

            return errors;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
