/* Copyright (C) 2019 Interactive Brokers LLC. All rights reserved. This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */
using System;
using System.Collections.Generic;
using System.Linq;

using IBApi;

using TradingEngine.messages;
using System.Threading;
using System.Threading.Tasks;

namespace TradingEngine
{
    public class IBClient : EWrapper
    {
        public Task<Contract> ResolveContractAsync(int conId, string refExch)
        {
            var reqId = new Random(DateTime.Now.Millisecond).Next();
            var resolveResult = new TaskCompletionSource<Contract>();
            var resolveContract_Error = new Action<int, int, string, string, Exception>((id, code, msg, advancedOrderRejectJson, ex) =>
                {
                    if (reqId != id)
                        return;

                    resolveResult.SetResult(null);
                });
            var resolveContract = new Action<ContractDetailsMessage>(msg =>
                {
                    if (msg.RequestId == reqId)
                        resolveResult.SetResult(msg.ContractDetails.Contract);
                });
            var contractDetailsEnd = new Action<int>(id =>
            {
                if (reqId == id && !resolveResult.Task.IsCompleted)
                    resolveResult.SetResult(null);
            });

            var tmpError = Error;
            var tmpContractDetails = ContractDetails;
            var tmpContractDetailsEnd = ContractDetailsEnd;

            Error = resolveContract_Error;
            ContractDetails = resolveContract;
            ContractDetailsEnd = contractDetailsEnd;

            resolveResult.Task.ContinueWith(t =>
            {
                Error = tmpError;
                ContractDetails = tmpContractDetails;
                ContractDetailsEnd = tmpContractDetailsEnd;
            });

            ClientSocket.reqContractDetails(reqId, new Contract
                                                   { ConId = conId, Exchange = refExch });

            return resolveResult.Task;
        }

        public Task<Contract[]> ResolveContractAsync(string secType, string symbol, string currency, string exchange)
        {
            var reqId = new Random(DateTime.Now.Millisecond).Next();
            var res = new TaskCompletionSource<Contract[]>();
            var contractList = new List<Contract>();
            var resolveContract_Error = new Action<int, int, string, string, Exception>((id, code, msg, advancedOrderRejectJson, ex) =>
                {
                    if (reqId != id)
                        return;

                    res.SetResult(new Contract[0]);
                });
            var contractDetails = new Action<ContractDetailsMessage>(msg =>
                {
                    if (reqId != msg.RequestId)
                        return;

                    contractList.Add(msg.ContractDetails.Contract);
                });
            var contractDetailsEnd = new Action<int>(id =>
                {
                    if (reqId == id)
                        res.SetResult(contractList.ToArray());
                });

            var tmpError = Error;
            var tmpContractDetails = ContractDetails;
            var tmpContractDetailsEnd = ContractDetailsEnd;

            Error = resolveContract_Error;
            ContractDetails = contractDetails;
            ContractDetailsEnd = contractDetailsEnd;

            res.Task.ContinueWith(t =>
            {
                Error = tmpError;
                ContractDetails = tmpContractDetails;
                ContractDetailsEnd = tmpContractDetailsEnd;
            });

            ClientSocket.reqContractDetails(reqId, new Contract
                                                   { SecType = secType, Symbol = symbol, Currency = currency, Exchange = exchange });

            return res.Task;
        }

        public int ClientId { get; set; }

        public int nextRequestId = 0;

        SynchronizationContext sc;

        public IBClient(EReaderSignal signal)
        {
            Signal = signal;
            ClientSocket = new EClientSocket(this, signal);
            sc = SynchronizationContext.Current;
        }

        public EClientSocket ClientSocket { get; private set; }
        public EReaderSignal Signal { get; private set; }

        public int NextOrderId { get; set; }

        public event Action<int, int, string, string, Exception> Error;

        void EWrapper.error(Exception e)
        {
            var tmp = Error;

            if (tmp != null)
                sc.Post(t => tmp(0, 0, null, null, e), null);
        }

        void EWrapper.error(string str)
        {
            var tmp = Error;

            if (tmp != null)
                sc.Post(t => tmp(0, 0, str, null, null), null);
        }

        void EWrapper.error(int id, int errorCode, string errorMsg, string advancedOrderRejectJson)
        {
            var tmp = Error;

            if (tmp != null)
                sc.Post(t => tmp(id, errorCode, errorMsg, advancedOrderRejectJson, null), null);
        }

        public event Action ConnectionClosed;

        void EWrapper.connectionClosed()
        {
            var tmp = ConnectionClosed;

            if (tmp != null)
                sc.Post(t => tmp(), null);
        }

        public event Action<long> CurrentTime;

        void EWrapper.currentTime(long time)
        {
            var tmp = CurrentTime;

            if (tmp != null)
                sc.Post(t => tmp(time), null);
        }

