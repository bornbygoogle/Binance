﻿using System;
using System.Collections.Generic;
using System.Threading;
using Binance.Api.WebSocket.Events;
using Binance.Market;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Binance.Api.WebSocket
{
    /// <summary>
    /// A <see cref="ITradeWebSocketClient"/> implementation.
    /// </summary>
    public class TradeWebSocketClient : BinanceWebSocketClient<TradeEventArgs>, ITradeWebSocketClient
    {
        #region Public Events

        public event EventHandler<TradeEventArgs> Trade;

        #endregion Public Events

        #region Public Properties

        public string Symbol { get; private set; }

        #endregion Public Properties

        #region Constructors

        /// <summary>
        /// Default constructor provides default web socket client, but no logging.
        /// </summary>
        public TradeWebSocketClient()
            : this(new BinanceWebSocketStream(), null)
        { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="webSocket"></param>
        /// <param name="logger"></param>
        public TradeWebSocketClient(IWebSocketStream webSocket, ILogger<TradeWebSocketClient> logger = null)
            : base(webSocket, logger)
        { }

        #endregion Construtors

        #region Public Methods

        public virtual void Subscribe(string symbol, Action<TradeEventArgs> callback)
        {
            Throw.IfNullOrWhiteSpace(symbol, nameof(symbol));

            Symbol = symbol.FormatSymbol();

            base.SubscribeTo($"{Symbol.ToLowerInvariant()}@trade", callback);
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Deserialize JSON and raise <see cref="TradeEventArgs"/> event.
        /// </summary>
        /// <param name="json"></param>
        /// <param name="token"></param>
        /// <param name="callback"></param>
        protected override void DeserializeJsonAndRaiseEvent(string json, CancellationToken token, IEnumerable<Action<TradeEventArgs>> callbacks)
        {
            Throw.IfNullOrWhiteSpace(json, nameof(json));

            Logger?.LogDebug($"{nameof(TradeWebSocketClient)}: \"{json}\"");

            try
            {
                var jObject = JObject.Parse(json);

                var eventType = jObject["e"].Value<string>();

                if (eventType == "trade")
                {
                    var eventTime = jObject["E"].Value<long>();

                    var trade = new Trade(
                        jObject["s"].Value<string>(),  // symbol
                        jObject["t"].Value<long>(),    // trade ID
                        jObject["p"].Value<decimal>(), // price
                        jObject["q"].Value<decimal>(), // quantity
                        jObject["b"].Value<long>(),    // buyer order ID
                        jObject["a"].Value<long>(),    // seller order ID
                        jObject["T"].Value<long>(),    // trade time (timestamp)
                        jObject["m"].Value<bool>(),    // is buyer the market maker?
                        jObject["M"].Value<bool>());   // is best price match?

                    var eventArgs = new TradeEventArgs(eventTime, token, trade);

                    try
                    {
                        if (callbacks != null)
                        {
                            foreach (var callback in callbacks)
                                callback(eventArgs);
                        }
                        Trade?.Invoke(this, eventArgs);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception e)
                    {
                        if (!token.IsCancellationRequested)
                        {
                            Logger?.LogError(e, $"{nameof(TradeWebSocketClient)}: Unhandled aggregate trade event handler exception.");
                        }
                    }
                }
                else
                {
                    Logger?.LogWarning($"{nameof(TradeWebSocketClient)}.{nameof(DeserializeJsonAndRaiseEvent)}: Unexpected event type ({eventType}).");
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                if (!token.IsCancellationRequested)
                {
                    Logger?.LogError(e, $"{nameof(TradeWebSocketClient)}.{nameof(DeserializeJsonAndRaiseEvent)}");
                }
            }
        }

        #endregion Protected Methods
    }
}
