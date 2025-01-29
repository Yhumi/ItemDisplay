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
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ItemDisplay.UI
{
    internal class SettingsUI : Window, IDisposable
    {
        private bool visible = false;

        private string ItemName = string.Empty;
        private List<ContentSelection> Content = new();

        private string contentSearch = string.Empty;
        private bool onlyShowSelected = false;

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

            Task.Run(() => SetupMapList());
            P.ws.AddWindow(this);
        }

        public void Dispose() 
        {
        }

        public async void SetupMapList()
        {
            Content = LuminaService.GetContent();
        }

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

        private string SelectedInstanceString(List<uint> selected)
        {
            if (selected.Count == 0)
                return "No Instance Selected";

            if (selected.Count == 1)
                return Content.FirstOrDefault(x => x.ContentId == selected.First()).ContentName;

            return $"{Content.FirstOrDefault(x => x.ContentId == selected.First()).ContentName}, and {selected.Count - 1} others..";
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
            if (ImGui.BeginCombo("###IconSel", $"Open me to add icons...", ImGuiComboFlags.HeightLargest))
            {
                var cursor = ImGui.GetCursorPos();
                ImGui.Dummy(new Vector2(200, ImGuiHelpers.MainViewport.Size.Y * P.Config.SelectorHeight / 100));
                ImGui.SetCursorPos(cursor);

                P.Selector.Draw();

                ImGui.EndCombo();
            }

            ImGui.Separator();

            foreach (var item in itemDisplays)
            {
                var itemCommand = item.TextCommand ?? string.Empty;
                var showDisplay = item.ShowDisplay;
                var showCount = item.ShowCount;
                var itemScale = item.Scale;
                var itemOpacity = item.Opacity;
                var iconRef = item.IconReference ?? string.Empty;

                var selectedInstances = item.Instances;

                var header = item.Type == ItemDisplayType.Item ? item.ItemName : $"Icon {item.IconId}";
                if (ImGui.CollapsingHeader($"{header} ({item.Id})"))
                {
                    if (ImGui.Checkbox($"Show Display###Disp-{item.Id}", ref showDisplay))
                    {
                        item.ShowDisplay = showDisplay;

                        P.Config.ItemDisplays[Svc.ClientState.LocalContentId] = itemDisplays;
                        P.Config.Save();

                        Task.Run(() => P.CleanupDisplayList());
                        Task.Run(() => P.UpdateAvailableItemCounts(item.Id));
                    }
                    if (item.Type == ItemDisplayType.Item)
                    {
                        ImGui.SameLine();
                        if (ImGui.Checkbox($"Show Count###Count-{item.Id}", ref showCount))
                        {
                            item.ShowCount = showCount;

                            P.Config.ItemDisplays[Svc.ClientState.LocalContentId] = itemDisplays;
                            P.Config.Save();

                            Task.Run(() => P.UpdateAvailableItemCounts(item.Id));
                        }
                    }
                    
                    ImGui.SameLine(ImGui.GetWindowWidth() / 2);
                    if (ImGuiUtil.DrawDisabledButton($"Delete Icon###{item.Id}-DeleteItemDisplay", default, "Delete Current Selection. Hold control & shift while clicking.", !ImGui.GetIO().KeyCtrl || !ImGui.GetIO().KeyShift, false))
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

                    if (ImGui.BeginCombo($"Zone###Zone-{item.Id}", SelectedInstanceString(selectedInstances)))
                    {
                        ImGui.Text("Search");
                        ImGui.SameLine();
                        ImGui.InputText($"###ContentSearch-{item.Id}", ref contentSearch, 100);
                        ImGui.SameLine();
                        ImGui.Text("Selected Only");
                        ImGui.SameLine();
                        ImGui.Checkbox($"###ContentSelected-{item.Id}", ref onlyShowSelected);

                        foreach (var cfc in Content
                            .Where(x => !onlyShowSelected || selectedInstances.Contains(x.ContentId))
                            .Where(x => 
                                x.ContentName.Contains(contentSearch, StringComparison.InvariantCultureIgnoreCase) 
                                || x.ContentType.Contains(contentSearch, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            if (ImGui.Selectable($"[{cfc.ContentType}] {cfc.ContentName}###ContentSelect-{cfc.ContentId}-{item.Id}", selectedInstances.Contains(cfc.ContentId)))
                            {
                                Svc.Log.Debug($"Attempting to remove {cfc.ContentId}");
                                if (!selectedInstances.Remove(cfc.ContentId))
                                {
                                    Svc.Log.Debug($"Attempting to add {cfc.ContentId}");
                                    selectedInstances.Add(cfc.ContentId);
                                }
                                
                                item.Instances = selectedInstances;

                                P.Config.ItemDisplays[Svc.ClientState.LocalContentId] = itemDisplays;
                                P.Config.Save();
                            }
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.TextWrapped($"Text Command(s)");
                    if (ImGui.InputTextMultiline($"###{item.Id}-TextCommand", ref itemCommand, 300, ImGuiHelpers.ScaledVector2(ImGui.GetWindowWidth() * 0.7f, 200f)))
                    {
                        Svc.Log.Info($"Setting command for {item.Id}: {itemCommand}");
                        item.TextCommand = itemCommand;

                        P.Config.ItemDisplays[Svc.ClientState.LocalContentId] = itemDisplays;
                        P.Config.Save();

                        Task.Run(() => P.UpdateAvailableItemCounts(item.Id));
                    }
                    ImGui.TextWrapped($"One command per line.\nUse [] instead of <>");
                }
            }
        }
    }
}
