// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Lykke.Service.CandlesHistory.Client.Models
{
    public class CandleEvolution
    {
        public CandleEvolutionType Type { get; set; }
        public decimal Low { get; set; }
        public decimal High { get; set; }
    }
}
