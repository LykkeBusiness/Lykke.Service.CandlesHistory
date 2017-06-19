﻿using Autofac;
using Common.Log;
using Lykke.Service.CandlesHistory.Core;

namespace Lykke.Service.CandlesHistory.DependencyInjection
{
    public class ApiModule : Module
    {
        private readonly ApplicationSettings.CandlesHistorySettings _settings;
        private readonly ILog _log;

        public ApiModule(ApplicationSettings.CandlesHistorySettings settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log).SingleInstance();

            builder.RegisterInstance(_settings).SingleInstance();
        }
    }
}