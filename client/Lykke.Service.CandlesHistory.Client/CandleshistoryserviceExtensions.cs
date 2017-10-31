﻿// Code generated by Microsoft (R) AutoRest Code Generator 1.0.1.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace Lykke.Service.CandlesHistory.Client
{
    using Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods for Candleshistoryservice.
    /// </summary>
    public static partial class CandleshistoryserviceExtensions
    {
            /// <summary>
            /// Asset's candles history
            /// </summary>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='assetPairId'>
            /// Asset pair ID
            /// </param>
            /// <param name='priceType'>
            /// Price type. Possible values include: 'Unspecified', 'Bid', 'Ask', 'Mid'
            /// </param>
            /// <param name='timeInterval'>
            /// Time interval. Possible values include: 'Unspecified', 'Sec', 'Minute',
            /// 'Min5', 'Min15', 'Min30', 'Hour', 'Hour4', 'Hour6', 'Hour12', 'Day',
            /// 'Week', 'Month'
            /// </param>
            /// <param name='fromMoment'>
            /// From moment in ISO 8601 (inclusive)
            /// </param>
            /// <param name='toMoment'>
            /// To moment in ISO 8601 (exclusive)
            /// </param>
            public static object GetCandlesHistoryOrError(this ICandleshistoryservice operations, string assetPairId, PriceType priceType, TimeInterval timeInterval, System.DateTime fromMoment, System.DateTime toMoment)
            {
                return operations.GetCandlesHistoryOrErrorAsync(assetPairId, priceType, timeInterval, fromMoment, toMoment).GetAwaiter().GetResult();
            }

            /// <summary>
            /// Asset's candles history
            /// </summary>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='assetPairId'>
            /// Asset pair ID
            /// </param>
            /// <param name='priceType'>
            /// Price type. Possible values include: 'Unspecified', 'Bid', 'Ask', 'Mid'
            /// </param>
            /// <param name='timeInterval'>
            /// Time interval. Possible values include: 'Unspecified', 'Sec', 'Minute',
            /// 'Min5', 'Min15', 'Min30', 'Hour', 'Hour4', 'Hour6', 'Hour12', 'Day',
            /// 'Week', 'Month'
            /// </param>
            /// <param name='fromMoment'>
            /// From moment in ISO 8601 (inclusive)
            /// </param>
            /// <param name='toMoment'>
            /// To moment in ISO 8601 (exclusive)
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<object> GetCandlesHistoryOrErrorAsync(this ICandleshistoryservice operations, string assetPairId, PriceType priceType, TimeInterval timeInterval, System.DateTime fromMoment, System.DateTime toMoment, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.GetCandlesHistoryOrErrorWithHttpMessagesAsync(assetPairId, priceType, timeInterval, fromMoment, toMoment, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <summary>
            /// Checks service is alive
            /// </summary>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            public static IsAliveResponse IsAlive(this ICandleshistoryservice operations)
            {
                return operations.IsAliveAsync().GetAwaiter().GetResult();
            }

            /// <summary>
            /// Checks service is alive
            /// </summary>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<IsAliveResponse> IsAliveAsync(this ICandleshistoryservice operations, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.IsAliveWithHttpMessagesAsync(null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

    }
}
