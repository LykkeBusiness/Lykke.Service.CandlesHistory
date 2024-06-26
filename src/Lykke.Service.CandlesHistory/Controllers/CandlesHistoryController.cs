﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Core.Services.Assets;
using Lykke.Service.CandlesHistory.Core.Services.Candles;
using Lykke.Service.CandlesHistory.Extensions;
using Lykke.Service.CandlesHistory.Models;
using Lykke.Service.CandlesHistory.Models.CandlesHistory;
using Lykke.Service.CandlesHistory.Services.Settings;
using Lykke.Service.CandlesHistory.Utils;
using Microsoft.AspNetCore.Mvc;
using MoreLinq;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lykke.Service.CandlesHistory.Controllers
{
    /// <summary>
    /// A controller for candles history
    /// </summary>
    [Route("api/[controller]")]
    public class CandlesHistoryController : Controller
    {
        private readonly ICandlesManager _candlesManager;
        private readonly IAssetPairsManager _assetPairsManager;
        private readonly DbSettings _dbSettings;
        private readonly ICandlesHistoryRepository _candlesHistoryRepository;

        #region Initialization

        public CandlesHistoryController(
            ICandlesManager candlesManager,
            IAssetPairsManager assetPairsManager,
            DbSettings dbSettings,
            ICandlesHistoryRepository candlesHistoryRepository)
        {
            _candlesManager = candlesManager;
            _assetPairsManager = assetPairsManager;
            _dbSettings = dbSettings;
            _candlesHistoryRepository = candlesHistoryRepository;
        }

        #endregion

        #region Public

        /// <summary>
        /// Pairs for which history can be requested
        /// </summary>
        [HttpGet("availableAssetPairs")]
        [ProducesResponseType(typeof(string[]), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.ServiceUnavailable)]
        public async Task<IActionResult> GetAvailableAssetPairs()
        {
            var assetPairs = await _assetPairsManager.GetAllEnabledAsync();

            return Ok(assetPairs.Select(p => p.Id));
        }

        /// <summary>
        /// Shows history depth limits for all supported asset pairs.
        /// </summary>
        [HttpGet("availableAssetPairs/Depth")]
        [ProducesResponseType(typeof(CandlesHistoryDepthResponseModel[]), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.ServiceUnavailable)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetAvailableAssetPairsHistoryDepth()
        {
            try
            {
                var assetPairs = await _assetPairsManager.GetAllEnabledAsync();

                var result = new List<CandlesHistoryDepthResponseModel>();

                // Now we do select new history depth items in parallel  style for each asset pair,
                // but the depth item constructor itself executes 4 awaitable queries non-parallel.
                // I.e., if we have 10 asset pairs, we get here 10 parallel tasks with 4 sequential
                // data queries in each, but not 10 * 4 parallel tasks.
                // UPD: the batch introduced to not to die with 1500 asset pairs.
                foreach (var batchedAssetPairs in assetPairs.Batch(10))
                {
                    var batchResults = await Task.WhenAll(
                        batchedAssetPairs.Select(async p => new CandlesHistoryDepthResponseModel
                        {
                            AssetPairId = p.Id,
                            OldestAskTimestamp = (await _candlesManager.TryGetOldestCandleAsync(p.Id, CandlePriceType.Ask, CandleTimeInterval.Sec))?.Timestamp,
                            OldestBidTimestamp = (await _candlesManager.TryGetOldestCandleAsync(p.Id, CandlePriceType.Bid, CandleTimeInterval.Sec))?.Timestamp,
                            OldestMidTimestamp = (await _candlesManager.TryGetOldestCandleAsync(p.Id, CandlePriceType.Mid, CandleTimeInterval.Sec))?.Timestamp,
                            OldestTradesTimestamp = (await _candlesManager.TryGetOldestCandleAsync(p.Id, CandlePriceType.Trades, CandleTimeInterval.Sec))?.Timestamp
                        }));
                    result.AddRange(batchResults);
                }

                return Ok(result.ToArray());
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ErrorResponse.Create("Internal", ex.Message));
            }
        }

        /// <summary>
        /// Shows history depth limits for the specified asset pair if it is supported.
        /// </summary>
        [HttpGet("availableAssetPairs/Depth/{assetPairId}")]
        [ProducesResponseType(typeof(CandlesHistoryDepthResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.ServiceUnavailable)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetAssetPairHistoryDepth(string assetPairId)
        {
            if (await _assetPairsManager.TryGetEnabledPairAsync(assetPairId) == null)
            {
                return BadRequest(ErrorResponse.Create(nameof(assetPairId), "Asset pair not found or disabled"));
            }

            var resultTasks = Services.Candles.Constants.StoredPriceTypes
                .Select(pt => _candlesManager.TryGetOldestCandleAsync(assetPairId, pt, CandleTimeInterval.Sec))
                .ToList();

            try
            {
                var results = (await Task.WhenAll(resultTasks)).Where(c => c != null).ToArray();

                return Ok(new CandlesHistoryDepthResponseModel
                {
                    AssetPairId = assetPairId,
                    OldestAskTimestamp = results.FirstOrDefault(c => c.PriceType == CandlePriceType.Ask)?.Timestamp,
                    OldestBidTimestamp = results.FirstOrDefault(c => c.PriceType == CandlePriceType.Bid)?.Timestamp,
                    OldestMidTimestamp = results.FirstOrDefault(c => c.PriceType == CandlePriceType.Mid)?.Timestamp,
                    OldestTradesTimestamp = results.FirstOrDefault(c => c.PriceType == CandlePriceType.Trades)?.Timestamp
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ErrorResponse.Create("AssetPair", ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ErrorResponse.Create("Internal", ex.Message));
            }
        }

        [HttpPost("batch")]
        [ProducesResponseType(typeof(Dictionary<string, CandlesHistoryResponseModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetCandlesHistoryBatch([FromBody] GetCandlesHistoryBatchRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return Ok(new Dictionary<string, CandlesHistoryResponseModel>());
            }

            if (cancellationToken.IsCancellationRequested || request.AssetPairs == null || !request.AssetPairs.Any())
            {
                return Ok(new Dictionary<string, CandlesHistoryResponseModel>());
            }

            request.FromMoment = request.FromMoment.ConvertToUtc();
            request.ToMoment = request.ToMoment.ConvertToUtc();

            if (request.PriceType == CandlePriceType.Unspecified)
            {
                return BadRequest(ErrorResponse.Create(nameof(request.PriceType), $"Price type should not be {CandlePriceType.Unspecified}"));
            }
            if (request.TimeInterval == CandleTimeInterval.Unspecified)
            {
                return BadRequest(ErrorResponse.Create(nameof(request.TimeInterval), $"Time interval should not be {CandleTimeInterval.Unspecified}"));
            }
            if (request.FromMoment > request.ToMoment)
            {
                return BadRequest(ErrorResponse.Create("From date should be early or equal than To date"));
            }

            var enabledPairsTask = request.AssetPairs.Select(p => _assetPairsManager.TryGetEnabledPairAsync(p)).ToArray();
            await Task.WhenAll(enabledPairsTask);

            if (enabledPairsTask.Any(t => t.Result == null))
            {
                var disabled = request.AssetPairs.Except(enabledPairsTask.Select(p => p.Result?.Id).Where(p => p != null));
                return BadRequest(
                    ErrorResponse.Create(nameof(request.AssetPairs),
                        $"Asset pairs [{string.Join(", ", disabled)}] are not found or disabled"));
            }

            var allHistory = new Dictionary<string, CandlesHistoryResponseModel>();
            var tasks = new List<Task<IEnumerable<ICandle>>>();
            foreach (var assetPair in request.AssetPairs)
            {
                allHistory[assetPair] = new CandlesHistoryResponseModel { History = Array.Empty<CandlesHistoryResponseModel.Candle>() };
                tasks.Add(_candlesManager.GetCandlesAsync(assetPair, request.PriceType, request.TimeInterval, request.FromMoment, request.ToMoment));
            }

            await Task.WhenAll(tasks);

            foreach (var task in tasks)
            {
                var candles = task.Result.Select(c => new
                {
                    pair = c.AssetPairId,
                    model = new CandlesHistoryResponseModel.Candle
                    {
                        DateTime = c.Timestamp,
                        Open = c.Open,
                        Close = c.Close,
                        High = c.High,
                        Low = c.Low,
                        TradingVolume = c.TradingVolume,
                        TradingOppositeVolume = c.TradingOppositeVolume,
                        LastTradePrice = c.LastTradePrice,
                        LastUpdateTimestamp = c.LastUpdateTimestamp,
                    }
                }).ToArray();

                if (candles.Any())
                {
                    var p = candles[0].pair;
                    allHistory[p] = new CandlesHistoryResponseModel { History = candles.Select(c => c.model) };
                }
            }
            return Ok(allHistory);
        }

        /// <summary>
        /// Asset's candles history.
        /// May return much less candles than it was requested or even an empty set of data for now the service looks
        /// only through the cache (no persistent data is used).
        /// </summary>
        /// <param name="assetPairId">Asset pair ID</param>
        /// <param name="priceType">Price type</param>
        /// <param name="timeInterval">Time interval</param>
        /// <param name="fromMoment">From moment in ISO 8601 (inclusive)</param>
        /// <param name="toMoment">To moment in ISO 8601 (inclusive)</param>
        [HttpGet("{assetPairId}/{priceType}/{timeInterval}/{fromMoment:datetime}/{toMoment:datetime}")]
        [ProducesResponseType(typeof(CandlesHistoryResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.ServiceUnavailable)]
        public async Task<IActionResult> GetCandlesHistory(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, DateTime fromMoment, DateTime toMoment)
        {
            fromMoment = fromMoment.ConvertToUtc();
            toMoment = toMoment.ConvertToUtc();

            if (string.IsNullOrWhiteSpace(assetPairId))
            {
                return BadRequest(ErrorResponse.Create(nameof(assetPairId), "Asset pair is required"));
            }
            if (priceType == CandlePriceType.Unspecified)
            {
                return BadRequest(ErrorResponse.Create(nameof(priceType), $"Price type should not be {CandlePriceType.Unspecified}"));
            }
            if (timeInterval == CandleTimeInterval.Unspecified)
            {
                return BadRequest(ErrorResponse.Create(nameof(timeInterval), $"Time interval should not be {CandleTimeInterval.Unspecified}"));
            }
            if (fromMoment > toMoment)
            {
                return BadRequest(ErrorResponse.Create("From date should be early or equal than To date"));
            }
            if (await _assetPairsManager.TryGetEnabledPairAsync(assetPairId) == null)
            {
                return BadRequest(ErrorResponse.Create(nameof(assetPairId), "Asset pair not found or disabled"));
            }

            var candles = await _candlesManager.GetCandlesAsync(assetPairId, priceType, timeInterval, fromMoment, toMoment);

            return Ok(new CandlesHistoryResponseModel
            {
                History = candles.Select(c => new CandlesHistoryResponseModel.Candle
                {
                    DateTime = c.Timestamp,
                    Open = c.Open,
                    Close = c.Close,
                    High = c.High,
                    Low = c.Low,
                    TradingVolume = c.TradingVolume,
                    TradingOppositeVolume = c.TradingOppositeVolume,
                    LastTradePrice = c.LastTradePrice,
                    LastUpdateTimestamp = c.LastUpdateTimestamp,
                })
            });
        }

        /// <summary>
        /// Returns the time of the closest available bar in the past if any.
        /// </summary>
        /// <param name="assetPairId">Asset pair ID</param>
        /// <param name="priceType">Price type</param>
        /// <param name="timeInterval">Time interval</param>
        /// <param name="lastMoment">From moment in ISO 8601</param>
        [HttpGet("recentCandleTime/{assetPairId}/{priceType}/{timeInterval}/{lastMoment:datetime}")]
        [ProducesResponseType(typeof(RecentCandleTimeResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.ServiceUnavailable)]
        public async Task<IActionResult> GetRecentCandleTime(string assetPairId, CandlePriceType priceType, CandleTimeInterval timeInterval, DateTime lastMoment)
        {
            lastMoment = lastMoment.ConvertToUtc();

            if (string.IsNullOrWhiteSpace(assetPairId))
            {
                return BadRequest(ErrorResponse.Create(nameof(assetPairId), "Asset pair is required"));
            }
            if (priceType == CandlePriceType.Unspecified)
            {
                return BadRequest(ErrorResponse.Create(nameof(priceType), $"Price type should not be {CandlePriceType.Unspecified}"));
            }
            if (timeInterval == CandleTimeInterval.Unspecified)
            {
                return BadRequest(ErrorResponse.Create(nameof(timeInterval), $"Time interval should not be {CandleTimeInterval.Unspecified}"));
            }
            if (await _assetPairsManager.TryGetEnabledPairAsync(assetPairId) == null)
            {
                return BadRequest(ErrorResponse.Create(nameof(assetPairId), "Asset pair not found or disabled"));
            }

            var recentTime = await _candlesManager.GetRecentCandleTimeAsync(assetPairId, priceType, timeInterval, lastMoment);

            return Ok(new RecentCandleTimeResponseModel
            {
                Exists = recentTime.HasValue,
                ResultTimestamp = recentTime ?? default
            });
        }

        [HttpGet("prices-evolution/{assetPairId}/{priceType}/{date:datetime}")]
        [ProducesResponseType(typeof(IEnumerable<PriceEvolution>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetPricesEvolution([FromRoute] string assetPairId, CandlePriceType priceType, DateTime date)
        {
            date = date == default
                ? DateTime.UtcNow.Date
                : date.ConvertToUtc().Date;

            var tasks = new Dictionary<PriceEvolutionPeriodType, Task<decimal?>>();
            foreach (PriceEvolutionPeriodType period in Enum.GetValues(typeof(PriceEvolutionPeriodType)))
            {
                var startDate = period.GetPriceEvolutionStartDate(date);
                tasks.Add(period, _candlesHistoryRepository.GetPricesEvolution(assetPairId, priceType, startDate));
            }
            await Task.WhenAll(tasks.Values);

            var result = new List<PriceEvolution>();

            tasks.ForEach(async x =>
            {
                var value = await x.Value;
                if (value.HasValue)
                {
                    result.Add(new PriceEvolution
                    {
                        Period = x.Key,
                        EodPrice = value.Value
                    });
                }
            });

            return Ok(result);
        }

        [HttpGet("candles-evolution/{assetPairId}/{priceType}/{date:datetime}")]
        [ProducesResponseType(typeof(IEnumerable<CandleEvolution>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetCandlesEvolution([FromRoute] string assetPairId, CandlePriceType priceType, DateTime date)
        {
            date = date == default
                ? DateTime.UtcNow.Date
                : date.ConvertToUtc().Date;

            var tasks = new Dictionary<CandleEvolutionType, Task<(decimal low, decimal high)?>>();
            foreach (CandleEvolutionType type in Enum.GetValues(typeof(CandleEvolutionType)))
            {
                var startDate = type.GetCandleEvolutionStartDate(date);
                tasks.Add(type, _candlesHistoryRepository.GetCandlesEvolution(assetPairId, priceType, startDate));
            }
            await Task.WhenAll(tasks.Values);

            var result = new List<CandleEvolution>();

            tasks.ForEach(async x =>
            {
                var value = await x.Value;
                if (value.HasValue)
                {
                    result.Add(new CandleEvolution
                    {
                        Type = x.Key,
                        Low = value.Value.low,
                        High = value.Value.high
                    }); 
                }
            });

            return Ok(result);
        }

        #endregion
    }
}
