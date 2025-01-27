using Dalamud.Configuration;
using Dalamud.Plugin;
using ECommons.DalamudServices;
using ItemDisplay.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace ItemDisplay;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public Dictionary<ulong, List<ItemDisplayModel>> ItemDisplays = new();

    public bool ShowDisplay { get; set; } = true;
    public bool MoveMode { get; set; } = false;
    public float TextScale { get; set; } = 1.3f;


    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Svc.PluginInterface.SavePluginConfig(this);
    }

    public static Configuration Load()
    {
        try
        {
            var contents = File.ReadAllText(Svc.PluginInterface.ConfigFile.FullName);
            var json = JObject.Parse(contents);
            var version = (int?)json["Version"] ?? 0;
            return json.ToObject<Configuration>() ?? new();
        }
        catch (Exception e)
        {
            Svc.Log.Error($"Failed to load config from {Svc.PluginInterface.ConfigFile.FullName}: {e}");
            return new();
        }
    }
}
