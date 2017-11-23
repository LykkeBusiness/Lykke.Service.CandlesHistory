﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using JetBrains.Annotations;
using Lykke.Domain.Prices;
using Lykke.Service.CandlesHistory.Core.Domain.HistoryMigration.HistoryProviders.MeFeedHistory;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.CandleHistory.Repositories.HistoryMigration.HistoryProviders.MeFeedHistory
{
    [UsedImplicitly]
    public class FeedHistoryRepository : IFeedHistoryRepository
    {
        private readonly INoSQLTableStorage<FeedHistoryEntity> _tableStorage;

        public FeedHistoryRepository(INoSQLTableStorage<FeedHistoryEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IFeedHistory> GetTopRecordAsync(string assetPair, PriceType priceType)
        {
            var entity = await _tableStorage.GetTopRecordAsync($"{assetPair}_{priceType}");

            return FeedHistory.Create(entity);
        }

        public Task GetCandlesByChunksAsync(string assetPair, PriceType priceType, DateTime endDate, Func<IEnumerable<IFeedHistory>, Task> readChunkFunc)
        {
            var partition = FeedHistoryEntity.GeneratePartitionKey(assetPair, priceType);
            var filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partition);
            var tableQuery = new TableQuery<FeedHistoryEntity>().Where(filter);
            var feedEndDate = endDate.RoundToMinute();

            return _tableStorage.GetDataByChunksAsync(tableQuery, async chunk =>
            {
                var yieldResult = new List<IFeedHistory>();

                foreach (var historyItem in chunk.Where(item => item.DateTime <= feedEndDate))
                {
                    yieldResult.Add(FeedHistory.Create(historyItem));
                }

                if (yieldResult.Count > 0)
                {
                    await readChunkFunc(yieldResult);
                }
            });
        }
    }
}
