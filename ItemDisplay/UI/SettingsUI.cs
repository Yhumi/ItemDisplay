using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ItemDisplay.Service;
using ImGuiNET;
using ItemDisplay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Interface.Components;
using static FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMiragePrismPrismBox.Delegates;
using OtterGui;
using System.Numerics;

namespace ItemDisplay.UI
{
    internal class SettingsUI : Window, IDisposable
    {
        private bool visible = false;

        private string ItemName = string.Empty;

        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        public SettingsUI() : base($"{P.Name} {P.GetType().Assembly.GetName().Version}###ItemDisplay")
        {
            this.RespectCloseHotkey = false;
            this.SizeConstraints = new()
            {
                MinimumSize = new(540, 400),
            };
            P.ws.AddWindow(this);
        }

        public void Dispose() 
        {
        }

        public async void CreateItemDisplay(string itemName)
        {
            var itemId = LuminaService.GetItemIdByItemName(itemName);
            Svc.Log.Info($"{itemName} id: {itemId}");
            if (itemId != 0)
            {
                var newItem = new ItemDisplayModel() { ItemId = itemId, ItemName = itemName };
                Task.Run(() => P.AddItem(newItem));
            }
            ItemName = string.Empty;
        }

        //public async void RemoveItemDisplay(string itemName)
        //{
        //    var itemId = LuminaService.GetItemIdByItemName(itemName);
        //    Svc.Log.Info($"{itemName} id: {itemId}");
        //    if (itemId != 0)
        //    {
        //        Task.Run(() => P.RemoveItem(item));
        //    }
        //    ItemName = string.Empty;
        //}

        public async void SetupItemDisplay(bool showDisplay)
        {
            P.Config.ShowDisplay = showDisplay;
            P.Config.Save();
        }

        public async void SetTextFloat(float textScale)
        {
            P.Config.TextScale = textScale;
            P.Config.Save();
        }

        public override async void Draw()
        {
            P.Config.ItemDisplays.TryGetValue(Svc.ClientState.LocalContentId, out var itemDisplays);
            if (itemDisplays == null) itemDisplays = new();

            bool ShowDisplay = P.Config.ShowDisplay;
            bool MoveMode = P.Config.MoveMode;

            if (ImGui.Checkbox("Show Display Windows", ref ShowDisplay))
            {
                Task.Run(() => SetupItemDisplay(ShowDisplay));
            }
            ImGuiComponents.HelpMarker($"Show/Hide all Item Displays.");

            if (ImGui.Checkbox("Enable ItemDisplay Movement", ref MoveMode))
            {
                Task.Run(() => P.UpdateMoveMode(MoveMode));
            }
            ImGuiComponents.HelpMarker($"Allow the clicking and dragging of Item Displays.");
            ImGui.TextWrapped("Note: Move mode will disable macro functionality while enabled.");

            float textScale = P.Config.TextScale;
            if (ImGui.SliderFloat("Text Scaling", ref textScale, 0.1f, 3f))
            {
                Task.Run(() => SetTextFloat(textScale));
            }

            ImGui.Separator();

            ImGui.TextWrapped("Add Item Display");
            ImGui.InputText("###ItemName", ref ItemName, 150);
            if (ImGui.Button("Add new Item Display"))
            {           
                Svc.Log.Info($"Adding item: {ItemName}");
                if (ItemName != string.Empty)
                {
                    Task.Run(() => CreateItemDisplay(ItemName));
                }
            }

            ImGui.Separator();

            foreach (var item in itemDisplays)
            {
                var itemCommand = item.TextCommand ?? string.Empty;
                var showDisplay = item.ShowDisplay;
                var showCount = item.ShowCount;
                var itemScale = item.Scale;
                var itemOpacity = item.Opacity;

                if (ImGui.CollapsingHeader($"{item.ItemName} ({item.Id})"))
                {
                    if (ImGui.Checkbox($"Show Display###Disp-{item.Id}", ref showDisplay))
                    {
                        item.ShowDisplay = showDisplay;

                        P.Config.ItemDisplays[Svc.ClientState.LocalContentId] = itemDisplays;
                        P.Config.Save();

                        Task.Run(() => P.UpdateAvailableItemCounts(item.Id));
                    }
                    ImGui.SameLine();
                    if (ImGui.Checkbox($"Show Count###Count-{item.Id}", ref showCount))
                    {
                        item.ShowCount = showCount;

                        P.Config.ItemDisplays[Svc.ClientState.LocalContentId] = itemDisplays;
                        P.Config.Save();

                        Task.Run(() => P.UpdateAvailableItemCounts(item.Id));
                    }
                    ImGui.SameLine(ImGui.GetWindowWidth() / 2);
                    if (ImGuiUtil.DrawDisabledButton($"Delete Set###{item.Id}-DeleteItemDisplay", default, "Delete Current Selection. Hold control while clicking.", !ImGui.GetIO().KeyCtrl, false))
                    {
                        Task.Run(() => P.RemoveItem(item.Id));
                    }

                    if (ImGui.SliderFloat($"Item Size Scaling###Scale-{item.Id}", ref itemScale, 0.1f, 10f))
                    {
                        item.Scale = itemScale;

                        P.Config.ItemDisplays[Svc.ClientState.LocalContentId] = itemDisplays;
                        P.Config.Save();

                        Task.Run(() => P.UpdateAvailableItemCounts(item.Id));
                    }

                    if (ImGui.SliderFloat($"Item Opacity###Opacity-{item.Id}", ref itemOpacity, 0f, 1f))
                    {
                        item.Opacity = itemOpacity;

                        P.Config.ItemDisplays[Svc.ClientState.LocalContentId] = itemDisplays;
                        P.Config.Save();

                        Task.Run(() => P.UpdateAvailableItemCounts(item.Id));
                    }

                    ImGui.TextWrapped($"Text Command(s)");
                    if (ImGui.InputText($"###{item.Id}-TextCommand", ref itemCommand, 300))
                    {
                        Svc.Log.Info($"Setting command for {item.Id}: {itemCommand}");
                        item.TextCommand = itemCommand;

                        P.Config.ItemDisplays[Svc.ClientState.LocalContentId] = itemDisplays;
                        P.Config.Save();

                        Task.Run(() => P.UpdateAvailableItemCounts(item.Id));
                    }
                    ImGui.TextWrapped($"Separate commands with ;\nUse [] instead of <>");

                    
                }
            }
        }
    }
}
