// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Lykke.Service.CandlesHistory.Models.CandlesHistory
{
    public class PriceEvolution
    {
        public PriceEvolutionPeriodType Period { get; set; }
        public decimal? FirstEod { get; set; }
        public decimal? Highest { get; set; }
        public decimal? Lowest { get; set; }
    }
}
