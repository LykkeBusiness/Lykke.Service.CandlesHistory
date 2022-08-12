// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Common.Log;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Common;
using Dapper;
using JetBrains.Annotations;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Logs.MsSql.Extensions;


namespace Lykke.Service.CandleHistory.Repositories.Candles
{
    public class SqlAssetPairCandlesHistoryRepository
    {
        private const int commandTimeout = 150;

        private const string CreateTableScript = "CREATE TABLE {0}(" +
                                                 "[Id] [bigint] NOT NULL IDENTITY(1,1) PRIMARY KEY," +
                                                 "[AssetPairId] [nvarchar] (64) NOT NULL, " +
                                                 "[PriceType] [int] NOT NULL ," +
                                                 "[Open] [float] NOT NULL, " +
                                                 "[Close] [float] NOT NULL, " +
                                                 "[High] [float] NOT NULL, " +
                                                 "[Low] [float] NOT NULL, " +
                                                 "[TimeInterval] [int] NOT NULL, " +
                                                 "[TradingVolume] [float] NOT NULL, " +
                                                 "[TradingOppositeVolume] [float] NOT NULL, " +
                                                 "[LastTradePrice] [float] NOT NULL, " +
                                                 "[Timestamp] [datetime] NULL, " +
                                                 "[LastUpdateTimestamp] [datetime] NULL" +
                                                 ",INDEX IX_UNIQUEINDEX UNIQUE NONCLUSTERED (Timestamp, PriceType, TimeInterval));"; 

        private readonly string _tableName;
        private readonly string _connectionString;

        public SqlAssetPairCandlesHistoryRepository(string assetName, string connectionString, ILog log)
        {
            _connectionString = connectionString;
            const string schemaName = "Candles";
            var fixedAssetName = assetName.Replace("-", "_");
            var justTableName = $"candleshistory_{fixedAssetName}";
            _tableName = $"[{schemaName}].[{justTableName}]";
            var createTableScript = CreateTableScript.Replace("UNIQUEINDEX", fixedAssetName);

            using (var conn = new SqlConnection(_connectionString))
            {
                try { conn.CreateTableIfDoesntExists(createTableScript, justTableName, schemaName); }
                catch (Exception ex)
                {
                    log?.WriteErrorAsync(nameof(SqlAssetPairCandlesHistoryRepository),
                        "CreateTableIfDoesntExists",
                        new {createTableScript, justTableName, schemaName}.ToJson(),
                        ex);
                    throw;
                }
            }

            log?.WriteInfoAsync(nameof(SqlAssetPairCandlesHistoryRepository),
                nameof(SqlAssetPairCandlesHistoryRepository), 
                $"New table has been created successfully: {_tableName}");
        }

        [CanBeNull]
        public async Task<decimal?> GetPricesEvolution(CandlePriceType priceType, DateTime? startDate)
        {
            var whereClause =
                "WHERE PriceType=@priceTypeVar AND TimeInterval=86400 AND (@startDateVar IS NULL OR Timestamp <= @startDateVar)";
            var orderByClause = $"ORDER BY Timestamp {(startDate.HasValue ? "DESC" : "ASC")}";
            using (var conn = new SqlConnection(_connectionString))
            {
                var result = await conn.QuerySingleOrDefaultAsync<double?>(
                    $"SELECT TOP 1 [Close] FROM {_tableName} {whereClause} {orderByClause}",
                    new
                    {
                        priceTypeVar = priceType,
                        startDateVar = startDate
                    }, null, commandTimeout);
                
                return result.HasValue ? Convert.ToDecimal(result.Value) : (decimal?)null;
            }
        }

        [CanBeNull]
        public async Task<(decimal low, decimal high)?> GetCandlesEvolution(CandlePriceType priceType, DateTime? startDate)
        {
            var lowAndHightwhereClause =
                "WHERE PriceType=@priceTypeVar AND TimeInterval=86400 AND (@startDateVar IS NULL OR Timestamp >= @startDateVar)";
            using (var conn = new SqlConnection(_connectionString))
            {
                var result = await conn.QuerySingleOrDefaultAsync<(double? low, double? high)>(
                    $"SELECT MIN(Low), MAX(High) FROM {_tableName} {lowAndHightwhereClause}",
                    new
                    {
                        priceTypeVar = priceType,
                        startDateVar = startDate
                    }, null, commandTimeout);

                return result.high.HasValue && result.low.HasValue
                    ? (Convert.ToDecimal(result.low.Value), Convert.ToDecimal(result.high.Value))
                    : ((decimal low, decimal high)?)null;
            }
        }

        public async Task<IEnumerable<ICandle>> GetCandlesAsync(CandlePriceType priceType, CandleTimeInterval interval, DateTime from, DateTime to)
        {

            var whereClause =
                "WHERE PriceType=@priceTypeVar AND TimeInterval=@intervalVar AND Timestamp >= @fromVar  AND Timestamp < @toVar";

            using (var conn = new SqlConnection(_connectionString))
            {

                    var objects = await conn.QueryAsync<SqlCandleHistoryItem>($"SELECT * FROM {_tableName} {whereClause}",
                        new { priceTypeVar = priceType, intervalVar = interval, fromVar = from, toVar = to }, null, commandTimeout: commandTimeout);

                    return objects;
               
            }

        }

        public async Task<ICandle> TryGetFirstCandleAsync(CandlePriceType priceType, CandleTimeInterval timeInterval)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var candle = await conn.QueryFirstOrDefaultAsync<SqlCandleHistoryItem>(
                    $"SELECT TOP(1) * FROM {_tableName} WHERE PriceType=@priceTypeVar AND TimeInterval=@intervalVar ",
                                                                    new { priceTypeVar = priceType, intervalVar = timeInterval });

                return candle;
            }
        }
    }
}
