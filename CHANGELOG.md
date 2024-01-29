## 2.5.0 - Nova 2. Delivery 39 (January 29, 2024)
### What's changed
* LT-5168: Add history of releases into `changelog.md`


## 2.4.0 - Nova 2. Delivery 36 (2023-08-31)
### What's changed
* LT-4908: Update nugets.


**Full change log**: https://github.com/lykkecloud/lykke.service.candleshistory/compare/v2.2.2...v2.4.0

## v2.2.2 - Nova 2. Delivery 28. Hotfix 3
* LT-4316: Upgrade LykkeBiz.Logs.Serilog to 3.3.1

## v2.2.1 - Nova 2. Delivery 28
## What's Changed
* LT-3721: NET 6 migration

### Deployment
* NET 6 runtime is required
* Dockerfile is updated to use native Microsoft images (see [DockerHub](https://hub.docker.com/_/microsoft-dotnet-runtime/))

**Full Changelog**: https://github.com/LykkeBusiness/Lykke.Service.CandlesHistory/compare/v2.1.7...v2.2.1

## v2.1.7 - Nova 2. Delivery 27
## What's Changed
* LT-4200: Product Performance is the same on daily and weekly

**Full Changelog**: https://github.com/LykkeBusiness/Lykke.Service.CandlesHistory/compare/v2.1.2...v2.1.7

## v2.1.6 - Nova 2. Delivery 26. Hotfix 1
### What's changed
* LT-4200: Product Performance is the same on daily and weekly 

**Full Changelog**: https://github.com/LykkeBusiness/Lykke.Service.CandlesHistory/compare/v2.1.5...v2.1.6

## v2.1.5 - Nova 2. Delivery 26
* LT-4061: CandlesHistory: add api to fetch low/high/eod mid prices for each period

## v2.1.4 - Nova 2. Delivery 24
## What's Changed
* Lt 3898 upgrade httpclientgenerator by @lykke-vashetsin in https://github.com/LykkeBusiness/Lykke.Service.CandlesHistory/pull/43

## New Contributors
* @lykke-vashetsin made their first contribution in https://github.com/LykkeBusiness/Lykke.Service.CandlesHistory/pull/42

**Full Changelog**: https://github.com/LykkeBusiness/Lykke.Service.CandlesHistory/compare/v2.1.2...v2.1.4

## v2.1.3 - Nova 2. Delivery 21
* LT-3717: NOVA security threats

## v2.1.0 - Nova 2. Delivery 5.
### Tasks

* LT-2845: Fix issue with candles for new products
* LT-2908: Watchlists don't work

## v1.6.0 - Migration to .NET 3.1
### Tasks

LT-2175: Migrate to 3.1 Core and update DL libraries

## v1.5.4 - Timezone bugfix 
### Tasks

LT-2049: CandlesHistory: CHARTS: the price is not moving in mobile app (STG, DEMO)

## v1.5.3 - Alpine docker image, Http logs
### Tasks

LT-1985: Migrate to Alpine docker images in MT Core services
LT-1760: Check that all services have .net core 2.2 in projects and docker
LT-1919: Lykke.Service.CandlesHistory: add WebHostLogger

## v1.5.1 - History request improvement, bug fix for history selection
### Tasks

LT-1722: Make response of chart history request inclusive
LT-1723: Send next time parameter on chart history request

## v1.5.0 - License
### Tasks
LT-1541: Update licenses in lego service to be up to latest requirements

## v1.4.1 - Secured API clients, improvements
### Tasks
MTC-809: Secure all "visible" endpoints in mt-core
MTC-740: Return Candles update date

### Deployment
Add new property ApiKey to Assets section (optional, if settings service does not use API key):
```json
"Assets": 
  {
    "ServiceUrl": "settings service url",
    "CacheExpirationPeriod": "00:05:00",
    "ApiKey": "settings service secret key"
  },
```

## v1.4.0 - Remove redundant SQL queries from Candles API
### Tasks:
MTC-720: Remove redundant SQL queries from Candles API

## v1.3.2 - Updated projects versions
No changes

## v1.3.1 - Fixes error with dashes in asset ID
### Bugfix

MTC-590 : Remove dash from table name and index in CandleHistory

## v1.3.0 - Auto schema creation
CandlesHistory create schema if not exists (MTC-500)

## v1.2.7 - Logs bugfix
### Bugfixes
- Fixed Serilog text logs (MTC-461)

## v1.2.6 - Commented unused code
Fix Fortify comment (MTC-439)

## v1.2.5 - Deployment and maintenance improvements
Introduced:
Text logs
Kestrel configuration (see README)

New settings:
UseSerilog": true

https://lykke-snow.atlassian.net/browse/MTC-398
https://lykke-snow.atlassian.net/browse/MTC-411
https://lykke-snow.atlassian.net/browse/MTC-413

## v1.2.0 - New configuration of connection strings
Changes in config: 
Added MtCandlesHistory -> Db -> SnapshotsConnectionString
Latest config example:  [candles.appsettings.json.zip](https://github.com/lykkecloud/Lykke.Service.CandlesHistory/files/2500847/candles.appsettings.json.zip)

## v1.1.0 - Bug fixes
No config changes

## v1.0.2 - No Azure dependencies, SQL
[Configs.zip](https://github.com/lykkecloud/Lykke.Service.CandlesHistory/files/2251286/Configs.zip)
