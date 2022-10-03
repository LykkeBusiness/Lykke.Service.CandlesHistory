// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Lykke.Service.CandlesHistory.Models.CandlesHistory;
using Lykke.Snow.Common.Types;
using DateOnly = Lykke.Snow.Common.Types.DateOnly;

namespace Lykke.Service.CandlesHistory.Extensions
{
    public static class PriceEvolutionPeriodExtensions
    {
        public static DateOnly? GetPriceEvolutionStartDate(this PriceEvolutionPeriodType period, DateOnly date)
        {
            var dateTime = (DateTime) date;
            switch (period)
            {
                case PriceEvolutionPeriodType.AllTime:
                    return null;
                case PriceEvolutionPeriodType.YTD:
                    return new DateTime(dateTime.Year, 1, 1, 0, 0, 0, dateTime.Kind);
                case PriceEvolutionPeriodType.Day:
                    return dateTime.AddDays(-1);
                case PriceEvolutionPeriodType.Week:
                    return dateTime.AddDays(-7);
                case PriceEvolutionPeriodType.Month:
                    return dateTime.AddMonths(-1);
                case PriceEvolutionPeriodType.ThreeMonths:
                    return dateTime.AddMonths(-3);
                case PriceEvolutionPeriodType.SixMonths:
                    return dateTime.AddMonths(-6);
                case PriceEvolutionPeriodType.Year:
                    return dateTime.AddYears(-1);
                case PriceEvolutionPeriodType.ThreeYears:
                    return dateTime.AddYears(-3);
                case PriceEvolutionPeriodType.FiveYears:
                    return dateTime.AddYears(-5);
                case PriceEvolutionPeriodType.TenYears:
                    return dateTime.AddYears(-10);
                default: throw new NotImplementedException();
            }
        }
        
        public static DateOnly? GetCandleEvolutionStartDate(this CandleEvolutionType type, DateOnly date)
        {
            var dateTime = (DateTime) date;
            switch (type)
            {
                case CandleEvolutionType.AllTime:
                    return null;
                case CandleEvolutionType.Today:
                    return dateTime;
                case CandleEvolutionType.Month:
                    return dateTime.AddMonths(-1);
                case CandleEvolutionType.Year:return dateTime.AddYears(-1);
                default: throw new NotImplementedException();
            }
        }
    }
}