        event Action<TickPriceMessage> TickPrice;

        void EWrapper.tickPrice(int tickerId, int field, double price, TickAttrib attribs)
        {
            var tmp = TickPrice;

            if (tmp != null)
                sc.Post(t => tmp(new TickPriceMessage(tickerId, field, price, attribs)), null);
        }

        event Action<TickSizeMessage> TickSize;

        void EWrapper.tickSize(int tickerId, int field, decimal size)
        {
            var tmp = TickSize;

            if (tmp != null)
                sc.Post(t => tmp(new TickSizeMessage(tickerId, field, size)), null);
        }

        public event Action<int, int, string> TickString;

        void EWrapper.tickString(int tickerId, int tickType, string value)
        {
            var tmp = TickString;

            if (tmp != null)
                sc.Post(t => tmp(tickerId, tickType, value), null);
        }

        event Action<TickGenericMessage> TickGeneric;

        void EWrapper.tickGeneric(int tickerId, int field, double value)
        {
            var tmp = TickGeneric;

            if (tmp != null)
                sc.Post(t => tmp(new TickGenericMessage(tickerId, field, value)), null);
        }

        public event Action<int, int, double, string, double, int, string, double, double> TickEFP;

        void EWrapper.tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays, string futureLastTradeDate, double dividendImpact, double dividendsToLastTradeDate)
        {
            var tmp = TickEFP;

            if (tmp != null)
                sc.Post(t => tmp(tickerId, tickType, basisPoints, formattedBasisPoints, impliedFuture, holdDays, futureLastTradeDate, dividendImpact, dividendsToLastTradeDate), null);
        }

        public event Action<int> TickSnapshotEnd;

        void EWrapper.tickSnapshotEnd(int tickerId)
        {
            var tmp = TickSnapshotEnd;

            if (tmp != null)
                sc.Post(t => tmp(tickerId), null);
        }

        event Action<ConnectionStatusMessage> NextValidId;

        void EWrapper.nextValidId(int orderId)
        {
            var tmp = NextValidId;

            if (tmp != null)
                sc.Post(t => tmp(new ConnectionStatusMessage(true)), null);

            NextOrderId = orderId;
        }

        public event Action<int, DeltaNeutralContract> DeltaNeutralValidation;

        void EWrapper.deltaNeutralValidation(int reqId, DeltaNeutralContract deltaNeutralContract)
        {
            var tmp = DeltaNeutralValidation;

            if (tmp != null)
                sc.Post(t => tmp(reqId, deltaNeutralContract), null);
        }

        event Action<ManagedAccountsMessage> ManagedAccounts;

        void EWrapper.managedAccounts(string accountsList)
        {
            var tmp = ManagedAccounts;

            if (tmp != null)
                sc.Post(t => tmp(new ManagedAccountsMessage(accountsList)), null);
        }

        event Action<TickOptionMessage> TickOptionCommunication;

        void EWrapper.tickOptionComputation(int tickerId, int field, int tickAttrib, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice)
        {
            var tmp = TickOptionCommunication;

            if (tmp != null)
                sc.Post(t => tmp(new TickOptionMessage(tickerId, field, tickAttrib, impliedVolatility, delta, optPrice, pvDividend, gamma, vega, theta, undPrice)), null);
        }

        event Action<AccountSummaryMessage> AccountSummary;

        void EWrapper.accountSummary(int reqId, string account, string tag, string value, string currency)
        {
            var tmp = AccountSummary;

            if (tmp != null)
                sc.Post(t => tmp(new AccountSummaryMessage(reqId, account, tag, value, currency)), null);
        }

        event Action<AccountSummaryEndMessage> AccountSummaryEnd;

        void EWrapper.accountSummaryEnd(int reqId)
        {
            var tmp = AccountSummaryEnd;

            if (tmp != null)
                sc.Post(t => tmp(new AccountSummaryEndMessage(reqId)), null);
        }

        event Action<AccountValueMessage> UpdateAccountValue;

        void EWrapper.updateAccountValue(string key, string value, string currency, string accountName)
        {
            var tmp = UpdateAccountValue;

            if (tmp != null)
                sc.Post(t => tmp(new AccountValueMessage(key, value, currency, accountName)), null);
        }

        event Action<UpdatePortfolioMessage> UpdatePortfolio;

        void EWrapper.updatePortfolio(Contract contract, decimal position, double marketPrice, double marketValue, double averageCost, double unrealizedPNL, double realizedPNL, string accountName)
        {
            var tmp = UpdatePortfolio;

            if (tmp != null)
                sc.Post(t => tmp(new UpdatePortfolioMessage(contract, position, marketPrice, marketValue, averageCost, unrealizedPNL, realizedPNL, accountName)), null);
        }

