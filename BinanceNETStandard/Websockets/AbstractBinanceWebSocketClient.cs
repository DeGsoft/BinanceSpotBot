﻿using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading.Tasks;
using BinanceNETStandard.API.Client.Interfaces;
using BinanceNETStandard.API.Enums;
using BinanceNETStandard.API.Extensions;
using BinanceNETStandard.API.Models.WebSocket;
using BinanceNETStandard.API.Utility;
using log4net;
using Newtonsoft.Json;
using WebSocketSharp;
using IWebSocketResponse = BinanceNETStandard.API.Models.WebSocket.Interfaces.IWebSocketResponse;

namespace BinanceNETStandard.API.Websockets
{
    /// <summary>
    /// Abstract class for creating WebSocketClients 
    /// </summary>
    public class AbstractBinanceWebSocketClient
    {
        protected SslProtocols SupportedProtocols { get; } = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;

        /// <summary> 
        /// Base WebSocket URI for Binance API
        /// </summary>
        protected string BaseWebsocketUri = "wss://stream.binance.com:9443/ws";

        /// <summary>
        /// Combined WebSocket URI for Binance API
        /// </summary>
        protected string CombinedWebsocketUri = "wss://stream.binance.com:9443/stream?streams";

        /// <summary>
        /// Used for deletion on the fly
        /// </summary>
        protected Dictionary<Guid, BinanceWebSocket> ActiveWebSockets;
        protected List<BinanceWebSocket> AllSockets;
        protected readonly IBinanceClient BinanceClient;
        protected ILog Logger;

        protected const string AccountEventType = "outboundAccountInfo";
        protected const string OrderTradeEventType = "executionReport";
        public string ListenKey { get; private set; }

        public AbstractBinanceWebSocketClient(IBinanceClient binanceClient, ILog logger = null)
        {
            BinanceClient = binanceClient;
            ActiveWebSockets = new Dictionary<Guid, BinanceWebSocket>();
            AllSockets = new List<BinanceWebSocket>();
            Logger = logger ?? LogManager.GetLogger(typeof(AbstractBinanceWebSocketClient));
        }

        // Expose the listenKey that gets returned by Binance REST request "POST /api/v1/userDataStream" to start a user data stream
        /// <summary>
        /// Connect to the UserData WebSocket
        /// </summary>
        /// <param name="userDataMessageHandlers"></param>
        /// <returns>Guid of connection.</returns>
        /// sets the Binance Listen Key (binanceListenKey)
        public async Task<Guid> ConnectToUserDataWebSocket(UserDataWebSocketMessages userDataMessageHandlers)
        {
            Guard.AgainstNull(BinanceClient, nameof(BinanceClient));
            Logger.Debug("Connecting to User Data Web Socket");
            var streamResponse = await BinanceClient.StartUserDataStream();
            ListenKey = streamResponse.ListenKey;
            var endpoint = new Uri($"{BaseWebsocketUri}/{ListenKey}");
            return CreateUserDataBinanceWebSocket(endpoint, userDataMessageHandlers);
        }


        /// <summary>
        /// Connect to the Kline WebSocket
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="interval"></param>
        /// <param name="messageEventHandler"></param>
        /// <returns></returns>
        public Guid ConnectToKlineWebSocket(string symbol, KlineInterval interval, BinanceWebSocketMessageHandler<BinanceKlineData> messageEventHandler)
        {
            Guard.AgainstNullOrEmpty(symbol, nameof(symbol));
            Logger.Debug("Connecting to Kline Web Socket");
            var endpoint = new Uri($"{BaseWebsocketUri}/{symbol.ToLower()}@kline_{EnumExtensions.GetEnumMemberValue(interval)}");
            return CreateBinanceWebSocket(endpoint, messageEventHandler);
        }

        /// <summary>
        /// Connect to the Depth WebSocket
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="messageEventHandler"></param>
        /// <returns></returns>
        public Guid ConnectToDepthWebSocket(string symbol, BinanceWebSocketMessageHandler<BinanceDepthData> messageEventHandler)
        {
            Guard.AgainstNullOrEmpty(symbol, nameof(symbol));
            Logger.Debug("Connecting to Depth Web Socket");
            var endpoint = new Uri($"{BaseWebsocketUri}/{symbol.ToLower()}@depth");
            return CreateBinanceWebSocket(endpoint, messageEventHandler);
        }

