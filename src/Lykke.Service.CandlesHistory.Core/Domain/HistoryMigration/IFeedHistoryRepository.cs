﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Domain.Prices;

namespace Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration
{
    public interface IFeedHistoryRepository
    {
        Task<IFeedHistory> GetTopRecordAsync(string assetPair);
        Task<IFeedHistory> GetCandle(string assetPair, PriceType priceType, string date);
        Task GetCandlesByChunkAsync(string assetPair, PriceType priceType, DateTime startDate, DateTime endDate, Func<IEnumerable<IFeedHistory>, PriceType, Task> chunkCallback);
    }
}
