﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration;
using Lykke.Service.CandlesHistory.Core.Extensions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.CandleHistory.Repositories.HistoryMigration
{
    public class FeedHistoryEntity : ITableEntity, IFeedHistory
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string ETag { get; set; }

        public FeedHistoryItem[] Candles { get; private set; }

        public string AssetPair => PartitionKey.Split('_')[0];
        public DateTime DateTime
        {
            get
            {
                if (!string.IsNullOrEmpty(RowKey))
                {
                    return ParseRowKey(RowKey, DateTimeKind.Utc);
                }
                return default(DateTime);
            }
        }

        public PriceType PriceType
        {
            get
            {
                if (!string.IsNullOrEmpty(PartitionKey))
                {
                    var value = PartitionKey.Split('_')[1];

                    if (Enum.TryParse(value, out PriceType priceType))
                    {
                        return priceType;
                    }
                }
                return PriceType.Unspecified;
            }
        }

        public static string GeneratePartitionKey(string assetPair, PriceType priceType)
        {
            return $"{assetPair}_{priceType.ToString()}";
        }

        private readonly Regex _regExp = new Regex("O=(.*);C=(.*);H=(.*);L=(.*);T=(.*)", RegexOptions.Compiled);

        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            if (properties.TryGetValue("Data", out var property))
            {
                var result = new List<FeedHistoryItem>();
                var candles = property.StringValue.Split('|', StringSplitOptions.RemoveEmptyEntries);
                foreach (var candle in candles)
                {
                    var match = _regExp.Match(candle);
                    if (match.Groups.Count == 6)
                    {
                        result.Add(new FeedHistoryItem
                        {
                            Open = Convert.ToDouble(match.Groups[1].Value, CultureInfo.InvariantCulture),
                            Close = Convert.ToDouble(match.Groups[2].Value, CultureInfo.InvariantCulture),
                            High = Convert.ToDouble(match.Groups[3].Value, CultureInfo.InvariantCulture),
                            Low = Convert.ToDouble(match.Groups[4].Value, CultureInfo.InvariantCulture),
                            Tick = Convert.ToInt32(match.Groups[5].Value)
                        });
                    }
                }

                Candles = result.OrderBy(c => c.Tick).ToArray();
            }
        }

        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var dict = new Dictionary<string, EntityProperty>
            {
                {"Data", new EntityProperty(Candles.ToFeedHistoryData())}
            };

            return dict;
        }

        private static DateTime ParseRowKey(string value, DateTimeKind kind)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            return DateTime.SpecifyKind(DateTime.ParseExact(value, "yyyyMMddHHmm", DateTimeFormatInfo.InvariantInfo), kind);
        }
    }
}
