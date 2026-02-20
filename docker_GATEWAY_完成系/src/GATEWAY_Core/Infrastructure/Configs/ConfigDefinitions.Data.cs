namespace GATEWAYCore.Infrastructure.Configs;

public static partial class ConfigDefinitions
{
    public static readonly IReadOnlyList<ConfigDefinition> All = new[]
    {
        new ConfigDefinition
        {
            Item = "設定ファイルバージョン",
            Key = "FileVersion",
            EnumKey = ConfigKey.FileVersion,
            ValueType = ConfigValueType.String,
            Value = "0.0.3",
            ValueMin = null,
            ValueMax = null,
            Target = ConfigTarget.Both
        },

        new ConfigDefinition
        {
            Item = "eth0 IPAddress",
            Key = "Eth0Ip",
            EnumKey = ConfigKey.Eth0Ip,
            ValueType = ConfigValueType.Hex,
            Value = 0xC0A80101,
            ValueMin = 0x00000000,
            ValueMax = 0xFFFFFFFF,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "eth0 Subnet mask",
            Key = "Eth0Mask",
            EnumKey = ConfigKey.Eth0Mask,
            ValueType = ConfigValueType.Hex,
            Value = 0xFFFFFF00,
            ValueMin = 0x00000000,
            ValueMax = 0xFFFFFFFF,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "eth0 DHCP Enabled",
            Key = "Eth0Dhcp",
            EnumKey = ConfigKey.Eth0Dhcp,
            ValueType = ConfigValueType.Decimal,
            Value = 0,
            ValueMin = 0,
            ValueMax = 1,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "eth1 IPAddress",
            Key = "Eth1Ip",
            EnumKey = ConfigKey.Eth1Ip,
            ValueType = ConfigValueType.Hex,
            Value = 0xC0A80102,
            ValueMin = 0x00000000,
            ValueMax = 0xFFFFFFFF,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "eth1 Subnet mask",
            Key = "Eth1Mask",
            EnumKey = ConfigKey.Eth1Mask,
            ValueType = ConfigValueType.Hex,
            Value = 0xFFFFFF00,
            ValueMin = 0x00000000,
            ValueMax = 0xFFFFFFFF,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "eth1 DHCP Enabled",
            Key = "Eth1Dhcp",
            EnumKey = ConfigKey.Eth1Dhcp,
            ValueType = ConfigValueType.Decimal,
            Value = 0,
            ValueMin = 0,
            ValueMax = 1,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "eth2 IPAddress",
            Key = "Eth2Ip",
            EnumKey = ConfigKey.Eth2Ip,
            ValueType = ConfigValueType.Hex,
            Value = 0xC0A80103,
            ValueMin = 0x00000000,
            ValueMax = 0xFFFFFFFF,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "eth2 Subnet mask",
            Key = "Eth2Mask",
            EnumKey = ConfigKey.Eth2Mask,
            ValueType = ConfigValueType.Hex,
            Value = 0xFFFFFF00,
            ValueMin = 0x00000000,
            ValueMax = 0xFFFFFFFF,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "eth2 DHCP Enabled",
            Key = "Eth2Dhcp",
            EnumKey = ConfigKey.Eth2Dhcp,
            ValueType = ConfigValueType.Decimal,
            Value = 0,
            ValueMin = 0,
            ValueMax = 1,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "eth gateway",
            Key = "EthGateway",
            EnumKey = ConfigKey.EthGateway,
            ValueType = ConfigValueType.Hex,
            Value = 0xC0A801FE,
            ValueMin = 0x00000000,
            ValueMax = 0xFFFFFFFF,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "eth 優先1",
            Key = "EthDnsFirst",
            EnumKey = ConfigKey.EthDnsFirst,
            ValueType = ConfigValueType.Hex,
            Value = 0xC0A801FE,
            ValueMin = 0x00000000,
            ValueMax = 0xFFFFFFFF,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "eth 優先2",
            Key = "EthDnsSecond",
            EnumKey = ConfigKey.EthDnsSecond,
            ValueType = ConfigValueType.Hex,
            Value = 0x00000000,
            ValueMin = 0x00000000,
            ValueMax = 0xFFFFFFFF,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "eth 終了時無効",
            Key = "ComEthEndDisable",
            EnumKey = ConfigKey.ComEthEndDisable,
            ValueType = ConfigValueType.Decimal,
            Value = 1,
            ValueMin = 0,
            ValueMax = 1,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "eth 有効化待ち時間",
            Key = "ComEthWaitEnable",
            EnumKey = ConfigKey.ComEthWaitEnable,
            ValueType = ConfigValueType.Decimal,
            Value = 3000,
            ValueMin = 0,
            ValueMax = 20000,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "eth 変更後待ち時間",
            Key = "ComEthWaitChange",
            EnumKey = ConfigKey.ComEthWaitChange,
            ValueType = ConfigValueType.Decimal,
            Value = 3000,
            ValueMin = 0,
            ValueMax = 20000,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "プロキシ有効 (0: 無効, 1: 有効)",
            Key = "ProxyEnable",
            EnumKey = ConfigKey.ProxyEnable,
            ValueType = ConfigValueType.Decimal,
            Value = 1,
            ValueMin = 0,
            ValueMax = 1,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "プロキシアドレス",
            Key = "ProxyAddress",
            EnumKey = ConfigKey.ProxyAddress,
            ValueType = ConfigValueType.String,
            Value = "",
            ValueMin = null,
            ValueMax = null,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "タイムゾーン",
            Key = "TimeZone",
            EnumKey = ConfigKey.TimeZone,
            ValueType = ConfigValueType.String,
            Value = "Asia/Tokyo",
            ValueMin = null,
            ValueMax = null,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "NTP サーバー機能有効設定 (0: 無効, 1: 有効)",
            Key = "NtpEnable",
            EnumKey = ConfigKey.NtpEnable,
            ValueType = ConfigValueType.Decimal,
            Value = 0,
            ValueMin = 0,
            ValueMax = 1,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "NTP サーバーURL",
            Key = "NtpUrl",
            EnumKey = ConfigKey.NtpUrl,
            ValueType = ConfigValueType.String,
            Value = "",
            ValueMin = null,
            ValueMax = null,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "UPS開始待ち時間[s]",
            Key = "UpsStartWait",
            EnumKey = ConfigKey.UpsStartWait,
            ValueType = ConfigValueType.Decimal,
            Value = 10,
            ValueMin = 0,
            ValueMax = 864000,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "UPS状態監視周期[s]",
            Key = "UpsWatchInterval",
            EnumKey = ConfigKey.UpsWatchInterval,
            ValueType = ConfigValueType.Decimal,
            Value = 300,
            ValueMin = 0,
            ValueMax = 864000,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "UPS有効設定 (0:無効|1:有効)",
            Key = "UpsEnable",
            EnumKey = ConfigKey.UpsEnable,
            ValueType = ConfigValueType.Decimal,
            Value = 0,
            ValueMin = 0,
            ValueMax = 1,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "UPS選択",
            Key = "UpsSelect",
            EnumKey = ConfigKey.UpsSelect,
            ValueType = ConfigValueType.String,
            Value = "",
            ValueMin = null,
            ValueMax = null,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "UPS接続方法リスト",
            Key = "UpsConnectList",
            EnumKey = ConfigKey.UpsConnectList,
            ValueType = ConfigValueType.String,
            Value = "COMx,USB",
            ValueMin = null,
            ValueMax = null,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "UPS監視OSシャットダウン待ち時間[s]",
            Key = "UpsWaitOsShutdown",
            EnumKey = ConfigKey.UpsWaitOsShutdown,
            ValueType = ConfigValueType.Decimal,
            Value = 60,
            ValueMin = 0,
            ValueMax = 2147483647,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "UPS監視UPS電源待ち時間[s]",
            Key = "UpsWaitUpsPoweroff",
            EnumKey = ConfigKey.UpsWaitUpsPoweroff,
            ValueType = ConfigValueType.Decimal,
            Value = 180,
            ValueMin = 0,
            ValueMax = 2147483647,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "UPS設定ファイルパス（APC)",
            Key = "UpsNutConfFilepath",
            EnumKey = ConfigKey.UpsNutConfFilepath,
            ValueType = ConfigValueType.String,
            Value = "/etc/nut/ups.conf",
            ValueMin = null,
            ValueMax = null,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "パスワード文字数下限",
            Key = "OsPasswdLenMin",
            EnumKey = ConfigKey.OsPasswdLenMin,
            ValueType = ConfigValueType.Decimal,
            Value = 1,
            ValueMin = 0,
            ValueMax = 127,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "パスワード文字数上限",
            Key = "OsPasswdLenMax",
            EnumKey = ConfigKey.OsPasswdLenMax,
            ValueType = ConfigValueType.Decimal,
            Value = 127,
            ValueMin = 0,
            ValueMax = 127,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "パスワード文字",
            Key = "WindowsPasswdChar",
            EnumKey = ConfigKey.WindowsPasswdChar,
            ValueType = ConfigValueType.String,
            Value = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ`~!@#$%^&()_-+={}[]|:;\"'<>,. ?",
            ValueMin = null,
            ValueMax = null,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "ユーザー初期パスワード",
            Key = "UserInitPasswd",
            EnumKey = ConfigKey.UserInitPasswd,
            ValueType = ConfigValueType.String,
            Value = "",
            ValueMin = null,
            ValueMax = null,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "コマンド実行タイムアウト",
            Key = "CmdexecTimeout",
            EnumKey = ConfigKey.CmdexecTimeout,
            ValueType = ConfigValueType.Decimal,
            Value = 5000,
            ValueMin = 0,
            ValueMax = 2147483647,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "Zabbixエージェント設定（0:無効、1:01を有効）",
            Key = "ZabbixServiceAgentEnable",
            EnumKey = ConfigKey.ZabbixServiceAgentEnable,
            ValueType = ConfigValueType.Decimal,
            Value = 0,
            ValueMin = 0,
            ValueMax = 10,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "Zabbixエージェント設定ファイルパス",
            Key = "ZabbixService01ConfFilepath",
            EnumKey = ConfigKey.ZabbixService01ConfFilepath,
            ValueType = ConfigValueType.String,
            Value = "/etc/zabbix/zabbix_agentd.conf",
            ValueMin = null,
            ValueMax = null,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "Zabbixエージェント設定 Server=, ServerActive=",
            Key = "ZabbixService01ConfValAddress",
            EnumKey = ConfigKey.ZabbixService01ConfValAddress,
            ValueType = ConfigValueType.String,
            Value = "127.0.0.1",
            ValueMin = null,
            ValueMax = null,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "ネットワークポート範囲確認",
            Key = "NetworkPortRange",
            EnumKey = ConfigKey.NetworkPortRange,
            ValueType = ConfigValueType.String,
            Value = "1",
            ValueMin = null,
            ValueMax = 65535,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "ポートフォワーディング設定",
            Key = "PortForwardingList",
            EnumKey = ConfigKey.PortForwardingList,
            ValueType = ConfigValueType.String,
            Value = "",
            ValueMin = null,
            ValueMax = null,
            Target = ConfigTarget.User
        },

        new ConfigDefinition
        {
            Item = "ポートフォワーディング設定数最大",
            Key = "PortForwardingMax",
            EnumKey = ConfigKey.PortForwardingMax,
            ValueType = ConfigValueType.Decimal,
            Value = 20,
            ValueMin = 1,
            ValueMax = 65535,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "CPU温度監視有無（0:無効、1:有効）",
            Key = "CpuTempMonEnabled",
            EnumKey = ConfigKey.CpuTempMonEnabled,
            ValueType = ConfigValueType.Decimal,
            Value = 1,
            ValueMin = 0,
            ValueMax = 1,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "CPU温度取得・判定周期[S]",
            Key = "CpuTempInterval",
            EnumKey = ConfigKey.CpuTempInterval,
            ValueType = ConfigValueType.Decimal,
            Value = 60,
            ValueMin = 0,
            ValueMax = 864000,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "CPU温度超過警告閾値[℃]",
            Key = "CpuTemphThresholdWarn",
            EnumKey = ConfigKey.CpuTemphThresholdWarn,
            ValueType = ConfigValueType.Decimal,
            Value = 70,
            ValueMin = 0,
            ValueMax = 100,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "CPU温度超過警告ヒステリシス値[℃]",
            Key = "CpuTempHysteresisWarn",
            EnumKey = ConfigKey.CpuTempHysteresisWarn,
            ValueType = ConfigValueType.Decimal,
            Value = 5,
            ValueMin = 0,
            ValueMax = 100,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "CPU温度超過エラー閾値[℃]",
            Key = "CpuTempThresholdError",
            EnumKey = ConfigKey.CpuTempThresholdError,
            ValueType = ConfigValueType.Decimal,
            Value = 80,
            ValueMin = 0,
            ValueMax = 100,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "CPU温度超過エラーヒステリシス値[℃]",
            Key = "CpuTempHysteresisError",
            EnumKey = ConfigKey.CpuTempHysteresisError,
            ValueType = ConfigValueType.Decimal,
            Value = 5,
            ValueMin = 0,
            ValueMax = 100,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "Disk使用率監視有無（0:無効、1:有効）",
            Key = "DiskUsageMonEnabled",
            EnumKey = ConfigKey.DiskUsageMonEnabled,
            ValueType = ConfigValueType.Decimal,
            Value = 1,
            ValueMin = 0,
            ValueMax = 1,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "Disk使用率取得・判定周期[S]",
            Key = "DiskUsageInterval",
            EnumKey = ConfigKey.DiskUsageInterval,
            ValueType = ConfigValueType.Decimal,
            Value = 300,
            ValueMin = 1,
            ValueMax = 864000,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "Disk使用率超過警告閾値[％]",
            Key = "DiskUsagehThresholdWarn",
            EnumKey = ConfigKey.DiskUsagehThresholdWarn,
            ValueType = ConfigValueType.Decimal,
            Value = 90,
            ValueMin = 1,
            ValueMax = 100,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "Disk使用率超過警告ヒステリシス値[％]",
            Key = "DiskUsageHysteresisWarn",
            EnumKey = ConfigKey.DiskUsageHysteresisWarn,
            ValueType = ConfigValueType.Decimal,
            Value = 5,
            ValueMin = 1,
            ValueMax = 100,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "Disk使用率超過エラー閾値[％]",
            Key = "DiskUsageThresholdError",
            EnumKey = ConfigKey.DiskUsageThresholdError,
            ValueType = ConfigValueType.Decimal,
            Value = 98,
            ValueMin = 1,
            ValueMax = 100,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "Disk使用率超過エラーヒステリシス値[％]",
            Key = "DiskUsageHysteresisError",
            EnumKey = ConfigKey.DiskUsageHysteresisError,
            ValueType = ConfigValueType.Decimal,
            Value = 5,
            ValueMin = 1,
            ValueMax = 100,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "CPU使用率監視有無（0:無効、1:有効）",
            Key = "CpuUsageMonEnabled",
            EnumKey = ConfigKey.CpuUsageMonEnabled,
            ValueType = ConfigValueType.Decimal,
            Value = 1,
            ValueMin = 0,
            ValueMax = 1,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "CPU使用率取得・判定周期[S]",
            Key = "CpuUsageInterval",
            EnumKey = ConfigKey.CpuUsageInterval,
            ValueType = ConfigValueType.Decimal,
            Value = 1,
            ValueMin = 1,
            ValueMax = 864000,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "CPU使用率監視連続超過カウント",
            Key = "CpuUsageDetectCount",
            EnumKey = ConfigKey.CpuUsageDetectCount,
            ValueType = ConfigValueType.Decimal,
            Value = 3,
            ValueMin = 1,
            ValueMax = 864000,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "CPU使用率超過警告閾値[％]",
            Key = "CpuUsageThresholdWarn",
            EnumKey = ConfigKey.CpuUsageThresholdWarn,
            ValueType = ConfigValueType.Decimal,
            Value = 75,
            ValueMin = 1,
            ValueMax = 100,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "CPU使用率超過警告ヒステリシス値[％]",
            Key = "CpuUsageHysteresisWarn",
            EnumKey = ConfigKey.CpuUsageHysteresisWarn,
            ValueType = ConfigValueType.Decimal,
            Value = 10,
            ValueMin = 1,
            ValueMax = 100,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "CPU使用率超過エラー閾値[％]",
            Key = "CpuUsageThresholdError",
            EnumKey = ConfigKey.CpuUsageThresholdError,
            ValueType = ConfigValueType.Decimal,
            Value = 90,
            ValueMin = 1,
            ValueMax = 100,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "CPU使用率超過エラーヒステリシス値[％]",
            Key = "CpuUsageHysteresisError",
            EnumKey = ConfigKey.CpuUsageHysteresisError,
            ValueType = ConfigValueType.Decimal,
            Value = 10,
            ValueMin = 1,
            ValueMax = 100,
            Target = ConfigTarget.System
        },

        new ConfigDefinition
        {
            Item = "ハードウェア情報モニタ表示周期[s]",
            Key = "HardwareInfoDisplayInterval",
            EnumKey = ConfigKey.HardwareInfoDisplayInterval,
            ValueType = ConfigValueType.Decimal,
            Value = 10,
            ValueMin = 1,
            ValueMax = 864000,
            Target = ConfigTarget.System
        },

    };
}