        /// <summary>
        /// Connect to thePartial Book Depth Streams
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="messageEventHandler"></param>
        /// <returns></returns>
        public Guid ConnectToPartialDepthWebSocket(string symbol, PartialDepthLevels levels, BinanceWebSocketMessageHandler<BinancePartialData> messageEventHandler)
        {
            Guard.AgainstNullOrEmpty(symbol, nameof(symbol)); 
            Logger.Debug("Connecting to Partial Depth Web Socket");
            var endpoint = new Uri($"{BaseWebsocketUri}/{symbol.ToLower()}@depth{(int)levels}");
            return CreateBinanceWebSocket(endpoint, messageEventHandler);
        }
        /// <summary>
        /// Connect to the Combined Depth WebSocket
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="messageEventHandler"></param>
        /// <returns></returns>
        public Guid ConnectToDepthWebSocketCombined(string symbols, BinanceWebSocketMessageHandler<BinanceCombinedDepthData> messageEventHandler)
        {
            Guard.AgainstNullOrEmpty(symbols, nameof(symbols));
            symbols = PrepareCombinedSymbols.CombinedDepth(symbols);
            Logger.Debug("Connecting to Combined Depth Web Socket");
            var endpoint = new Uri($"{CombinedWebsocketUri}={symbols}");
            return CreateBinanceWebSocket(endpoint, messageEventHandler);
        }
        /// <summary>
        /// Connect to the Combined Partial Depth WebSocket
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="depth"></param>
        /// <param name="messageEventHandler"></param>
        /// <returns></returns>
        public Guid ConnectToDepthWebSocketCombinedPartial(string symbols, string depth, BinanceWebSocketMessageHandler<BinancePartialDepthData> messageEventHandler)
        {
            Guard.AgainstNullOrEmpty(symbols, nameof(symbols));
            Guard.AgainstNullOrEmpty(depth, nameof(depth));
            symbols = PrepareCombinedSymbols.CombinedPartialDepth(symbols, depth);
            Logger.Debug("Connecting to Combined Partial Depth Web Socket");
            var endpoint = new Uri($"{CombinedWebsocketUri}={symbols}");
            return CreateBinanceWebSocket(endpoint, messageEventHandler);
        }

        /// <summary>
        /// Connect to the Trades WebSocket
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="messageEventHandler"></param>
        /// <returns></returns>
        public Guid ConnectToTradesWebSocket(string symbol, BinanceWebSocketMessageHandler<BinanceAggregateTradeData> messageEventHandler)
        {
            Guard.AgainstNullOrEmpty(symbol, nameof(symbol));
            Logger.Debug("Connecting to Trades Web Socket");
            var endpoint = new Uri($"{BaseWebsocketUri}/{symbol.ToLower()}@aggTrade");
            return CreateBinanceWebSocket(endpoint, messageEventHandler);
        }

        /// <summary>
        /// Connect to the Individual Symbol Ticker WebSocket
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="messageEventHandler"></param>
        /// <returns></returns>
        public Guid ConnectToIndividualSymbolTickerWebSocket(string symbol, BinanceWebSocketMessageHandler<BinanceTradeData> messageEventHandler)
        {
            Guard.AgainstNullOrEmpty(symbol, nameof(symbol));
            Logger.Debug("Connecting to Individual Symbol Ticker Web Socket");
            var endpoint = new Uri($"{BaseWebsocketUri}/{symbol.ToLower()}@ticker");
            return CreateBinanceWebSocket(endpoint, messageEventHandler);
        }

        /// <summary>
        /// Connect to the All Market Symbol Ticker WebSocket
        /// </summary>
        /// <param name="messageEventHandler"></param>
        /// <returns></returns>
        public Guid ConnectToIndividualSymbolTickerWebSocket(BinanceWebSocketMessageHandler<BinanceAggregateTradeData> messageEventHandler)
        {
            Logger.Debug("Connecting to All Market Symbol Ticker Web Socket");
            var endpoint = new Uri($"{BaseWebsocketUri}/!ticker@arr");
            return CreateBinanceWebSocket(endpoint, messageEventHandler);
        }

