using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using GATEWAYCore.Infrastructure.Configs;
using ConfigValueTypeLocal = GATEWAYCore.Infrastructure.Configs.ConfigValueType;

namespace GATEWAYCore.Service;

/// <summary>
/// 設定のgRPCサービス実装クラス
/// </summary>
public sealed class ConfigsGrpcService : ConfigsService.ConfigsServiceBase
{
    private readonly ConfigsManager _configsManager;
    private readonly ILogger<ConfigsGrpcService> _logger;

    public ConfigsGrpcService(ConfigsManager configsManager, ILogger<ConfigsGrpcService> logger)
    {
        _configsManager = configsManager ?? throw new ArgumentNullException(nameof(configsManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 設定値を取得する
    /// </summary>
    public override Task<GetConfigResponse> GetConfig(GetConfigRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var configKey = (ConfigKey)request.Key;
            var value = _configsManager.GetConfig(configKey);
            var definition = ConfigDefinitions.GetByEnumKey(configKey);

            if (definition == null)
            {
                return Task.FromResult(new GetConfigResponse
                {
                    Found = false,
                    Error = $"設定キーが見つかりません: {configKey}"
                });
            }

            var response = new GetConfigResponse
            {
                Found = true,
                Value = value,
                ValueType = ConvertToProtoValueType(definition.ValueType)
            };

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting config for key {Key}", request.Key);
            return Task.FromResult(new GetConfigResponse
            {
                Found = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// 指定された設定キーの値と範囲情報を取得する
    /// </summary>
    public override Task<GetConfigRangeResponse> GetConfigRange(GetConfigRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var configKey = (ConfigKey)request.Key;
            var value = _configsManager.GetConfig(configKey);
            var definition = ConfigDefinitions.GetByEnumKey(configKey);

            if (definition == null)
            {
                return Task.FromResult(new GetConfigRangeResponse
                {
                    Found = false,
                    Error = $"設定キーが見つかりません: {configKey}"
                });
            }

            var response = new GetConfigRangeResponse
            {
                Found = true,
                MinValue = definition.ValueMin ?? 0,
                MaxValue = definition.ValueMax ?? 0,
                Value = value,
                ValueType = ConvertToProtoValueType(definition.ValueType)
            };

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting config range");
            return Task.FromResult(new GetConfigRangeResponse
            {
                Found = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// 設定値を設定する（メモリのみ）
    /// </summary>
    public override Task<SetConfigResponse> SetConfig(SetConfigRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var configKey = (ConfigKey)request.Key;
            var (success, error) = _configsManager.SetConfig(configKey, request.Value);

            var response = new SetConfigResponse
            {
                Success = success,
                Error = error ?? "",
                Value = request.Value
            };

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting config for key {Key}", request.Key);
            return Task.FromResult(new SetConfigResponse
            {
                Success = false,
                Error = ex.Message,
                Value = ""
            });
        }
    }

    /// <summary>
    /// メモリ上の設定をファイルに保存する
    /// </summary>
    public override async Task<SaveConfigResponse> SaveConfig(Empty request, ServerCallContext context)
    {
        try
        {
            await _configsManager.SaveConfig();
            _logger.LogInformation("Config saved successfully");

            return new SaveConfigResponse
            {
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving config");
            return new SaveConfigResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// 設定値を設定して保存する
    /// </summary>
    public override async Task<SetConfigResponse> SetSaveConfig(SetConfigRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var configKey = (ConfigKey)request.Key;
            var (success, error) = await _configsManager.SetSaveConfig(configKey, request.Value);

            var response = new SetConfigResponse
            {
                Success = success,
                Error = error ?? "",
                Value = request.Value
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting and saving config");
            return new SetConfigResponse
            {
                Success = false,
                Error = ex.Message,
                Value = ""
            };
        }
    }

    /// <summary>
    /// すべての設定値を取得する
    /// </summary>
    public override Task<GetAllConfigsResponse> GetAllConfigs(Empty request, ServerCallContext context)
    {
        try
        {
            var response = new GetAllConfigsResponse();

            foreach (var definition in ConfigDefinitions.All)
            {
                var value = _configsManager.GetConfig(definition.EnumKey);

                var configItem = new ConfigItem
                {
                    Key = (int)definition.EnumKey,
                    Value = value,
                    ValueType = ConvertToProtoValueType(definition.ValueType)
                };

                response.Configs.Add(configItem);
            }

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all configs");
            var response = new GetAllConfigsResponse();
            response.Error = ex.Message;
            return Task.FromResult(response);
        }
    }

    /// <summary>
    /// 複数の設定値をバッチで設定する
    /// </summary>
    public override Task<SetConfigsBatchResponse> SetConfigsBatch(SetConfigsBatchRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = new SetConfigsBatchResponse();

        try
        {
            foreach (var item in request.Updates)
            {
                var configKey = (ConfigKey)item.Key;
                var (success, error) = _configsManager.SetConfig(configKey, item.Value);

                response.Results.Add(new ConfigSetResult
                {
                    Key = item.Key,
                    Success = success,
                    Error = error ?? ""
                });
            }

            response.Success = response.Results.All(r => r.Success);

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch setting configs");
            response.Success = false;
            response.Error = ex.Message;
            return Task.FromResult(response);
        }
    }

    /// <summary>
    /// 設定値を検証する
    /// </summary>
    public override Task<ValidateConfigResponse> ValidateConfig(ValidateConfigRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var configKey = (ConfigKey)request.Key;
            var definition = ConfigDefinitions.GetByEnumKey(configKey);

            if (definition == null)
            {
                return Task.FromResult(new ValidateConfigResponse
                {
                    IsValid = false,
                    ErrorMessage = $"設定キーが見つかりません: {configKey}"
                });
            }

            var (isValid, errorMessage) = definition.ValidateValue(request.Value);

            return Task.FromResult(new ValidateConfigResponse
            {
                IsValid = isValid,
                ErrorMessage = errorMessage ?? ""
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating config");
            return Task.FromResult(new ValidateConfigResponse
            {
                IsValid = false,
                ErrorMessage = ex.Message
            });
        }
    }

    /// <summary>
    /// 設定値をリセットする
    /// </summary>
    public override async Task<ResetConfigResponse> ResetConfig(ResetConfigRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var configKey = (ConfigKey)request.Key;
            var definition = ConfigDefinitions.GetByEnumKey(configKey);

            if (definition == null)
            {
                return new ResetConfigResponse
                {
                    Success = false,
                    Error = $"設定キーが見つかりません: {configKey}"
                };
            }

            var (success, error) = _configsManager.SetConfig(configKey, definition.DefaultValue);

            if (success)
            {
                await _configsManager.SaveConfig();
            }

            return new ResetConfigResponse
            {
                Success = success,
                Error = error ?? "",
                DefaultValue = definition.DefaultValue
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting config");
            return new ResetConfigResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// すべての設定値をリセットする
    /// </summary>
    public override async Task<ResetConfigResponse> ResetAllConfigs(Empty request, ServerCallContext context)
    {
        try
        {
            foreach (var definition in ConfigDefinitions.All)
            {
                _configsManager.SetConfig(definition.EnumKey, definition.DefaultValue);
            }

            await _configsManager.SaveConfig();

            return new ResetConfigResponse
            {
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting all configs");
            return new ResetConfigResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// ConfigValueType を Proto3 の ConfigValueType に変換する
    /// </summary>
    private static ConfigValueType ConvertToProtoValueType(ConfigValueTypeLocal valueType)
    {
        return valueType switch
        {
            ConfigValueTypeLocal.Hex => ConfigValueType.Hex,
            ConfigValueTypeLocal.Decimal => ConfigValueType.Decimal,
            ConfigValueTypeLocal.String => ConfigValueType.String,
            _ => (ConfigValueType)0
        };
    }
}
