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
                MinimumSize = new(400, 200),
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

        public async void RemoveItemDisplay(string itemName)
        {
            var itemId = LuminaService.GetItemIdByItemName(itemName);
            Svc.Log.Info($"{itemName} id: {itemId}");
            if (itemId != 0)
            {
                Task.Run(() => P.RemoveItem(itemId));
            }
            ItemName = string.Empty;
        }

        public async void SetupItemDisplay(bool showDisplay)
        {
            P.Config.ShowDisplay = showDisplay;
            P.Config.Save();
        }

        public override async void Draw()
        {
            P.Config.ItemDisplays.TryGetValue(Svc.ClientState.LocalContentId, out var itemDisplays);
            if (itemDisplays == null) itemDisplays = new();

            bool ShowDisplay = P.Config.ShowDisplay;

            if (ImGui.Checkbox("Show Display Windows", ref ShowDisplay))
            {
                Task.Run(() => SetupItemDisplay(ShowDisplay));
            }
            ImGuiComponents.HelpMarker($"Draw the windows/UI edits tied to the Materia Melding window in game.");

            ImGui.Separator();

            ImGui.TextWrapped("Add Item Display");
            ImGui.InputText("###ItemName", ref ItemName, 150);
            if (ImGui.Button("Add new Item Display"))
            {           
                Svc.Log.Info($"Adding item: {ItemName}");
                if (ItemName != string.Empty && !itemDisplays.Any(x => x.ItemName.ToLower() == ItemName.ToLower()))
                {
                    Task.Run(() => CreateItemDisplay(ItemName));
                }
            }

            if (ImGui.Button("Remove Item Display"))
            {
                Svc.Log.Info($"Removing item: {ItemName}");
                if (ItemName != string.Empty && itemDisplays.Any(x => x.ItemName.ToLower() == ItemName.ToLower()))
                {
                    Task.Run(() => RemoveItemDisplay(ItemName));
                }
            }

            ImGui.Separator();
            ImGui.TextWrapped("Item Displays:");

            foreach (var item in itemDisplays)
            {
                ImGui.TextWrapped($"{item.ItemName} ({item.ItemId})");
            }
        }
    }
}
