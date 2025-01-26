using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.DalamudServices;
using Dalamud.Interface.Style;
using ItemDisplay.UI;
using Dalamud.Game.Command;
using ItemDisplay.Service;
using System.Collections.Generic;
using ItemDisplay.Model;
using System.Threading.Tasks;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using System.Linq;
using Microsoft.VisualBasic;
using OtterGui.Classes;
using System;
using Dalamud.Interface.FontIdentifier;
using ImGuiScene;
using static FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Delegates;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.CharaView.Delegates;
using Dalamud.Interface.Textures.TextureWraps;

namespace ItemDisplay;

public sealed class ItemDisplay : IDalamudPlugin
{
    public string Name => "ItemDisplay";
    private const string ItemDisplayCommand = "/itemdisplay";

    internal static ItemDisplay P = null;
    internal SettingsUI PluginUi;
    internal WindowSystem ws;
    internal Configuration Config;

    internal Dictionary<uint, ItemDisplayUI> DisplayUIs = new();

    internal TextureCache Icons;
    internal StyleModel Style;

    public ItemDisplay(IDalamudPluginInterface pluginInterface)
    {
        ECommonsMain.Init(pluginInterface, this, Module.All);

        P = this;
        P.Config = Configuration.Load();
        
        LuminaService.Init();

        ws = new();
        Config = P.Config;
        PluginUi = new();

        Icons = new(Svc.Data, Svc.Texture);

        Svc.Commands.AddHandler(ItemDisplayCommand, new CommandInfo(DrawSettingsUICmd)
        {
            HelpMessage = "Opens the ItemDisplay settings.\n",
            ShowInHelp = true,
        });

        Svc.PluginInterface.UiBuilder.Draw += ws.Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi += DrawSettingsUI;
        Svc.GameInventory.InventoryChangedRaw += InventoryChanged;
        
        Svc.ClientState.Logout += PlayerLoggedOut;
        Svc.ClientState.Login += PlayerLoggedIn;

        Style = StyleModel.GetFromCurrent()!;
        Task.Run(() => LoadItems());
    }

    private void PlayerLoggedOut(int type, int code)
    {
        foreach (var item in DisplayUIs)
        {
            item.Value.Dispose();
            DisplayUIs.Remove(item);
        }
    }

    private void PlayerLoggedIn()
    {
        Task.Run(() => LoadItems());
    }

    public void Dispose()
    {
        PluginUi.Dispose();

        GenericHelpers.Safe(() => Svc.Commands.RemoveHandler(ItemDisplayCommand));

        Svc.PluginInterface.UiBuilder.Draw -= ws.Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= DrawSettingsUI;
        Svc.GameInventory.InventoryChangedRaw -= InventoryChanged;

        Svc.ClientState.Logout -= PlayerLoggedOut;

        foreach(var item in DisplayUIs)
        {
            item.Value.Dispose();
            DisplayUIs.Remove(item);
        }

        //GenericHelpers.Safe(NativeController.Dispose);

        ws?.RemoveAllWindows();
        ws = null!;

        ECommonsMain.Dispose();
        P = null!;
    }

    private void DrawSettingsUICmd(string command, string args) => DrawSettingsUI();
    private void DrawSettingsUI()
    {
        PluginUi.IsOpen = true;
    }

    public void InventoryChanged(IReadOnlyCollection<InventoryEventArgs> events)
    {
        if (P.Config.ItemDisplays.TryGetValue(Svc.ClientState.LocalContentId, out var itemDisplays))
        {
            if (itemDisplays == null) return;

            if (events.Any(x =>
            x.Item.ContainerType == Dalamud.Game.Inventory.GameInventoryType.Inventory1 ||
            x.Item.ContainerType == Dalamud.Game.Inventory.GameInventoryType.Inventory2 ||
            x.Item.ContainerType == Dalamud.Game.Inventory.GameInventoryType.Inventory3 ||
            x.Item.ContainerType == Dalamud.Game.Inventory.GameInventoryType.Inventory4 ||
            x.Item.ContainerType == Dalamud.Game.Inventory.GameInventoryType.SaddleBag1 ||
            x.Item.ContainerType == Dalamud.Game.Inventory.GameInventoryType.SaddleBag2 ||
            x.Item.ContainerType == Dalamud.Game.Inventory.GameInventoryType.PremiumSaddleBag1 ||
            x.Item.ContainerType == Dalamud.Game.Inventory.GameInventoryType.PremiumSaddleBag2))
            {
                var intersectList = itemDisplays
                    .Select(x => x.ItemId)
                    .Intersect(events.Select(x => x.Item.ItemId));
                if (intersectList.Any())
                {
                    foreach (var item in intersectList)
                    {
                        Task.Run(() => UpdateItemCount(item));
                    }
                }
            }
        }
    }

