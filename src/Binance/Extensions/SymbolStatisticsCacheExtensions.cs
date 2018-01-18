﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Binance.Cache.Events;

// ReSharper disable once CheckNamespace
namespace Binance.Cache
{
    public static class SymbolStatisticsCacheExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cache"></param>
        /// <returns></returns>
        public static void Subscribe(this ISymbolStatisticsCache cache)
            => cache.Subscribe(null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static void Subscribe(this ISymbolStatisticsCache cache, string symbol)
            => cache.Subscribe(null, symbol);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static Task StreamAsync(this ISymbolStatisticsCache cache, CancellationToken token)
            => StreamAsync(cache, (Action<SymbolStatisticsCacheEventArgs>)null, token);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="symbol"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static Task StreamAsync(this ISymbolStatisticsCache cache, string symbol, CancellationToken token)
            => StreamAsync(cache, symbol, null, token);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="callback"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static Task StreamAsync(this ISymbolStatisticsCache cache, Action<SymbolStatisticsCacheEventArgs> callback, CancellationToken token)
        {
            Throw.IfNull(cache, nameof(cache));

            cache.Subscribe(callback);

            return cache.Client.WebSocket.StreamAsync(token);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="symbol"></param>
        /// <param name="callback"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static Task StreamAsync(this ISymbolStatisticsCache cache, string symbol, Action<SymbolStatisticsCacheEventArgs> callback, CancellationToken token)
        {
            Throw.IfNull(cache, nameof(cache));

            cache.Subscribe(callback, symbol);

            return cache.Client.WebSocket.StreamAsync(token);
        }
    }
}
