﻿using JetBrains.Annotations;
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.CandlesHistory.Services.Settings
{
    [UsedImplicitly]
    public class DbSettings
    {
        [AzureTableCheck]
        public string LogsConnectionString { get; set; }

        public StorageMode StorageMode { get; set; }

        [SqlCheck]
        public string SqlConnectionString { get; set; }
    }
}