    public async Task LoadItems()
    {
        if (P.Config.ItemDisplays.TryGetValue(Svc.ClientState.LocalContentId, out var itemDisplays))
        {
            if (itemDisplays == null) itemDisplays = new();
            foreach (var item in itemDisplays)
            {
                Svc.Log.Info($"Fetching count for {item.ItemName}");
                var itemCount = await InventoryService.GetItemCount(item.ItemId);
                Svc.Log.Info($"Count: {itemCount}");
                item.ItemCount = itemCount;

                await AddItemUI(item);
            }

            P.Config.ItemDisplays[Svc.ClientState.LocalContentId] = itemDisplays;
            P.Config.Save();
        }
    }

    public async Task AddItem(ItemDisplayModel model)
    {
        P.Config.ItemDisplays.TryGetValue(Svc.ClientState.LocalContentId, out var itemDisplays);
        if (itemDisplays == null) itemDisplays = new();
          
        Svc.Log.Info($"Fetching count for {model.ItemName}");
        model.ItemCount = await InventoryService.GetItemCount(model.ItemId);
        Svc.Log.Info($"Count: {model.ItemCount}");

        itemDisplays.Add(model);
        await AddItemUI(model);

        P.Config.ItemDisplays[Svc.ClientState.LocalContentId] = itemDisplays;
        P.Config.Save();
    }

    public async Task RemoveItem(uint itemId)
    {
        if (P.Config.ItemDisplays.TryGetValue(Svc.ClientState.LocalContentId, out var itemDisplays))
        {
            if (itemDisplays == null) itemDisplays = new();

            var model = itemDisplays.FirstOrDefault(x => x.ItemId == itemId);
            if (model != null)
            {
                itemDisplays.Remove(model);
                P.Config.ItemDisplays[Svc.ClientState.LocalContentId] = itemDisplays;
                P.Config.Save();

                await RemoveItemUI(model);
            }
        }
    }

    public async Task UpdateItemCount(uint itemId)
    {
        if (P.Config.ItemDisplays.TryGetValue(Svc.ClientState.LocalContentId, out var itemDisplays))
        {
            if (itemDisplays == null) itemDisplays = new();

            var item = itemDisplays.Where(x => x.ItemId == itemId).FirstOrDefault();
            if (item != null)
            {
                Svc.Log.Info($"Fetching count for {item.ItemName}");
                item.ItemCount = await InventoryService.GetItemCount(item.ItemId);
                Svc.Log.Info($"Count: {item.ItemCount}");

                if (DisplayUIs.TryGetValue(itemId, out var itemDisplay))
                {
                    itemDisplay.UpdateItemModel(item);
                }
            }

            P.Config.ItemDisplays[Svc.ClientState.LocalContentId] = itemDisplays;
            P.Config.Save();            
        }
    }

    public async Task AddItemUI(ItemDisplayModel model)
    {
        if (!DisplayUIs.ContainsKey(model.ItemId))
        {
            var newUi = new ItemDisplayUI(model);
            DisplayUIs.Add(model.ItemId, newUi);
        }  
    }

    public async Task RemoveItemUI(ItemDisplayModel model)
    {
        Svc.Log.Info($"Removing Item Display for {model.ItemName}");
        if (DisplayUIs.TryGetValue(model.ItemId, out var itemDisplay))
        {
            itemDisplay.Dispose();
            DisplayUIs.Remove(model.ItemId);
        }
    }
}
