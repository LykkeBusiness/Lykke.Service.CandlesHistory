﻿using System.Threading.Tasks;
using Autofac;
using Lykke.Domain.Prices.Contracts;

namespace Lykke.Service.CandlesHistory.Core.Services.Candles
{
    public interface ICandlesProducer : IStartable
    {
        Task ProduceAsync(IFeedCandle failedCandlesEnvelope);
    }
}