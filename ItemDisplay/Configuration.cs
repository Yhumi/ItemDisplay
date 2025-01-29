using Dalamud.Configuration;
using Dalamud.Plugin;
using ECommons.DalamudServices;
using ItemDisplay.Model;
using ItemDisplay.Service;
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
    public int SelectorHeight { get; set; } = 33;


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

    public void Update()
    {
        if (Version == 0)
        {
            Svc.Log.Info($"Performing Migration to Config v1");
            Version += 1;
        }

        if (Version == 1)
        {
            Svc.Log.Info($"Performing Migration to Config v2");
            var disp = ItemDisplays;
            foreach (var item in disp)
            {
                foreach (var display in item.Value)
                {
                    if (display.IconId == 0 && display.ItemId != 0)
                        display.IconId = LuminaService.GetIconId(display.ItemId);

                    if (display.ItemId != 0)
                        display.Type = ItemDisplayType.Item;
                }
            }

            P.Config.ItemDisplays = disp;
            Version += 1;
        }

        Version = 2;
        P.Config.Save();
    }
}
