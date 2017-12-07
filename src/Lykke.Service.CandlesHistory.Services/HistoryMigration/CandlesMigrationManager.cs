﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Core.Services.HistoryMigration;
using Lykke.Service.CandlesHistory.Core.Services.HistoryMigration.HistoryProviders;
using Lykke.Service.CandlesHistory.Services.HistoryMigration.HistoryProviders.MeFeedHistory;
using Lykke.Service.CandlesHistory.Services.HistoryMigration.Telemetry;
using Lykke.Service.CandlesHistory.Services.Settings;

namespace Lykke.Service.CandlesHistory.Services.HistoryMigration
{
    [UsedImplicitly]
    public class CandlesMigrationManager : IDisposable
    {
        public IReadOnlyDictionary<string, AssetPairMigrationTelemetryService> Health => _assetHealthServices;

        private readonly IHealthService _healthService;
        private readonly MigrationCandlesGenerator _candlesGenerator;
        private readonly IMissedCandlesGenerator _missedCandlesGenerator;
        private readonly ICandlesHistoryMigrationService _candlesHistoryMigrationService;
        private readonly ICandlesPersistenceQueue _candlesPersistenceQueue;
        private readonly IAssetPairsManager _assetPairsManager;
        private readonly ICandlesHistoryRepository _candlesHistoryRepository;
        private readonly ILog _log;
        private readonly Dictionary<string, AssetPairMigrationManager> _assetManagers;
        private readonly Dictionary<string, AssetPairMigrationTelemetryService> _assetHealthServices;
        private readonly MigrationSettings _settings;

        public CandlesMigrationManager(
            IHealthService healthService,
            MigrationCandlesGenerator candlesGenerator,
            IMissedCandlesGenerator missedCandlesGenerator,
            ICandlesHistoryMigrationService candlesHistoryMigrationService, 
            ICandlesPersistenceQueue candlesPersistenceQueue,
            IAssetPairsManager assetPairsManager,
            ICandlesHistoryRepository candlesHistoryRepository,
            ILog log, 
            MigrationSettings settings)
        {
            _candlesGenerator = candlesGenerator;
            _missedCandlesGenerator = missedCandlesGenerator;
            _candlesHistoryMigrationService = candlesHistoryMigrationService;
            _candlesPersistenceQueue = candlesPersistenceQueue;
            _assetPairsManager = assetPairsManager;
            _candlesHistoryRepository = candlesHistoryRepository;
            _log = log;
            _settings = settings;
            _healthService = healthService;

            _assetManagers = new Dictionary<string, AssetPairMigrationManager>();
            _assetHealthServices = new Dictionary<string, AssetPairMigrationTelemetryService>();
        }

        public async Task<string> MigrateAsync(string assetPairId, IHistoryProvider historyProvider)
        {
            if (!_candlesHistoryRepository.CanStoreAssetPair(assetPairId))
            {
                return $"Connection string for the asset pair '{assetPairId}' not configuer";
            }

            var assetPair = await _assetPairsManager.TryGetAssetPairAsync(assetPairId);

            if (assetPair == null)
            {
                return $"Asset pair '{assetPairId}' not found";
            }

            lock (_assetManagers)
            {
                if (_assetManagers.ContainsKey(assetPairId))
                {
                    return $"{assetPairId} already being processed";
                }

                var telemetryService = new AssetPairMigrationTelemetryService(_log, assetPairId);
                var assetManager = new AssetPairMigrationManager(
                    _healthService,
                    _candlesPersistenceQueue,
                    _candlesGenerator,
                    telemetryService,
                    assetPair, 
                    _log,
                    new BidAskHCacheService(),
                    historyProvider,
                    _candlesHistoryMigrationService,
                    OnMigrationStopped,
                    _settings);

                assetManager.Start();

                _assetHealthServices.Add(assetPairId, telemetryService);
                _assetManagers.Add(assetPairId, assetManager);
                
                 return $"{assetPairId} processing is started";
            }
        }

        private void OnMigrationStopped(string assetPair)
        {
            lock (_assetManagers)
            {
                _assetManagers.Remove(assetPair);
                _candlesGenerator.RemoveAssetPair(assetPair);
                _missedCandlesGenerator.RemoveAssetPair(assetPair);
            }
        }

        public void Stop()
        {
            lock (_assetManagers)
            {
                foreach (var pair in _assetManagers.Keys)
                    _assetManagers[pair].Stop();
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
