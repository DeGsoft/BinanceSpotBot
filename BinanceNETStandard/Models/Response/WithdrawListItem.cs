﻿using System;
using System.Runtime.Serialization;
using BinanceNETStandard.API.Converter;
using BinanceNETStandard.API.Enums;
using BinanceNETStandard.API.Models.Response.Interfaces;
using Newtonsoft.Json;

namespace BinanceNETStandard.API.Models.Response
{
    [DataContract]
    public class WithdrawListItem : IResponse
    {
        [DataMember(Order = 1)]
        public string Id { get; set; }

        [DataMember(Order = 2)]
        public decimal Amount { get; set; }

        [DataMember(Order = 3)]
        public string Address { get; set; }

        [DataMember(Order = 4)]
        public string AddressTag { get; set; }

        [DataMember(Order = 5)]
        [JsonProperty(PropertyName = "txId")]
        public string TransactionId { get; set; }

        [DataMember(Order = 6)]
        public string Asset { get; set; }

        [DataMember(Order = 7)]
        [JsonConverter(typeof(EpochTimeConverter))]
        public DateTime ApplyTime { get; set; }

        [DataMember(Order = 8)]
        public WithdrawHistoryStatus Status { get; set; }
    }
}