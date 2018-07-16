﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.SettingsReader;

namespace Lykke.Service.CandleHistory.Repositories.Candles
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class SqlCandlesHistoryRepository : ICandlesHistoryRepository
    {
        private readonly ILog _log;
        private readonly IReloadingManager<Dictionary<string, string>> _assetConnectionStrings;
        private readonly string _sqlConnectionString;

        private readonly ConcurrentDictionary<string, SqlAssetPairCandlesHistoryRepository> _sqlAssetPairRepositories;

        public SqlCandlesHistoryRepository(ILog log, IReloadingManager<Dictionary<string, string>> assetConnectionStrings, string sqlServerConnectionString)
        {
            _log = log;
            _assetConnectionStrings = assetConnectionStrings;
            _sqlConnectionString = sqlServerConnectionString;

            _sqlAssetPairRepositories = new ConcurrentDictionary<string, SqlAssetPairCandlesHistoryRepository>();
        }

        public async Task<IEnumerable<ICandle>> GetCandlesAsync(string assetPairId, CandleTimeInterval interval,
            CandlePriceType priceType, DateTime from, DateTime to)
        {
            var repo = GetRepo(assetPairId);
            try
            {
                return await repo.GetCandlesAsync(priceType, interval, from, to);
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync("get candle rows with retries failed", assetPairId, ex);
                throw;
            }
        }

        public bool CanStoreAssetPair(string assetPairId)
        {
            return _assetConnectionStrings.CurrentValue.ContainsKey(assetPairId);
        }

        public async Task<ICandle> TryGetFirstCandleAsync(string assetPairId, CandleTimeInterval interval, CandlePriceType priceType)
        {
            var repo = GetRepo(assetPairId);
            try
            {
                return await repo.TryGetFirstCandleAsync(priceType, interval);
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync("get first candle row with retries failed", assetPairId, ex);
                throw;
            }
        }

        private SqlAssetPairCandlesHistoryRepository GetRepo(string assetPairId)
        {
            var key = assetPairId;

            if (!_sqlAssetPairRepositories.TryGetValue(key, out SqlAssetPairCandlesHistoryRepository repo) || repo == null)
            {
                repo = new SqlAssetPairCandlesHistoryRepository(assetPairId, _sqlConnectionString, _log);
                _sqlAssetPairRepositories.TryAdd(key, repo);
            }

            return repo;
        }

    }
}
