// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Lykke.Service.CandlesHistory.Core.Services.ProductPerformance
{
    public interface IPriceEvolutionService
    {
        Task<(decimal? firstEod, decimal? lowest, decimal? highest)> GetPrices(string assetPairId, DateTime? from);
    }
}
