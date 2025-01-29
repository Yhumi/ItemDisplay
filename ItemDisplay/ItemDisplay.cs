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
using Dalamud.Game.Inventory;
using ECommons.EzEventManager;
using ImGuiNET;
using ECommons.ImGuiMethods;
using ECommons.Automation.LegacyTaskManager;

namespace ItemDisplay;

public sealed class ItemDisplay : IDalamudPlugin
{
    public string Name => "Icon Display";
    private const string ItemDisplayCommand = "/icondisplay";
    private int CurrentConfigVersion = 2;

    internal static ItemDisplay P = null;
    internal SettingsUI PluginUi;
    internal WindowSystem ws;
    internal Configuration Config;
    internal TaskManager TM;

    internal Dictionary<Guid, ItemDisplayUI> DisplayUIs = new();
    internal IconSelector Selector;

    internal Queue<ItemDisplayModel> ItemModelsForUpdate = new();
    internal bool UpdatingItem = false;

    internal TextureCache Icons;
    internal StyleModel Style;

    public ItemDisplay(IDalamudPluginInterface pluginInterface)
    {
        ECommonsMain.Init(pluginInterface, this, Module.All);

        P = this;
        P.Config = Configuration.Load();

        if (P.Config.Version < CurrentConfigVersion)
        {
            P.Config.Update();
        }
        
        LuminaService.Init();

        ws = new();
        Config = P.Config;
        PluginUi = new();
        Selector = new();

        TM = new() { AbortOnTimeout = true, TimeLimitMS = 20000 };
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

        Svc.ClientState.TerritoryChanged += TerritoryChange;

        //Svc.Framework.Update += FrameworkUpdate;

        Style = StyleModel.GetFromCurrent()!;
        Task.Run(() => LoadItems());
    }

