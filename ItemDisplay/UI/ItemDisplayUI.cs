using Dalamud.Interface.Colors;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
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

        private Vector2 imageStart = new Vector2(2f, 2f);
        private Vector2 imageSize = new Vector2(64f, 64f);

        private readonly Vector2 baseImageSize = new Vector2(64f, 64f);
        private IDalamudTextureWrap iconTextureWrap;
        private bool iconLoaded = false;

        public ItemDisplayUI(ItemDisplayModel model) : base($"###ItemDisplayUI-{model.Id}", 
            ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoResize)
        {
            RespectCloseHotkey = false;
            IsOpen = true;
            
            ItemModel = model;
            P.ws.AddWindow(this);

            DisableWindowSounds = true;

            imageSize = baseImageSize * ItemModel.Scale;

            if (!P.Config.MoveMode)
                Flags |= ImGuiWindowFlags.NoMove;
        }

        public void Dispose()
        {
            IsOpen = false;
            P.ws.RemoveWindow(this);
        }

        public void UpdateItemModel(ItemDisplayModel model)
        {
            ItemModel = model;
            imageSize = baseImageSize * ItemModel.Scale;
        }

        public void UpdateMoveMode()
        {
            Flags ^= ImGuiWindowFlags.NoMove;
        }

        public override bool DrawConditions()
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas]) return false;

            if (ItemModel.Instances.Count > 0 && 
                (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty] ||
                Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty56] ||
                Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty95]))
            {
                var duty = DutyService.GetCurrentInstanceId();
                if (duty == 0) return P.Config.ShowDisplay && ItemModel.ShowDisplay;

                return ItemModel.Instances.Contains(duty);
            }

            return P.Config.ShowDisplay && ItemModel.ShowDisplay;
        }

        public override void PreDraw()
        {
            base.PreDraw();
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f, 0f));
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ItemModel.Opacity);
            ImGui.SetNextWindowSize(new Vector2(imageSize.X + (imageStart.X * 2f), imageSize.Y + (imageStart.Y * 2f)));

            iconLoaded = ThreadLoadImageHandler.TryGetIconTextureWrap(ItemModel.IconId, true, out iconTextureWrap);
        }

        public override void PostDraw()
        {
            ImGui.PopStyleVar(2);
            base.PostDraw();
        }

        public override async void Draw()
        {
            if (!iconLoaded) return;

            ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPos().X + imageStart.X, ImGui.GetCursorPos().Y + imageStart.Y));
            if (String.IsNullOrWhiteSpace(ItemModel.TextCommand) || P.Config.MoveMode)
            {
                ImGui.Image(iconTextureWrap.ImGuiHandle, imageSize, Vector2.Zero, Vector2.One);
            }
            else
            {
                if(ImGui.ImageButton(iconTextureWrap.ImGuiHandle, imageSize, Vector2.Zero, Vector2.One, 0))
                {
                    string[] commandList = ItemModel.TextCommand.Contains('\n') ? ItemModel.TextCommand.Split('\n') : [ItemModel.TextCommand];
                    foreach (var command in commandList)
                    {
                        if (String.IsNullOrWhiteSpace(command)) continue;
                        var textCommand = CommandService.FormatCommand(command);
                        P.TM.Enqueue(() => Chat.SendMessage($"{textCommand}"));
                    }
                }
            }

            if (ItemModel.Type != ItemDisplayType.Item) return;
            if (!ItemModel.ShowCount) return;

            DrawQuantText(
                new Vector2(ImGui.GetWindowPos().X + imageStart.X + imageSize.X, ImGui.GetWindowPos().Y + imageStart.Y + imageSize.Y), 
                $"x{ItemModel.ItemCount:n0}", 
                ImGuiColors.DalamudWhite,
                P.Config.TextScale * ItemModel.Scale,
                true, false);
        }

        private static void DrawQuantText(Vector2 drawPosition, string text, Vector4 color, float scale, bool alignRight = false, bool debug = false)
        {
            var font = ImGui.GetFont();
            var drawList = ImGui.GetWindowDrawList();
            var stringSize = ImGui.CalcTextSize(text) * scale;

            drawPosition = new Vector2(drawPosition.X, drawPosition.Y - stringSize.Y);
            if (alignRight)
            {
                drawPosition = new Vector2(drawPosition.X - stringSize.X - 1f, drawPosition.Y);
            }   

            if (debug)
                drawList.AddRect(drawPosition, drawPosition + stringSize, ImGui.GetColorU32(Dalamud.Interface.Colors.ImGuiColors.HealerGreen));

            drawList.AddText(font, font.FontSize * scale, drawPosition, ImGui.GetColorU32(color), text);
        }
    }
}
