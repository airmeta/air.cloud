using Air.Cloud.Modules.ReverseTunnel.Common.Options;

using Microsoft.Extensions.Configuration;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

internal static class ReverseTunnelOptionsCloner
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static ReverseTunnelOptions CloneAndNormalize(ReverseTunnelOptions options)
    {
        var json = JsonSerializer.Serialize(options ?? new ReverseTunnelOptions(), SerializerOptions);
        var clone = JsonSerializer.Deserialize<ReverseTunnelOptions>(json, SerializerOptions) ?? new ReverseTunnelOptions();
        clone.PostConfigure(clone, new ConfigurationBuilder().Build());
        return clone;
    }

    public static string Serialize(object value)
    {
        return JsonSerializer.Serialize(value, SerializerOptions);
    }

    public static ReverseTunnelOptions? DeserializeOptions(Stream stream)
    {
        return JsonSerializer.Deserialize<ReverseTunnelOptions>(stream, SerializerOptions);
    }
}
