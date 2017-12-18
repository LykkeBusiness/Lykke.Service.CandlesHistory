﻿using System.Threading.Tasks;
using Lykke.Service.CandlesHistory.Core.Services.HistoryMigration.HistoryProviders;
using Lykke.Service.CandlesHistory.Services.HistoryMigration;
using Lykke.Service.CandlesHistory.Services.HistoryMigration.HistoryProviders.MeFeedHistory;
using Microsoft.AspNetCore.Mvc;
namespace Lykke.Service.CandlesHistory.Controllers
{
    [Route("api/[controller]")]
    public class CandlesHistoryMigrationController : Controller
    {
        private readonly CandlesMigrationManager _candlesMigrationManager;
        private readonly IHistoryProvidersManager _historyProvidersManager;

        public CandlesHistoryMigrationController(
            CandlesMigrationManager candlesMigrationManager, 
            IHistoryProvidersManager historyProvidersManager)
        {
            _candlesMigrationManager = candlesMigrationManager;
            _historyProvidersManager = historyProvidersManager;
        }

        [HttpPost]
        [Route("{assetPair}")]
        public async Task<IActionResult> Migrate(string assetPair)
        {
            var result = await _candlesMigrationManager.MigrateAsync(
                assetPair,
                _historyProvidersManager.GetProvider<MeFeedHistoryProvider>());

            return Ok(result);
        }

        [HttpGet]
        [Route("health")]
        public IActionResult Health()
        {
            return Ok(_candlesMigrationManager.Health);
        }

        [HttpGet]
        [Route("health/{assetPair}")]
        public IActionResult Health(string assetPair)
        {
            if (!_candlesMigrationManager.Health.ContainsKey(assetPair))
            {
                return NotFound();
            }

            return Ok(_candlesMigrationManager.Health[assetPair]);
        }
    }
}