        event Action<UpdateAccountTimeMessage> UpdateAccountTime;

        void EWrapper.updateAccountTime(string timestamp)
        {
            var tmp = UpdateAccountTime;

            if (tmp != null)
                sc.Post(t => tmp(new UpdateAccountTimeMessage(timestamp)), null);
        }

        event Action<AccountDownloadEndMessage> AccountDownloadEnd;

        void EWrapper.accountDownloadEnd(string account)
        {
            var tmp = AccountDownloadEnd;

            if (tmp != null)
                sc.Post(t => tmp(new AccountDownloadEndMessage(account)), null);
        }

        event Action<OrderStatusMessage> OrderStatus;

        void EWrapper.orderStatus(int orderId, string status, decimal filled, decimal remaining, double avgFillPrice, int permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice)
        {
            var tmp = OrderStatus;

            if (tmp != null)
                sc.Post(t => tmp(new OrderStatusMessage(orderId, status, filled, remaining, avgFillPrice, permId, parentId, lastFillPrice, clientId, whyHeld, mktCapPrice)), null);
        }

        event Action<OpenOrderMessage> OpenOrder;

        void EWrapper.openOrder(int orderId, Contract contract, Order order, OrderState orderState)
        {
            var tmp = OpenOrder;

            if (tmp != null)
                sc.Post(t => tmp(new OpenOrderMessage(orderId, contract, order, orderState)), null);
        }

        public event Action OpenOrderEnd;

        void EWrapper.openOrderEnd()
        {
            var tmp = OpenOrderEnd;

            if (tmp != null)
                sc.Post(t => tmp(), null);
        }

        event Action<ContractDetailsMessage> ContractDetails;

        void EWrapper.contractDetails(int reqId, ContractDetails contractDetails)
        {
            var tmp = ContractDetails;

            if (tmp != null)
                sc.Post(t => tmp(new ContractDetailsMessage(reqId, contractDetails)), null);
        }

        public event Action<int> ContractDetailsEnd;

        void EWrapper.contractDetailsEnd(int reqId)
        {
            var tmp = ContractDetailsEnd;

            if (tmp != null)
                sc.Post(t => tmp(reqId), null);
        }

        event Action<ExecutionMessage> ExecDetails;

        void EWrapper.execDetails(int reqId, Contract contract, Execution execution)
        {
            var tmp = ExecDetails;

            if (tmp != null)
                sc.Post(t => tmp(new ExecutionMessage(reqId, contract, execution)), null);
        }

        public event Action<int> ExecDetailsEnd;

        void EWrapper.execDetailsEnd(int reqId)
        {
            var tmp = ExecDetailsEnd;

            if (tmp != null)
                sc.Post(t => tmp(reqId), null);
        }

        public event Action<CommissionReport> CommissionReport;

        void EWrapper.commissionReport(CommissionReport commissionReport)
        {
            var tmp = CommissionReport;

            if (tmp != null)
                sc.Post(t => tmp(commissionReport), null);
        }

        event Action<FundamentalsMessage> FundamentalData;

        void EWrapper.fundamentalData(int reqId, string data)
        {
            var tmp = FundamentalData;

            if (tmp != null)
                sc.Post(t => tmp(new FundamentalsMessage(data)), null);
        }

        event Action<HistoricalDataMessage> HistoricalData;

        void EWrapper.historicalData(int reqId, Bar bar)
        {
            var tmp = HistoricalData;

            if (tmp != null)
                sc.Post(t => tmp(new HistoricalDataMessage(reqId, bar)), null);
        }

        event Action<HistoricalDataEndMessage> HistoricalDataEnd;

        public event EventHandler HistoricalDataAllEnded;

        void EWrapper.historicalDataEnd(int reqId, string startDate, string endDate)
        {
            var tmp = HistoricalDataEnd;

            HistoryDataRequestIdCompletion[reqId] = true;

            if (tmp != null)
                sc.Post(t => tmp(new HistoricalDataEndMessage(reqId, startDate, endDate)), null);

            if (!HistoryDataRequestIdCompletion.ContainsValue(false))
            {
                if (HistoricalDataAllEnded != null)
                {   
                    HistoricalDataAllEnded(this, EventArgs.Empty);
                }
            }
        }

        public void ResetHistoryDataCompletionDictionary()
        {
            HistoryDataRequestIdCompletion = new Dictionary<int, bool>();
        }

        event Action<MarketDataTypeMessage> MarketDataType;

        void EWrapper.marketDataType(int reqId, int marketDataType)
        {
            var tmp = MarketDataType;

            if (tmp != null)
                sc.Post(t => tmp(new MarketDataTypeMessage(reqId, marketDataType)), null);
        }