    private void TerritoryChange(ushort e)
    {
        var instanceId = DutyService.GetCurrentInstanceId();
        if (P.Config.ItemDisplays.TryGetValue(Svc.ClientState.LocalContentId, out var itemDisplays))
        {
            foreach (var item in itemDisplays.Where(x => x.Instances.Count > 0))
            {
                if (instanceId != 0)
                {
                    if (item.Instances.Contains(instanceId))
                        P.TM.Enqueue(async () => await AddItemUI(item));
                    else
                        P.TM.Enqueue(async () => await RemoveItemUI(item));
                }
                else
                {
                    if (item.ShowDisplay)
                        P.TM.Enqueue(async () => await AddItemUI(item));
                    else
                        P.TM.Enqueue(async () => await RemoveItemUI(item));
                }
            }
        }
        Svc.Log.Info($"Current instance: {DutyService.GetCurrentInstanceId()}");
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
        Svc.ClientState.Login -= PlayerLoggedIn;

        ThreadLoadImageHandler.ClearAll();

        //Svc.Framework.Update -= FrameworkUpdate;

        foreach (var item in DisplayUIs)
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

    public async void InventoryChanged(IReadOnlyCollection<InventoryEventArgs> events)
    {
        if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas]) return;
        if (P.Config.ItemDisplays.TryGetValue(Svc.ClientState.LocalContentId, out var itemDisplays))
        {
            if (itemDisplays == null) return;

            var intersectList = itemDisplays.Where(x => events.Select(x => x.Item.ItemId).Contains(x.ItemId)).ToList();

            foreach (var item in intersectList)
            {
                Task.Run(() => UpdateAvailableItemCounts(item.Id));                  
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
                if (item.Type == ItemDisplayType.Item)
                {
                    Svc.Log.Info($"Fetching count for {item.ItemName}");
                    SetItemCountsOnModel(item);
                }

                var dutyId = DutyService.GetCurrentInstanceId();
                if (dutyId != 0)
                {
                    //In duty, display global ones.
                    if (item.ShowDisplay && item.Instances.Count == 0)
                    {
                        await AddItemUI(item);
                        continue;
                    }

                    //Display any for this duty regardless of showDisplay state
                    if (item.Instances.Contains(dutyId))
                    {
                        await AddItemUI(item);
                        continue;
                    }
                }
                else
                {
                    //Not in duty, only display enabled ones - we dont care what duty they're from
                    if (item.ShowDisplay)
                        await AddItemUI(item);
                }
            }

            P.Config.ItemDisplays[Svc.ClientState.LocalContentId] = itemDisplays;
            P.Config.Save();
        }
    }

    public async Task AddItem(ItemDisplayModel model)
    {
        P.Config.ItemDisplays.TryGetValue(Svc.ClientState.LocalContentId, out var itemDisplays);
        if (itemDisplays == null) itemDisplays = new();
          
        if (model.Type == ItemDisplayType.Item)
        {
            Svc.Log.Info($"Fetching count for {model.ItemName}"); 
            SetItemCountsOnModel(model);
            Svc.Log.Info($"Count: {model.ItemCount}");
        }

        itemDisplays.Add(model);
        await AddItemUI(model);

        P.Config.ItemDisplays[Svc.ClientState.LocalContentId] = itemDisplays;
        P.Config.Save();
    }

    public async Task RemoveItem(Guid id)
    {
        if (P.Config.ItemDisplays.TryGetValue(Svc.ClientState.LocalContentId, out var itemDisplays))
        {
            if (itemDisplays == null) itemDisplays = new();

            var model = itemDisplays.FirstOrDefault(x => x.Id == id);
            if (model != null)
            {
                itemDisplays.Remove(model);
                P.Config.ItemDisplays[Svc.ClientState.LocalContentId] = itemDisplays;
                P.Config.Save();

                await RemoveItemUI(model);
            }
        }
    }

    public async Task UpdateAvailableItemCounts(Guid id)
    {
        if (P.Config.ItemDisplays.TryGetValue(Svc.ClientState.LocalContentId, out var itemDisplays))
        {
            if (itemDisplays == null) itemDisplays = new();

            var item = itemDisplays.Where(x => x.Id == id).FirstOrDefault();
            bool fullyUpdated = true;
            if (item != null)
            {
                fullyUpdated = SetItemCountsOnModel(item);

                Svc.Log.Info($"Count: {item.ItemCount}");

                if (DisplayUIs.TryGetValue(id, out var itemDisplay))
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
        if (!DisplayUIs.ContainsKey(model.Id))
        {
            Svc.Log.Info($"Adding Item Display for {model.Id}");
            var newUi = new ItemDisplayUI(model);
            DisplayUIs.Add(model.Id, newUi);
        }

        Svc.Log.Debug($"DisplayUI count: {DisplayUIs.Count}");
    }

    public async Task UpdateMoveMode(bool moveMode)
    {
        P.Config.MoveMode = moveMode;
        P.Config.Save();

        Svc.Log.Debug($"Move mode: {moveMode}");

        foreach (var ui in DisplayUIs)
        {
            ui.Value.UpdateMoveMode();
        }
    }

    public async Task RemoveItemUI(ItemDisplayModel model)
    {
        if (DisplayUIs.TryGetValue(model.Id, out var itemDisplay))
        {
            Svc.Log.Info($"Removing Item Display for {model.Id}");
            itemDisplay.Dispose();
            DisplayUIs.Remove(model.Id);
        }
        Svc.Log.Debug($"DisplayUI count: {DisplayUIs.Count}");
    }

    public async Task CleanupDisplayList()
    {
        P.Config.ItemDisplays.TryGetValue(Svc.ClientState.LocalContentId, out var itemDisplays);
        if (itemDisplays == null) return;

        foreach (var item in itemDisplays)
        {
            if (P.Config.ShowDisplay && item.ShowDisplay && !DisplayUIs.ContainsKey(item.Id))
                Task.Run(() => AddItemUI(item));

            if ((!P.Config.ShowDisplay || !item.ShowDisplay) && DisplayUIs.ContainsKey(item.Id))
                Task.Run(() =>  RemoveItemUI(item));
        }
    }

    public bool SetItemCountsOnModel(ItemDisplayModel model)
    {
        bool fullyUpdated = InventoryService.InventoryAccess() && InventoryService.SaddlebagAccess();

        if (InventoryService.InventoryAccess())
            model.InventoryCount = InventoryService.GetInventoryItemCount(model.ItemId);

        if (InventoryService.SaddlebagAccess())
            model.SaddlebagCount = InventoryService.GetSaddlebagItemCount(model.ItemId);

        return fullyUpdated;
    }
}
