﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.Core.Services.Candles;

namespace Lykke.Service.CandlesHistory.Services
{
    public class StartupManager : IStartupManager
    {
        private readonly ILog _log;
        private readonly ICandlesSubscriber _candlesSubscriber;
        private readonly ISnapshotSerializer _snapshotSerializer;
        private readonly ICandlesCacheSnapshotRepository _candlesCacheSnapshotRepository;
        private readonly ICandlesPersistenceQueueSnapshotRepository _persistenceQueueSnapshotRepository;
        private readonly ICandlesCacheService _candlesCacheService;
        private readonly ICandlesPersistenceQueue _persistenceQueue;
        private readonly ICandlesPersistenceManager _persistenceManager;
        private readonly ICandlesCacheInitalizationService _cacheInitalizationService;

        public StartupManager(
            ILog log, 
            ICandlesCacheInitalizationService cacheInitalizationService,
            ICandlesSubscriber candlesSubscriber,
            ISnapshotSerializer snapshotSerializer,
            ICandlesCacheSnapshotRepository candlesCacheSnapshotRepository,
            ICandlesPersistenceQueueSnapshotRepository persistenceQueueSnapshotRepository,
            ICandlesCacheService candlesCacheService,
            ICandlesPersistenceQueue persistenceQueue,
            ICandlesPersistenceManager persistenceManager)
        {
            _log = log.CreateComponentScope(nameof(StartupManager));
            _candlesSubscriber = candlesSubscriber;
            _snapshotSerializer = snapshotSerializer;
            _candlesCacheSnapshotRepository = candlesCacheSnapshotRepository;
            _persistenceQueueSnapshotRepository = persistenceQueueSnapshotRepository;
            _candlesCacheService = candlesCacheService;
            _persistenceQueue = persistenceQueue;
            _persistenceManager = persistenceManager;
            _cacheInitalizationService = cacheInitalizationService;
        }

        public async Task StartAsync()
        {
            await _log.WriteInfoAsync(nameof(StartAsync), "", "Deserializing persistence queue async...");

            var tasks = new List<Task>
            {
                _snapshotSerializer.DeserializeAsync(_persistenceQueue, _persistenceQueueSnapshotRepository)
            };

            await _log.WriteInfoAsync(nameof(StartAsync), "", "Deserializing cache...");

            if (!await _snapshotSerializer.DeserializeAsync(_candlesCacheService, _candlesCacheSnapshotRepository))
            {
                await _log.WriteInfoAsync(nameof(StartAsync), "", "Initializing cache from the history async...");

                tasks.Add(_cacheInitalizationService.InitializeCacheAsync());
            }

            await _log.WriteInfoAsync(nameof(StartAsync), "", "Waiting for async tasks...");

            await Task.WhenAll(tasks);

            await _log.WriteInfoAsync(nameof(StartAsync), "", "Starting persistence queue...");

            _persistenceQueue.Start();

            await _log.WriteInfoAsync(nameof(StartAsync), "", "Starting persistence manager...");

            _persistenceManager.Start();

            await _log.WriteInfoAsync(nameof(StartAsync), "", "Starting candles subscriber...");

            _candlesSubscriber.Start();

            await _log.WriteInfoAsync(nameof(StartAsync), "", "Started up");
        }
    }
}