        event Action<DeepBookMessage> UpdateMktDepth;

        void EWrapper.updateMktDepth(int tickerId, int position, int operation, int side, double price, decimal size)
        {
            var tmp = UpdateMktDepth;

            if (tmp != null)
                sc.Post(t => tmp(new DeepBookMessage(tickerId, position, operation, side, price, size, "", false)), null);
        }

        event Action<DeepBookMessage> UpdateMktDepthL2;

        void EWrapper.updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, decimal size, bool isSmartDepth)
        {
            var tmp = UpdateMktDepthL2;

            if (tmp != null)
                sc.Post(t => tmp(new DeepBookMessage(tickerId, position, operation, side, price, size, marketMaker, isSmartDepth)), null);
        }

        public event Action<int, int, string, string> UpdateNewsBulletin;

        void EWrapper.updateNewsBulletin(int msgId, int msgType, string message, string origExchange)
        {
            var tmp = UpdateNewsBulletin;

            if (tmp != null)
                sc.Post(t => tmp(msgId, msgType, message, origExchange), null);
        }

        event Action<PositionMessage> Position;

        void EWrapper.position(string account, Contract contract, decimal pos, double avgCost)
        {
            var tmp = Position;

            if (tmp != null)
                sc.Post(t => tmp(new PositionMessage(account, contract, pos, avgCost)), null);
        }

        public event Action PositionEnd;

        void EWrapper.positionEnd()
        {
            var tmp = PositionEnd;

            if (tmp != null)
                sc.Post(t => tmp(), null);
        }

        event Action<RealTimeBarMessage> RealtimeBar;

        void EWrapper.realtimeBar(int reqId, long time, double open, double high, double low, double close, decimal volume, decimal WAP, int count)
        {
            var tmp = RealtimeBar;

            if (tmp != null)
                sc.Post(t => tmp(new RealTimeBarMessage(reqId, time, open, high, low, close, volume, WAP, count)), null);
        }

        public event Action<string> ScannerParameters;

        void EWrapper.scannerParameters(string xml)
        {
            var tmp = ScannerParameters;

            if (tmp != null)
                sc.Post(t => tmp(xml), null);
        }

        event Action<ScannerMessage> ScannerData;

        void EWrapper.scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr)
        {
            var tmp = ScannerData;

            if (tmp != null)
                sc.Post(t => tmp(new ScannerMessage(reqId, rank, contractDetails, distance, benchmark, projection, legsStr)), null);
        }

        public event Action<int> ScannerDataEnd;

        void EWrapper.scannerDataEnd(int reqId)
        {
            var tmp = ScannerDataEnd;

            if (tmp != null)
                sc.Post(t => tmp(reqId), null);
        }

        event Action<AdvisorDataMessage> ReceiveFA;

        void EWrapper.receiveFA(int faDataType, string faXmlData)
        {
            var tmp = ReceiveFA;

            if (tmp != null)
                sc.Post(t => tmp(new AdvisorDataMessage(faDataType, faXmlData)), null);
        }

        event Action<BondContractDetailsMessage> BondContractDetails;

        void EWrapper.bondContractDetails(int requestId, ContractDetails contractDetails)
        {
            var tmp = BondContractDetails;

            if (tmp != null)
                sc.Post(t => tmp(new BondContractDetailsMessage(requestId, contractDetails)), null);
        }

        public event Action<string> VerifyMessageAPI;

        void EWrapper.verifyMessageAPI(string apiData)
        {
            var tmp = VerifyMessageAPI;

            if (tmp != null)
                sc.Post(t => tmp(apiData), null);
        }
        public event Action<bool, string> VerifyCompleted;

        void EWrapper.verifyCompleted(bool isSuccessful, string errorText)
        {
            var tmp = VerifyCompleted;

            if (tmp != null)
                sc.Post(t => tmp(isSuccessful, errorText), null);
        }

        public event Action<string, string> VerifyAndAuthMessageAPI;

        void EWrapper.verifyAndAuthMessageAPI(string apiData, string xyzChallenge)
        {
            var tmp = VerifyAndAuthMessageAPI;

            if (tmp != null)
                sc.Post(t => tmp(apiData, xyzChallenge), null);
        }

        public event Action<bool, string> VerifyAndAuthCompleted;

        void EWrapper.verifyAndAuthCompleted(bool isSuccessful, string errorText)
        {
            var tmp = VerifyAndAuthCompleted;

            if (tmp != null)
                sc.Post(t => tmp(isSuccessful, errorText), null);
        }

        public event Action<int, string> DisplayGroupList;

        void EWrapper.displayGroupList(int reqId, string groups)
        {
            var tmp = DisplayGroupList;

            if (tmp != null)
                sc.Post(t => tmp(reqId, groups), null);
        }

        public event Action<int, string> DisplayGroupUpdated;

        void EWrapper.displayGroupUpdated(int reqId, string contractInfo)
        {
            var tmp = DisplayGroupUpdated;

            if (tmp != null)
                sc.Post(t => tmp(reqId, contractInfo), null);
        }


        void EWrapper.connectAck()
        {
            if (ClientSocket.AsyncEConnect)
                ClientSocket.startApi();
        }

        event Action<PositionMultiMessage> PositionMulti;

        void EWrapper.positionMulti(int reqId, string account, string modelCode, Contract contract, decimal pos, double avgCost)
        {
            var tmp = PositionMulti;

            if (tmp != null)
                sc.Post(t => tmp(new PositionMultiMessage(reqId, account, modelCode, contract, pos, avgCost)), null);
        }

        public event Action<int> PositionMultiEnd;

        void EWrapper.positionMultiEnd(int reqId)
        {
            var tmp = PositionMultiEnd;

            if (tmp != null)
                sc.Post(t => tmp(reqId), null);
        }

        event Action<AccountUpdateMultiMessage> AccountUpdateMulti;

        void EWrapper.accountUpdateMulti(int reqId, string account, string modelCode, string key, string value, string currency)
        {
            var tmp = AccountUpdateMulti;

            if (tmp != null)
                sc.Post(t => tmp(new AccountUpdateMultiMessage(reqId, account, modelCode, key, value, currency)), null);
        }

        public event Action<int> AccountUpdateMultiEnd;

        void EWrapper.accountUpdateMultiEnd(int reqId)
        {
            var tmp = AccountUpdateMultiEnd;

            if (tmp != null)
                sc.Post(t => tmp(reqId), null);
        }

        event Action<SecurityDefinitionOptionParameterMessage> SecurityDefinitionOptionParameter;

        void EWrapper.securityDefinitionOptionParameter(int reqId, string exchange, int underlyingConId, string tradingClass, string multiplier, HashSet<string> expirations, HashSet<double> strikes)
        {
            var tmp = SecurityDefinitionOptionParameter;

            if (tmp != null)
                sc.Post(t => tmp(new SecurityDefinitionOptionParameterMessage(reqId, exchange, underlyingConId, tradingClass, multiplier, expirations, strikes)), null);
        }

        public event Action<int> SecurityDefinitionOptionParameterEnd;

        void EWrapper.securityDefinitionOptionParameterEnd(int reqId)
        {
            var tmp = SecurityDefinitionOptionParameterEnd;

            if (tmp != null)
                sc.Post(t => tmp(reqId), null);
        }

        event Action<SoftDollarTiersMessage> SoftDollarTiers;

        void EWrapper.softDollarTiers(int reqId, SoftDollarTier[] tiers)
        {
            var tmp = SoftDollarTiers;

            if (tmp != null)
                sc.Post(t => tmp(new SoftDollarTiersMessage(reqId, tiers)), null);
        }

        public event Action<FamilyCode[]> FamilyCodes;

        void EWrapper.familyCodes(FamilyCode[] familyCodes)
        {
            var tmp = FamilyCodes;

            if (tmp != null)
                sc.Post(t => tmp(familyCodes), null);
        }

        event Action<SymbolSamplesMessage> SymbolSamples;

        void EWrapper.symbolSamples(int reqId, ContractDescription[] contractDescriptions)
        {
            var tmp = SymbolSamples;

            if (tmp != null)
                sc.Post(t => tmp(new SymbolSamplesMessage(reqId, contractDescriptions)), null);
        }


        public event Action<DepthMktDataDescription[]> MktDepthExchanges;

        void EWrapper.mktDepthExchanges(DepthMktDataDescription[] depthMktDataDescriptions)
        {
            var tmp = MktDepthExchanges;

            if (tmp != null)
                sc.Post(t => tmp(depthMktDataDescriptions), null);
        }

        event Action<TickNewsMessage> TickNews;

        void EWrapper.tickNews(int tickerId, long timeStamp, string providerCode, string articleId, string headline, string extraData)
        {
            var tmp = TickNews;

            if (tmp != null)
                sc.Post(t => tmp(new TickNewsMessage(tickerId, timeStamp, providerCode, articleId, headline, extraData)), null);
        }

        public event Action<int, Dictionary<int, KeyValuePair<string, char>>> SmartComponents;

        public Dictionary<int, Contract> RequestIdToContract = new Dictionary<int, Contract>();
        public Dictionary<int, string> RequestIdToType = new Dictionary<int, string>();
        public Dictionary<int, bool> HistoryDataRequestIdCompletion = new Dictionary<int, bool>();

        void EWrapper.smartComponents(int reqId, Dictionary<int, KeyValuePair<string, char>> theMap)
        {
            var tmp = SmartComponents;

            if (tmp != null)
                sc.Post(t => tmp(reqId, theMap), null);
        }

        event Action<TickReqParamsMessage> TickReqParams;

        void EWrapper.tickReqParams(int tickerId, double minTick, string bboExchange, int snapshotPermissions)
        {
            var tmp = TickReqParams;

            if (tmp != null)
                sc.Post(t => tmp(new TickReqParamsMessage(tickerId, minTick, bboExchange, snapshotPermissions)), null);
        }

        public event Action<NewsProvider[]> NewsProviders;

        void EWrapper.newsProviders(NewsProvider[] newsProviders)
        {
            var tmp = NewsProviders;

            if (tmp != null)
                sc.Post(t => tmp(newsProviders), null);
        }

        event Action<NewsArticleMessage> NewsArticle;

        void EWrapper.newsArticle(int requestId, int articleType, string articleText)
        {
            var tmp = NewsArticle;

            if (tmp != null)
                sc.Post(t => tmp(new NewsArticleMessage(requestId, articleType, articleText)), null);
        }

        event Action<HistoricalNewsMessage> HistoricalNews;

        void EWrapper.historicalNews(int requestId, string time, string providerCode, string articleId, string headline)
        {
            var tmp = HistoricalNews;

            if (tmp != null)
                sc.Post(t => tmp(new HistoricalNewsMessage(requestId, time, providerCode, articleId, headline)), null);
        }

        event Action<HistoricalNewsEndMessage> HistoricalNewsEnd;

        void EWrapper.historicalNewsEnd(int requestId, bool hasMore)
        {
            var tmp = HistoricalNewsEnd;

            if (tmp != null)
                sc.Post(t => tmp(new HistoricalNewsEndMessage(requestId, hasMore)), null);
        }

        event Action<HeadTimestampMessage> HeadTimestamp;

        void EWrapper.headTimestamp(int reqId, string headTimestamp)
        {
            var tmp = HeadTimestamp;

            if (tmp != null)
                sc.Post(t => tmp(new HeadTimestampMessage(reqId, headTimestamp)), null);
        }

        event Action<HistogramDataMessage> HistogramData;

        void EWrapper.histogramData(int reqId, HistogramEntry[] data)
        {
            var tmp = HistogramData;

            if (tmp != null)
                sc.Post(t => tmp(new HistogramDataMessage(reqId, data)), null);
        }

        event Action<HistoricalDataMessage> HistoricalDataUpdate;

        void EWrapper.historicalDataUpdate(int reqId, Bar bar)
        {
            var tmp = HistoricalDataUpdate;

            if (tmp != null)
                sc.Post(t => tmp(new HistoricalDataMessage(reqId, bar)), null);
        }

        public event Action<int, int, string> RerouteMktDataReq;

        void EWrapper.rerouteMktDataReq(int reqId, int conId, string exchange)
        {
            var tmp = RerouteMktDataReq;

            if (tmp != null)
                sc.Post(t => tmp(reqId, conId, exchange), null);
        }

        public event Action<int, int, string> RerouteMktDepthReq;

        void EWrapper.rerouteMktDepthReq(int reqId, int conId, string exchange)
        {
            var tmp = RerouteMktDepthReq;

            if (tmp != null)
                sc.Post(t => tmp(reqId, conId, exchange), null);
        }

        public event Action<MarketRuleMessage> MarketRule;

        void EWrapper.marketRule(int marketRuleId, PriceIncrement[] priceIncrements)
        {
            var tmp = MarketRule;

            if (tmp != null)
                sc.Post(t => tmp(new MarketRuleMessage(marketRuleId, priceIncrements)), null);
        }

        event Action<PnLMessage> pnl;

        void EWrapper.pnl(int reqId, double dailyPnL, double unrealizedPnL, double realizedPnL)
        {
            var tmp = pnl;

            if (tmp != null)
                sc.Post(t => tmp(new PnLMessage(reqId, dailyPnL, unrealizedPnL, realizedPnL)), null);
        }

        event Action<PnLSingleMessage> pnlSingle;

        void EWrapper.pnlSingle(int reqId, decimal pos, double dailyPnL, double unrealizedPnL, double realizedPnL, double value)
        {
            var tmp = pnlSingle;

            if (tmp != null)
                sc.Post(t => tmp(new PnLSingleMessage(reqId, pos, dailyPnL, unrealizedPnL, realizedPnL, value)), null);
        }

        event Action<HistoricalTickMessage> historicalTick;

        void EWrapper.historicalTicks(int reqId, HistoricalTick[] ticks, bool done)
        {
            var tmp = historicalTick;

            if (tmp != null)
                ticks.ToList().ForEach(tick => sc.Post(t => tmp(new HistoricalTickMessage(reqId, tick.Time, tick.Price, tick.Size)), null));
        }

        event Action<HistoricalTickBidAskMessage> historicalTickBidAsk;

        void EWrapper.historicalTicksBidAsk(int reqId, HistoricalTickBidAsk[] ticks, bool done)
        {
            var tmp = historicalTickBidAsk;

            if (tmp != null)
                ticks.ToList().ForEach(tick => sc.Post(t =>
                    tmp(new HistoricalTickBidAskMessage(reqId, tick.Time, tick.TickAttribBidAsk, tick.PriceBid, tick.PriceAsk, tick.SizeBid, tick.SizeAsk)), null));
        }

        event Action<HistoricalTickLastMessage> historicalTickLast;

        void EWrapper.historicalTicksLast(int reqId, HistoricalTickLast[] ticks, bool done)
        {
            var tmp = historicalTickLast;

            if (tmp != null)
                ticks.ToList().ForEach(tick => sc.Post(t => 
                    tmp(new HistoricalTickLastMessage(reqId, tick.Time, tick.TickAttribLast, tick.Price, tick.Size, tick.Exchange, tick.SpecialConditions)), null));
        }

        event Action<TickByTickAllLastMessage> tickByTickAllLast;

        void EWrapper.tickByTickAllLast(int reqId, int tickType, long time, double price, decimal size, TickAttribLast tickAttribLast, string exchange, string specialConditions)
        {
            var tmp = tickByTickAllLast;

            if (tmp != null)
                sc.Post(t => tmp(new TickByTickAllLastMessage(reqId, tickType, time, price, size, tickAttribLast, exchange, specialConditions)), null);
        }

        event Action<TickByTickBidAskMessage> tickByTickBidAsk;

        void EWrapper.tickByTickBidAsk(int reqId, long time, double bidPrice, double askPrice, decimal bidSize, decimal askSize, TickAttribBidAsk tickAttribBidAsk)
        {
            var tmp = tickByTickBidAsk;

            if (tmp != null)
                sc.Post(t => tmp(new TickByTickBidAskMessage(reqId, time, bidPrice, askPrice, bidSize, askSize, tickAttribBidAsk)), null);
        }

        event Action<TickByTickMidPointMessage> tickByTickMidPoint;

        void EWrapper.tickByTickMidPoint(int reqId, long time, double midPoint)
        {
            var tmp = tickByTickMidPoint;

            if (tmp != null)
                sc.Post(t => tmp(new TickByTickMidPointMessage(reqId, time, midPoint)), null);
        }

        event Action<OrderBoundMessage> OrderBound;

        void EWrapper.orderBound(long orderId, int apiClientId, int apiOrderId)
        {
            var tmp = OrderBound;

            if (tmp != null)
                sc.Post(t => tmp(new OrderBoundMessage(orderId, apiClientId, apiOrderId)), null);
        }

        event Action<CompletedOrderMessage> CompletedOrder;

        void EWrapper.completedOrder(Contract contract, Order order, OrderState orderState)
        {
            var tmp = CompletedOrder;

            if (tmp != null)
                sc.Post(t => tmp(new CompletedOrderMessage(contract, order, orderState)), null);
        }

        public event Action CompletedOrdersEnd;

        void EWrapper.completedOrdersEnd()
        {
            var tmp = CompletedOrdersEnd;

            if (tmp != null)
                sc.Post(t => tmp(), null);
        }

        public event Action<int, string> ReplaceFAEnd;

        void EWrapper.replaceFAEnd(int reqId, string text)
        {
            var tmp = ReplaceFAEnd;

            if (tmp != null)
                sc.Post(t => tmp(reqId, text), null);
        }

        public event Action<int, string> WshMetaData;

        public void wshMetaData(int reqId, string dataJson)
        {
            var tmp = WshMetaData;

            if (tmp != null)
                sc.Post(t => tmp(reqId, dataJson), null);
        }

        public event Action<int, string> WshEventData;

        public void wshEventData(int reqId, string dataJson)
        {
            var tmp = WshEventData;

            if (tmp != null)
                sc.Post(t => tmp(reqId, dataJson), null);
        }

        event Action<HistoricalScheduleMessage> HistoricalSchedule;

        public void historicalSchedule(int reqId, string startDateTime, string endDateTime, string timeZone, HistoricalSession[] sessions)
        {
            var tmp = HistoricalSchedule;

            if (tmp != null)
                sc.Post(t => tmp(new HistoricalScheduleMessage(reqId, startDateTime, endDateTime, timeZone, sessions)), null);
        }

        public event Action<string> UserInfo;
        void EWrapper.userInfo(int reqId, string whiteBrandingId)
        {
            var tmp = UserInfo;
            if (tmp != null)
                sc.Post(t => tmp(whiteBrandingId), null);
        }

        public void ConnectToTWS()
        {
            string host = "127.0.0.1"; // Localhost for TWS
            int port = 7497;          // Default port for paper trading, use 7496 for live
            int clientId = 0;         // Unique client ID

            try
            {
                // Establish the connection
                ClientSocket.eConnect(host, port, clientId);

                if (ClientSocket.IsConnected())
                {
                    Console.WriteLine("Successfully connected to TWS.");
                    StartMessageProcessing();
                }
                else
                {
                    Console.WriteLine("Failed to connect to TWS.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to TWS: {ex.Message}");
            }
        }

        public void DisconnectFromTWS()
        {
            try
            {
                if (ClientSocket != null && ClientSocket.IsConnected())
                {
                    ClientSocket.eDisconnect();
                    Console.WriteLine("Successfully disconnected from TWS.");
                }
                else
                {
                    Console.WriteLine("Client is not connected to TWS.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during disconnection: {ex.Message}");
            }
        }

        public void GetRealtimeDataForSymbol(string symbol, string exchange, string currency, string secType = "STK")
        {
            if (ClientSocket == null || !ClientSocket.IsConnected())
            {
                Console.WriteLine("Not connected to TWS. Please connect before requesting real-time data.");
                return;
            }

            var contract = new Contract
            {
                Symbol = symbol,
                SecType = secType, // Default to "STK" (stock)
                Exchange = exchange,
                Currency = currency
            };

            int requestId = nextRequestId++;

            RequestIdToContract[requestId] = contract;
            ClientSocket.reqMktData(requestId, contract, "", false, false, null);
            //todo - ClientSocket.reqRealTimeBars(

            Console.WriteLine($"Requested real-time data for {symbol} on {exchange}.");
        }

        public void GetHistoricalDataForSymbol(string symbol, string exchange, string currency, string duration, string barSize, string secType = "STK", string endDateTime = "", string whatToShow = "TRADES")
        {
            if (ClientSocket == null || !ClientSocket.IsConnected())
            {
                Console.WriteLine("Not connected to TWS. Please connect before requesting historical data.");
                return;
            }

            var contract = new Contract
            {
                Symbol = symbol,
                SecType = secType,
                Exchange = exchange,
                Currency = currency
            };

            int requestId = nextRequestId++;
            RequestIdToContract[requestId] = contract;
            RequestIdToType[requestId] = whatToShow;
            HistoryDataRequestIdCompletion[requestId] = false;
            ClientSocket.reqHistoricalData(
                requestId,
                contract,
                endDateTime,  // Leave empty for the current date/time
                duration,     // Duration string (e.g., "1 D", "1 W", "1 M")
                barSize,      // Bar size (e.g., "1 min", "5 mins", "1 day")
                whatToShow,   // Data type to return (e.g., "TRADES", "MIDPOINT")
                1,            // Use RTH (Regular Trading Hours) or not (1 = yes, 0 = no)
                1,            // Format date (1 = YYYYMMDD hh:mm:ss, 2 = Unix time)
                false,  //todo - check what is upToDate
                null          // Chart options
            );

            Console.WriteLine($"Requested historical data for {symbol} on {exchange}.");
        }

        public void GetHistoricalTickForSymbol(
            string symbol,
            string exchange,
            string currency,
            string secType,
            string startTime,
            string endTime,
            int numberOfTicks,
            string whatToShow = "TRADES")
        {
            if (ClientSocket == null || !ClientSocket.IsConnected())
            {
                Console.WriteLine("Not connected to TWS. Please connect before requesting historical tick data.");
                return;
            }

            var contract = new Contract
            {
                Symbol = symbol,
                SecType = secType,
                Exchange = exchange,
                Currency = currency
            };

            int requestId = nextRequestId++;

            // Request historical tick data
            ClientSocket.reqHistoricalTicks(
                requestId,
                contract,
                startTime,
                endTime,
                numberOfTicks,
                whatToShow,
                1, //use All hours// Use RTH (Regular Trading Hours)
                true, // Ignore size (optional)
                null);

            Console.WriteLine($"Requested historical tick data for {symbol} from {startTime} to {endTime}.");
        }

        private void StartMessageProcessing()
        {
            var reader = new EReader(ClientSocket, Signal);
            reader.Start();

            // Use a background thread to continuously process incoming messages
            new System.Threading.Thread(() =>
            {
                while (ClientSocket.IsConnected())
                {
                    Signal.waitForSignal();
                    reader.processMsgs();
                }
            })
            { IsBackground = true }.Start();
        }
    }
}