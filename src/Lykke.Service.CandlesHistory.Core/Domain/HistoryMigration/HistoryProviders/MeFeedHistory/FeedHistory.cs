﻿using System;
using Lykke.Domain.Prices;

namespace Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration.HistoryProviders.MeFeedHistory
{
    public class FeedHistory : IFeedHistory
    {
        public string AssetPair { get; private set; }
        public PriceType PriceType { get; private set; }
        public DateTime DateTime { get; private set; }
        public FeedHistoryItem[] Candles { get; private set; }

        public static IFeedHistory Create(IFeedHistory item)
        {
            return new FeedHistory
            {
                AssetPair = item.AssetPair,
                PriceType = item.PriceType,
                DateTime = item.DateTime,
                Candles = item.Candles
            };
        }
    }
}
