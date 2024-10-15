// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Logs.MsSql;
using Lykke.Logs.MsSql.Repositories;
using Lykke.Logs.Serilog;
using Lykke.Service.CandlesHistory.Core.Domain;
using Lykke.Service.CandlesHistory.Core.Services;
using Lykke.Service.CandlesHistory.DependencyInjection;
using Lykke.SettingsReader;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Converters;
using Lykke.Service.CandlesHistory.Models;
using Lykke.Service.CandlesHistory.Services.Settings;
using Lykke.Service.CandlesHistory.Core.Domain.Candles;
using Lykke.Service.CandlesHistory.Services;
using Lykke.Service.CandlesHistory.Services.Assets;
using Lykke.Snow.Common.AssemblyLogging;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.Logging;
using Lykke.Snow.Common.Startup.Log;
using Lykke.Snow.Common.Startup.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace Lykke.Service.CandlesHistory
{
    [UsedImplicitly]
    public class Startup
    {
        private IReloadingManager<AppSettings> _mtSettingsManager;
        private IHostEnvironment Environment { get; set; }
        private ILifetimeScope ApplicationContainer { get; set; }
        private IConfigurationRoot Configuration { get; }
        private ILog Log { get; set; }

        public static string ServiceName { get; } = PlatformServices.Default.Application.ApplicationName;

        public Startup(IHostEnvironment env)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddSerilogJson(env)
                .AddEnvironmentVariables()
                .Build();
            Environment = env;
        }

        [UsedImplicitly]
        public void ConfigureServices(IServiceCollection services)
        {
            try
            {
                services.AddAssemblyLogger();
                services
                    .AddControllers()
                    .AddNewtonsoftJson(options =>
                    {
                        options.SerializerSettings.Converters.Add(new StringEnumConverter());
                        options.SerializerSettings.ContractResolver =
                            new Newtonsoft.Json.Serialization.DefaultContractResolver();
                    });

                services.AddEndpointsApiExplorer();
                services.AddSwaggerGen(options =>
                {
                    options.DefaultLykkeConfiguration("v1", "Candles history service");
                });
                
                _mtSettingsManager = Configuration.LoadSettings<AppSettings>();

                var candlesHistory = _mtSettingsManager.CurrentValue.CandlesHistory != null
                    ? _mtSettingsManager.Nested(x => x.CandlesHistory)
                    : _mtSettingsManager.Nested(x => x.MtCandlesHistory);
                

                Log = CreateLogWithSlack(Configuration, services, candlesHistory,
                    _mtSettingsManager.CurrentValue.SlackNotifications);

                services.AddSingleton<ILoggerFactory>(x => new WebHostLoggerFactory(Log));

                services.AddApplicationInsightsTelemetry();
            }
            catch (Exception ex)
            {
                Log?.WriteFatalErrorAsync(nameof(Startup), nameof(ConfigureServices), "", ex).Wait();
                throw;
            }
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            var marketType = _mtSettingsManager.CurrentValue.CandlesHistory != null
                ? MarketType.Spot
                : MarketType.Mt;

            var candlesHistory = _mtSettingsManager.CurrentValue.CandlesHistory != null
                ? _mtSettingsManager.Nested(x => x.CandlesHistory)
                : _mtSettingsManager.Nested(x => x.MtCandlesHistory);
            
            builder.RegisterModule(new ApiModule(
                marketType,
                candlesHistory.CurrentValue,
                _mtSettingsManager.CurrentValue.Assets,
                _mtSettingsManager.CurrentValue.RedisSettings,
                candlesHistory.ConnectionString(x => x.Db.SnapshotsConnectionString),
                Log));

            if (marketType == MarketType.Mt)
            {
                builder.RegisterBuildCallback(c => c.Resolve<MtAssetPairsManager>());
            }
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app, IHostEnvironment env, IHostApplicationLifetime appLifetime)
        {
            try
            {
                ApplicationContainer = app.ApplicationServices.GetAutofacRoot();
                
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }
                else
                {
                    app.UseHsts();
                }

                app.UseLykkeMiddleware(nameof(Startup),
                    ex => ErrorResponse.Create(
                        $"Technical problem:  {ex.Message}\r\nInnerEX:  {ex.InnerException.Message}"), false, false);

                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
                app.UseSwagger(c =>
                {
                    c.PreSerializeFilters.Add((swagger, httpReq) => 
                        swagger.Servers =
                            new List<OpenApiServer>
                            {
                                new OpenApiServer
                                {
                                    Url = $"{httpReq.Scheme}://{httpReq.Host.Value}"
                                }
                            }
                    );
                });
                app.UseSwagger();
                app.UseSwaggerUI(x =>
                {
                    x.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                });

                
                app.UseStaticFiles();

                appLifetime.ApplicationStarted.Register(() => StartApplication().GetAwaiter().GetResult());
                appLifetime.ApplicationStopping.Register(() => StopApplication().GetAwaiter().GetResult());
                appLifetime.ApplicationStopped.Register(() => CleanUp().GetAwaiter().GetResult());
            }
            catch (Exception ex)
            {
                Log?.WriteFatalErrorAsync(nameof(Startup), nameof(Configure), "", ex).Wait();
                throw;
            }
        }

        private async Task StartApplication()
        {
            try
            {   
                Program.AppHost.WriteLogs(Environment, Log);

                await Log.WriteMonitorAsync("", "", "Started");
            }
            catch (Exception ex)
            {
                await Log.WriteFatalErrorAsync(nameof(Startup), nameof(StartApplication), "", ex);
                throw;
            }
        }

        private async Task StopApplication()
        {
            try
            {
                await ApplicationContainer.Resolve<IShutdownManager>().ShutdownAsync();
            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    await Log.WriteFatalErrorAsync(nameof(Startup), nameof(StopApplication), "", ex);
                }
                throw;
            }
        }

        private async Task CleanUp()
        {
            try
            {
                if (Log != null)
                {
                    await Log.WriteMonitorAsync("", "", "Terminating");
                }

                ApplicationContainer.Dispose();
            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    await Log.WriteFatalErrorAsync(nameof(Startup), nameof(CleanUp), "", ex);
                }
                throw;
            }
        }

        private static ILog CreateLogWithSlack(IConfiguration configuration, IServiceCollection services,
            IReloadingManager<CandlesHistorySettings> settings, SlackNotificationsSettings slackSettings)
        {
            const string tableName = "CandlesHistoryServiceLog";
            var consoleLogger = new LogToConsole();
            var aggregateLogger = new AggregateLogger();
            var settingsValue = settings.CurrentValue;

            aggregateLogger.AddLog(consoleLogger);

            if (settingsValue.UseSerilog)
            {
                aggregateLogger.AddLog(new SerilogLogger(typeof(Startup).Assembly, configuration));
            }
            else if (settingsValue.Db.StorageMode == StorageMode.SqlServer)
            {
                aggregateLogger.AddLog(
                    new LogToSql(new SqlLogRepository(tableName, settingsValue.Db.LogsConnectionString)));
            }
            else if (settingsValue.Db.StorageMode == StorageMode.Azure)
            {
                throw new NotImplementedException("Azure storage is not implemented");
            }

            LogLocator.Log = aggregateLogger;

            return aggregateLogger;
        }
    }
}
