using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Lykke.Service.Assets.Client;
using Lykke.Service.CandlesHistory.Services.Settings;
using Lykke.SettingsReader.Attributes;
using Lykke.Snow.Common.Startup.ApiKey;

namespace Lykke.Service.CandlesHistory
{
    public class AppSettings
    {
        [Optional]
        public CandlesHistorySettings CandlesHistory { get; set; }
        [Optional]
        public CandlesHistorySettings MtCandlesHistory { get; set; }
        [Optional, CanBeNull]
        public SlackNotificationsSettings SlackNotifications { get; set; }

        public AssetsSettings Assets { get; set; }

        public RedisSettings RedisSettings { get; set; }

        [Optional]
        public ClientSettings MtCandlesHistoryServiceClient { get; set; } = new ClientSettings();
    }
}
