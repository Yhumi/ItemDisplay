using Dalamud.Interface.Colors;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ImGuiNET;
using ImGuiScene;
using ItemDisplay.Model;
using ItemDisplay.Service;
using Lumina.Excel.Sheets;
using OtterGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ItemDisplay.UI
{
    internal class ItemDisplayUI : Window, IDisposable
    {
        private ItemDisplayModel ItemModel;

        public ItemDisplayUI(ItemDisplayModel model) : base($"###ItemDisplayUI-{model.ItemId}", 
            ImGuiWindowFlags.NoDecoration | ImGuiNET.ImGuiWindowFlags.NoBackground)
        {
            RespectCloseHotkey = false;
            IsOpen = true;

            this.SetMinSize(80f, 80f);
            Size = ImGuiHelpers.ScaledVector2(80f, 80f);
            
            ItemModel = model;
            P.ws.AddWindow(this);
        }

        public void Dispose()
        {
            IsOpen = false;
            P.ws.RemoveWindow(this);
        }

        public void UpdateItemModel(ItemDisplayModel model)
        {
            ItemModel = model;
        }

        public override async void Draw()
        {
            if (!P.Config.ShowDisplay) return;
            if (!ItemModel.ShowDisplay) return;

            var iconId = LuminaService.GetIconId(ItemModel.ItemId);
            var icon = Svc.Texture.GetFromGameIcon(new GameIconLookup(iconId));

            if (icon != null)
            {
                icon.TryGetWrap(out var wrap, out _);
                if (wrap != null)
                {
                    if (String.IsNullOrWhiteSpace(ItemModel.TextCommand))
                    {
                        ImGui.Image(wrap.ImGuiHandle, ImGuiHelpers.ScaledVector2(64f, 64f), Vector2.Zero, Vector2.One);
                    }
                    else
                    {
                        if(ImGui.ImageButton(wrap.ImGuiHandle, ImGuiHelpers.ScaledVector2(64f, 64f), Vector2.Zero, Vector2.One, 0))
                        {
                            string[] commandList = ItemModel.TextCommand.Contains(';') ? ItemModel.TextCommand.Split(';') : [ItemModel.TextCommand];
                            foreach (var command in commandList)
                            {
                                if (String.IsNullOrWhiteSpace(command)) continue;
                                var textCommand = CommandService.FormatCommand(command);
                                Task.Run(() => Chat.SendMessage($"{textCommand}"));
                            }
                        }
                    }
                    

                    DrawQuantText(
                        new Vector2(ImGui.GetWindowPos().X + 74f, ImGui.GetWindowPos().Y + 47f), 
                        $"x{ItemModel.ItemCount:n0}", 
                        ImGuiColors.DalamudWhite,
                        ImGuiHelpers.GlobalScale * 1.42f,
                        true, false);
                }
            }  
        }

        private static void DrawQuantText(Vector2 drawPosition, string text, Vector4 color, float scale, bool alignRight = false, bool debug = false)
        {
            var font = ImGui.GetFont();
            var drawList = ImGui.GetWindowDrawList();
            var stringSize = ImGui.CalcTextSize(text) * scale;

            if (alignRight)
            {
                drawPosition = new Vector2(drawPosition.X - stringSize.X, drawPosition.Y);
            }   

            if (debug)
                drawList.AddRect(drawPosition, drawPosition + stringSize, ImGui.GetColorU32(Dalamud.Interface.Colors.ImGuiColors.HealerGreen));

            drawList.AddText(font, font.FontSize * scale, drawPosition, ImGui.GetColorU32(color), text);
        }
    }
}
