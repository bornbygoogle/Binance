﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Binance.Api.WebSocket.Events;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Binance.Api.WebSocket
{
    /// <summary>
    /// A <see cref="IDepthWebSocketClient"/> implementation.
    /// </summary>
    public class DepthWebSocketClient : BinanceWebSocketClient<DepthUpdateEventArgs>, IDepthWebSocketClient
    {
        #region Public Events

        public event EventHandler<DepthUpdateEventArgs> DepthUpdate;

        #endregion Public Events

        #region Public Properties

        public string Symbol { get; private set; }

        #endregion Public Properties

        #region Constructors

        /// <summary>
        /// Default constructor provides default web socket stream, but no logging.
        /// </summary>
        public DepthWebSocketClient()
            : this(new BinanceWebSocketStream(), null)
        { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="logger"></param>
        public DepthWebSocketClient(IWebSocketStream stream, ILogger<DepthWebSocketClient> logger = null)
            : base(stream, logger)
        { }

        #endregion Construtors

        #region Public Methods

        public virtual void Subscribe(string symbol, int limit, Action<DepthUpdateEventArgs> callback)
        {
            Throw.IfNullOrWhiteSpace(symbol, nameof(symbol));

            Symbol = symbol.FormatSymbol();

            SubscribeTo(limit > 0 ? $"{Symbol.ToLowerInvariant()}@depth{limit}" : $"{Symbol.ToLowerInvariant()}@depth", callback);
        }

        #endregion Public Methods

        #region Protected Methods

        protected override void DeserializeJsonAndRaiseEvent(string json, CancellationToken token, IEnumerable<Action<DepthUpdateEventArgs>> callbacks)
        {
            Throw.IfNullOrWhiteSpace(json, nameof(json));

            Logger?.LogDebug($"{nameof(DepthWebSocketClient)}: \"{json}\"");

            try
            {
                var jObject = JObject.Parse(json);

                var eventType = jObject["e"]?.Value<string>();

                DepthUpdateEventArgs eventArgs;

                switch (eventType)
                {
                    case null:
                    {
                        // Simulate event time.
                        var eventTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                        var lastUpdateId = jObject["lastUpdateId"].Value<long>();

                        var bids = jObject["bids"].Select(entry => (entry[0].Value<decimal>(), entry[1].Value<decimal>())).ToArray();
                        var asks = jObject["asks"].Select(entry => (entry[0].Value<decimal>(), entry[1].Value<decimal>())).ToArray();

                        eventArgs = new DepthUpdateEventArgs(eventTime, token, Symbol, lastUpdateId, lastUpdateId, bids, asks);
                        break;
                    }
                    case "depthUpdate":
                    {
                        var symbol = jObject["s"].Value<string>();
                        var eventTime = jObject["E"].Value<long>();

                        var firstUpdateId = jObject["U"].Value<long>();
                        var lastUpdateId = jObject["u"].Value<long>();

                        var bids = jObject["b"].Select(entry => (entry[0].Value<decimal>(), entry[1].Value<decimal>())).ToArray();
                        var asks = jObject["a"].Select(entry => (entry[0].Value<decimal>(), entry[1].Value<decimal>())).ToArray();

                        eventArgs = new DepthUpdateEventArgs(eventTime, token, symbol, firstUpdateId, lastUpdateId, bids, asks);
                        break;
                    }
                    default:
                        Logger?.LogWarning($"{nameof(DepthWebSocketClient)}.{nameof(DeserializeJsonAndRaiseEvent)}: Unexpected event type ({eventType}).");
                        return;
                }

                try
                {
                    if (callbacks != null)
                    {
                        foreach (var callback in callbacks)
                            callback(eventArgs);
                    }
                    DepthUpdate?.Invoke(this, eventArgs);
                }
                catch (OperationCanceledException) { }
                catch (Exception e)
                {
                    if (!token.IsCancellationRequested)
                    {
                        Logger?.LogError(e, $"{nameof(DepthWebSocketClient)}: Unhandled depth update event handler exception.");
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                if (!token.IsCancellationRequested)
                {
                    Logger?.LogError(e, $"{nameof(DepthWebSocketClient)}.{nameof(DeserializeJsonAndRaiseEvent)}");
                }
            }
        }

        #endregion Protected Methods
    }
}
