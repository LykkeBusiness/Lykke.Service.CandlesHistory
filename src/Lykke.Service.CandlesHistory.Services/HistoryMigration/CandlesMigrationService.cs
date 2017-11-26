﻿using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.HistoryMigration;

namespace Lykke.Service.CandlesHistory.Services.HistoryMigration
{
    [UsedImplicitly]
    public class CandlesesHistoryMigrationService : ICandlesHistoryMigrationService
    {
        private readonly ICandlesHistoryRepository _candlesHistoryRepository;

        public CandlesesHistoryMigrationService(ICandlesHistoryRepository candlesHistoryRepository)
        {
            _candlesHistoryRepository = candlesHistoryRepository;
        }
        
        public async Task<ICandle> GetFirstCandleOfHistoryAsync(string assetPair, CandlePriceType priceType)
        {
            var candle = await _candlesHistoryRepository.TryGetFirstCandleAsync(assetPair, CandleTimeInterval.Sec, priceType);

            return candle;
        }
    }
}
