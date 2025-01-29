using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ImGuiNET;
using ItemDisplay.Model;
using ItemDisplay.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static FFXIVClientStructs.FFXIV.Client.LayoutEngine.LayoutManager;

namespace ItemDisplay.UI
{
    internal class IconSelector : Window, IDisposable
    {
        private List<IconSelectorChoice> ItemIcons = [];
        private List<IconSelectorChoice> MarkerIcons = [];
        private List<IconSelectorChoice> MacroIcons = [];

        public static readonly Vector2 IconSize = new(32, 32);
        private string Filter = "";

        public IconSelector() : base("Select Icon")
        {
            this.SetMinSize();

            Task.Run(() => SetupIconLists());
            
            P.ws.AddWindow(this);
        }

        public async void SetupIconLists()
        {
            MarkerIcons = LuminaService.GetMarkerIconList();
            MacroIcons = LuminaService.GetMacroIconList();
            ItemIcons = LuminaService.GetItemIconList();
        }

        public async void CreateItemDisplay(IconSelectorChoice icon)
        {
            var newItem = new ItemDisplayModel()
            {
                IconId = icon.IconId,
                ItemName = icon.ItemName,
                ItemId = icon.ItemId,
                Type = icon.ItemId == 0 ? ItemDisplayType.Icon : ItemDisplayType.Item
            };

            Task.Run(() => P.AddItem(newItem));
        }

        public void Dispose()
        {
        }

        public override void Draw()
        {
            ImGui.SetNextItemWidth(350f);
            ImGui.InputTextWithHint("###icon-search", "Filter...", ref Filter, 75);

            if (ImGui.BeginChild("icon-search-child"))
            {
                if (ImGui.CollapsingHeader("Items"))
                {
                    if (String.IsNullOrWhiteSpace(Filter))
                        ImGuiEx.Text(EColor.RedBright, $"Please start searching to find an item...");
                    else
                    {
                        DrawIconTable(ItemIcons, "Items");
                    }
                }
                if (ImGui.CollapsingHeader("Macro Icons"))
                {
                    DrawIconTable(MacroIcons, "MacroIcons");
                }
                if (ImGui.CollapsingHeader("Markers"))
                {
                    DrawIconTable(MarkerIcons, "Markers");
                }
                ImGui.EndChild();
            }
        }
        
        private void DrawIconTable(IEnumerable<IconSelectorChoice> icons, string id)
        {
            icons = icons.Where(x => x.ItemId == 0 || (!string.IsNullOrEmpty(x.ItemName) && x.ItemName.Contains(Filter, StringComparison.CurrentCultureIgnoreCase)));
            if (!icons.Any())
            {
                ImGuiEx.Text(EColor.RedBright, $"There are no elements that match filter conditions.");
            }

            int cols = 0;
            if (icons.Any(x => x.ItemId != 0))
                cols = Math.Clamp((int)(ImGui.GetWindowSize().X / 150f), 1, 10);
            else
                cols = Math.Clamp((int)(ImGui.GetWindowSize().X / 48f), 1, 10);

            if (ImGui.BeginTable($"IconTable-{id}", cols, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchSame))
            {
                for (var i = 0; i < cols; i++)
                {
                    ImGui.TableSetupColumn($"Col-{id}-{i}");
                }
                var index = 0;

                foreach (var icon in icons)
                {
                    if (index % cols == 0) ImGui.TableNextRow();
                    index++;
                    ImGui.TableNextColumn();

                    if (ThreadLoadImageHandler.TryGetIconTextureWrap(icon.IconId, false, out var wrap))
                    {
                        if (ImGui.ImageButton(wrap.ImGuiHandle, IconSize))
                        {
                            Task.Run(() => CreateItemDisplay(icon));
                        }
                        if (!string.IsNullOrEmpty(icon.ItemName))
                        {
                            ImGui.SameLine();
                            ImGuiEx.TextWrapped($"{icon.ItemName}");
                        } 
                    }
                }
                ImGui.EndTable();
            }
        }
    }
}
