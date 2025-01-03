[![.NET](https://github.com/LykkeBusiness/Lykke.Service.CandlesHistory/actions/workflows/build.yml/badge.svg)](https://github.com/LykkeBusiness/Lykke.Service.CandlesHistory/actions/workflows/build.yml)

# MarginTrading.CandlesHistory API #

API to get candles history.

## How to use in prod env? ##

1. Pull "candleshistory" docker image with a corresponding tag.
2. Configure environment variables according to "Environment variables" section.
3. Put secrets.json with endpoint data including the certificate:
```json
"Kestrel": {
  "EndPoints": {
    "HttpsInlineCertFile": {
      "Url": "https://*:5100",
      "Certificate": {
        "Path": "<path to .pfx file>",
        "Password": "<certificate password>"
      }
    }
  }
}
```
4. Initialize all dependencies.
5. Run.

## How to run for debug? ##

1. Clone repo to some directory.
2. In MarginTrading.CandlesHistory root create a appsettings.dev.json with settings.
3. Add environment variable "SettingsUrl": "appsettings.dev.json".
4. VPN to a corresponding env must be connected and all dependencies must be initialized.
5. Run.

### Dependencies ###

TBD

### Configuration ###

Kestrel configuration may be passed through appsettings.json, secrets or environment.
All variables and value constraints are default. For instance, to set host URL the following env variable may be set:
```json
{
    "Kestrel__EndPoints__Http__Url": "http://*:5000"
}
```

### Environment variables ###

* *RESTART_ATTEMPTS_NUMBER* - number of restart attempts. If not set int.MaxValue is used.
* *RESTART_ATTEMPTS_INTERVAL_MS* - interval between restarts in milliseconds. If not set 10000 is used.
* *SettingsUrl* - defines URL of remote settings or path for local settings.

### Settings ###

Settings schema is:
<!-- MARKDOWN-AUTO-DOCS:START (CODE:src=./template.json) -->
<!-- The below code snippet is automatically added from ./template.json -->
```json
{
  "APPINSIGHTS_INSTRUMENTATIONKEY": "String",
  "APP_UID": "Integer",
  "ASPNETCORE_ENVIRONMENT": "String",
  "Assets": {
    "ApiKey": "String",
    "CacheExpirationPeriod": "DateTime",
    "ServiceUrl": "String"
  },
  "ENVIRONMENT": "String",
  "ENV_INFO": "String",
  "Kestrel": {
    "EndPoints": {
      "Http": {
        "Url": "String"
      }
    }
  },
  "MtCandlesHistory": {
    "AssetsCache": {
      "ApiKey": "String",
      "ExpirationPeriod": "DateTime"
    },
    "Db": {
      "LogsConnectionString": "String",
      "SnapshotsConnectionString": "String",
      "StorageMode": "String"
    },
    "MaxCandlesCountWhichCanBeRequested": "Integer",
    "UseSerilog": "Boolean"
  },
  "RedisSettings": {
    "Configuration": "String"
  },
  "serilog": {
    "minimumLevel": {
      "default": "String"
    }
  },
  "TZ": "String"
}
```
<!-- MARKDOWN-AUTO-DOCS:END -->
