﻿using System;
using BinanceNETStandard.API.Enums;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using BinanceNETStandard.API.Converter;
using BinanceNETStandard.API.Models.Request.Interfaces;

namespace BinanceNETStandard.API.Models.Request
{
    [DataContract]
    public class FundHistoryRequest : IRequest
    {
        [DataMember(Order = 1)]
        public string Asset { get; set; }

        [DataMember(Order = 2)]
        public DepositHistoryStatus? Status { get; set; }

        [DataMember(Order = 3)]
        [JsonConverter(typeof(EpochTimeConverter))]
        public DateTime? StartTime { get; set; }

        [DataMember(Order = 4)]
        [JsonConverter(typeof(EpochTimeConverter))]
        public DateTime? EndTime { get; set; }
    }
}