        private Guid CreateUserDataBinanceWebSocket(Uri endpoint, UserDataWebSocketMessages userDataWebSocketMessages)
        {
            var websocket = new BinanceWebSocket(endpoint.AbsoluteUri);
            websocket.OnOpen += (sender, e) =>
            {
                Logger.Debug($"WebSocket Opened:{endpoint.AbsoluteUri}");
            };
            websocket.OnMessage += (sender, e) =>
            {
                Logger.Debug($"WebSocket Message Received on Endpoint: {endpoint.AbsoluteUri}");
                var primitive = JsonConvert.DeserializeObject<BinanceWebSocketResponse>(e.Data);
                switch (primitive.EventType)
                {
                    case AccountEventType:
                        var userData = JsonConvert.DeserializeObject<BinanceAccountUpdateData>(e.Data);
                        userDataWebSocketMessages.AccountUpdateMessageHandler?.Invoke(userData);
                        break;
                    case OrderTradeEventType:
                        var orderTradeData = JsonConvert.DeserializeObject<BinanceTradeOrderData>(e.Data);
                        if (orderTradeData.ExecutionType == ExecutionType.Trade)
                        {
                            userDataWebSocketMessages.TradeUpdateMessageHandler?.Invoke(orderTradeData);
                        }
                        else
                        {
                            userDataWebSocketMessages.OrderUpdateMessageHandler?.Invoke(orderTradeData);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            };
            websocket.OnError += (sender, e) =>
            {
                Logger.Error($"WebSocket Error on {endpoint.AbsoluteUri}: ", e.Exception);
                CloseWebSocketInstance(websocket.Id, true);
                throw new Exception("Binance UserData WebSocket failed")
                {
                    Data =
                    {
                        {"ErrorEventArgs", e}
                    }
                };
            };

            if (!ActiveWebSockets.ContainsKey(websocket.Id))
            {
                ActiveWebSockets.Add(websocket.Id, websocket);
            }

            AllSockets.Add(websocket);
            websocket.SslConfiguration.EnabledSslProtocols = SupportedProtocols;
            websocket.Connect();

            return websocket.Id;
        }

        private Guid CreateBinanceWebSocket<T>(Uri endpoint, BinanceWebSocketMessageHandler<T> messageEventHandler) where T : IWebSocketResponse
        {
            var websocket = new BinanceWebSocket(endpoint.AbsoluteUri);
            websocket.OnOpen += (sender, e) =>
            {
                Logger.Debug($"WebSocket Opened:{endpoint.AbsoluteUri}");
            };
            websocket.OnMessage += (sender, e) =>
            {
                Logger.Debug($"WebSocket Messge Received on: {endpoint.AbsoluteUri}");
                //TODO: Log message received
                var data = JsonConvert.DeserializeObject<T>(e.Data);
                messageEventHandler(data);
            };
            websocket.OnError += (sender, e) =>
            {
                Logger.Debug($"WebSocket Error on {endpoint.AbsoluteUri}:", e.Exception);
                CloseWebSocketInstance(websocket.Id, true);
                throw new Exception("Binance WebSocket failed")
                {
                    Data =
                    {
                        {"ErrorEventArgs", e}
                    }
                };
            };

            if (!ActiveWebSockets.ContainsKey(websocket.Id))
            {
                ActiveWebSockets.Add(websocket.Id, websocket);
            }

            AllSockets.Add(websocket);
            websocket.SslConfiguration.EnabledSslProtocols = SupportedProtocols;
            websocket.Connect();

            return websocket.Id;
        }

        /// <summary>
        /// Close a specific WebSocket instance using the Guid provided on creation
        /// </summary>
        /// <param name="id"></param>
        /// <param name="fromError"></param>
        public void CloseWebSocketInstance(Guid id, bool fromError = false)
        {
            if (ActiveWebSockets.ContainsKey(id))
            {
                var ws = ActiveWebSockets[id];
                ActiveWebSockets.Remove(id);
                if (!fromError)
                {
                    ws.Close(CloseStatusCode.PolicyViolation);
                }
            }
            else
            {
                throw new Exception($"No Websocket exists with the Id {id.ToString()}");
            }
        }

        /// <summary>
        /// Checks whether a specific WebSocket instance is active or not using the Guid provided on creation
        /// </summary>
        public bool IsAlive(Guid id)
        {
            if (ActiveWebSockets.ContainsKey(id))
            {
                var ws = ActiveWebSockets[id];
                return ws.IsAlive;
            }
            else
            {
                throw new Exception($"No Websocket exists with the Id {id.ToString()}");
            }
        }
    }
}
