using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent.UI.Elements;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Localization;
using Terraria.ModLoader.UI.Elements;
using System.Diagnostics;
using Terraria;
using SimpleNPCStats2.Common.Core;
using System.Reflection;
using Terraria.UI.Chat;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.ID;
using ReLogic.Content;

namespace SimpleNPCStats2.Common.Config.UI.NPCUI
{
    public class NPCGroups : SmartUIPanel
    {
        public UIList list;

        public NPCGroups()
        {
            hoverStatus = UIHoverStatus.NoHover;
            muted = true;
        }

        public const int MAX_GROUPS = 20;

        public override void SafeOnInitialize()
        {
            var upperElement = new SmartUIElement();
            upperElement.SetSize(0, 50, 1, 0);
            Append(upperElement);

            var mainText = new UIText(Language.GetTextValue(SNSHelper.LocalizationDirectory + "GroupsMain"), 0.4f, true);
            mainText.DynamicallyScaleDownToWidth = true;
            mainText.SetSize(0, 50, 1f, 0);
            mainText.TextOriginX = 0.5f;
            mainText.TextOriginY = 0.5f;
            upperElement.Append(mainText);

            var listPanel = new SmartUIPanel();
            listPanel.hoverStatus = UIHoverStatus.NoHover;
            listPanel.SetSize(0, -50, 1, 1);
            listPanel.SetPosition(0, 50, 0, 0);
            listPanel.SetPadding(4);
            listPanel.muted = true;
            Append(listPanel);

            list = new UIList();
            list.SetSize(0, 0, 1, 1);
            list.ManualSortMethod = (element) =>
            {
                // No sorting needed!!!
            };
            listPanel.Append(list);

            var scrollbar = new UIScrollbar();
            scrollbar.HAlign = 1;
            scrollbar.VAlign = 0.5f;
            scrollbar.Height.Set(-10, 1);
            list.SetScrollbar(scrollbar);
            listPanel.Append(scrollbar);

            var newGroupPanel = new SmartUIPanel();
            newGroupPanel.hoverText = Language.GetTextValue(SNSHelper.LocalizationDirectory + "GroupsCreateNew");
            newGroupPanel.SetSize(50, 50, 0, 0);
            newGroupPanel.HAlign = 1f;

            var newGroupText = new UIText(Language.GetTextValue(SNSHelper.LocalizationDirectory + "GroupsNew"), 0.5f, true);
            newGroupText.DynamicallyScaleDownToWidth = true;
            newGroupText.TextOriginX = 0.5f;
            newGroupText.TextOriginY = 0.5f;
            newGroupText.HAlign = 0.5f;
            newGroupText.VAlign = 0.5f;
            newGroupPanel.OnLeftClick += delegate
            {
                if (list.Count < MAX_GROUPS)
                {
                    var data = new ConfigData.NPCGroup(Language.GetTextValue(SNSHelper.LocalizationDirectory + "GroupsGroupDefault", list.Count + 1));
                    NewNPCGroup(data);
                }
            };
            newGroupPanel.Append(newGroupText);
            upperElement.Append(newGroupPanel);

            var titleImageElement = new SmartUIElement();
            titleImageElement.mouseOverSound = SoundID.MenuTick;
            titleImageElement.SetPadding(0);
            titleImageElement.SetSize(20, 20, 0, 0);
            titleImageElement.VAlign = 0.5f;
            titleImageElement.hoverText = Language.GetTextValue(SNSHelper.LocalizationDirectory + "GroupsTutorial");
            upperElement.Append(titleImageElement);

            var titleImage = new UIImage(ModContent.Request<Texture2D>("SimpleNPCStats2/Assets/Info"));
            titleImage.NormalizedOrigin = new Vector2(0.5f, 0.5f);
            titleImage.SetSize(0, 0, 1, 1);
            titleImage.HAlign = 0.5f;
            titleImage.VAlign = 0.5f;
            titleImageElement.Append(titleImage);

            LoadData();
        }

        private void LoadData()
        {
            foreach (var group in ConfigDataElement.instance.Data.NPCGroups)
            {
                NewNPCGroup(group, false);
            }
        }

        public void NewNPCGroup(ConfigData.NPCGroup data, bool isNewData = true)
        {
            var entry = new Entry(data);
            entry.SetSize(-25, 60, 1, 0);
            list.Add(entry);
            entry.Activate();

            entry.Edit.OnLeftClick += delegate
            {
                ConfigDataElement.instance.SetElement(new NPCBrowser(entry.Data));
            };
            entry.Delete.OnLeftClick += delegate
            {
                list.Remove(entry);
                ConfigDataElement.instance.Data.NPCGroups.Remove(entry.Data);
                ConfigDataElement.instance.Save();
            };
            entry.Up.OnLeftClick += delegate
            {
                int index = list._items.IndexOf(entry);
                SwapEntries(index, index - 1);
            };
            entry.Down.OnLeftClick += delegate
            {
                int index = list._items.IndexOf(entry);
                SwapEntries(index, index + 1);
            };
            entry.Duplicate.OnLeftClick += delegate
            {
                if (list.Count < MAX_GROUPS)
                {
                    var newData = data.Clone();
                    newData.name += " (Copy)";
                    newData.name = newData.name[0..Math.Min(NPCBrowser.MAX_NAME_LENGTH, newData.name.Length)];
                    NewNPCGroup(newData);
                }
            };

            void SwapEntries(int index1, int index2)
            {
                if (index1 >= 0 && index1 < list.Count && index2 >= 0 && index2 < list.Count && index1 != index2)
                {
                    (list._items[index1], list._items[index2]) = (list._items[index2], list._items[index1]);
                    var groups = ConfigDataElement.instance.Data.NPCGroups;
                    (groups[index1], groups[index2]) = (groups[index2], groups[index1]);
                    ConfigDataElement.instance.Save();
                }
            }

            if (isNewData)
            {
                ConfigDataElement.instance.Data.NPCGroups.Add(data);
                ConfigDataElement.instance.Save();
            }
        }

        public class Entry : SmartUIPanel
        {
            public ConfigData.NPCGroup Data { get; private set; }
            public SmartUIPanel Edit { get; private set; }
            public SmartUIPanel Delete { get; private set; }
            public SmartUIPanel Up { get; private set; }
            public SmartUIPanel Down { get; private set; }
            public SmartUIPanel Duplicate { get; private set; }
            public UIText Text { get; private set; }

            public Entry(ConfigData.NPCGroup data)
            {
                hoverStatus = UIHoverStatus.NoHover;
                muted = true;
                this.Data = data;
            }

            public override void SafeOnInitialize()
            {
                float squareSize = GetInnerDimensions().Height;

                const float TEXT_SCALE = 0.7f;

                string name = Data.name;
                Vector2 GetTextScale() => ChatManager.GetStringSize(FontAssets.DeathText.Value, name, Vector2.One * TEXT_SCALE);
                var nameSize = GetTextScale();
                bool loopExecuted = false;
                while (nameSize.X > 250)
                {
                    loopExecuted = true;
                    if (name.Length == 0)
                    {
                        name = "";
                        break;
                    }
                    name = name[0..^1];
                    nameSize = GetTextScale();
                }
                if (loopExecuted)
                {
                    name += "...";
                }
                Text = new UIText(name, TEXT_SCALE, true);
                Text.TextOriginX = 0f;
                Text.TextOriginY = 0.5f;
                Text.SetSize(-squareSize * 3 - 25, 0, 1, 1);
                Text.SetPosition(squareSize + 10, 0, 0, 0);
                Text.IgnoresMouseInteraction = true;
                Append(Text);

                Edit = new SmartUIPanel();
                Edit.leftClickSound = SoundID.MenuOpen;
                Edit.SetSize(squareSize, squareSize, 0, 0);
                Edit.SetPadding(0);
                Edit.hoverText = Language.GetTextValue(SNSHelper.LocalizationDirectory + "GroupsEdit");
                Append(Edit);
                var edit_Image = new UIImage(ModContent.Request<Texture2D>("Terraria/Images/UI/Reforge_0", AssetRequestMode.ImmediateLoad))
                {
                    HAlign = 0.5f,
                    VAlign = 0.5f,
                };
                edit_Image.NormalizedOrigin = new Vector2(0.5f, 0.5f);
                Edit.Append(edit_Image);

                Delete = new SmartUIPanel();
                Delete.CopyStyle(Edit);
                Delete.HAlign = 1;
                Delete.Left.Pixels -= squareSize + 5;
                Delete.hoverText = Language.GetTextValue(SNSHelper.LocalizationDirectory + "GroupsDelete");
                Append(Delete);
                var delete_Image = new UIImage(ModContent.Request<Texture2D>("Terraria/Images/UI/InfoIcon_8", AssetRequestMode.ImmediateLoad))
                {
                    HAlign = 0.5f,
                    VAlign = 0.5f,
                };
                delete_Image.NormalizedOrigin = new Vector2(0.5f, 0.5f);
                Delete.Append(delete_Image);

                Duplicate = new SmartUIPanel();
                Duplicate.CopyStyle(Delete);
                Duplicate.Left.Pixels -= squareSize + 5;
                Duplicate.hoverText = Language.GetTextValue(SNSHelper.LocalizationDirectory + "GroupsDuplicate");
                Append(Duplicate);
                var duplicate_Image = new UIImage(ModContent.Request<Texture2D>("SimpleNPCStats2/Assets/Plus", AssetRequestMode.ImmediateLoad))
                {
                    HAlign = 0.5f,
                    VAlign = 0.5f,
                };
                duplicate_Image.NormalizedOrigin = new Vector2(0.5f, 0.5f);
                Duplicate.Append(duplicate_Image);

                Up = new SmartUIPanel();
                Up.SetPadding(0);
                Up.HAlign = 1;
                Up.SetSize(squareSize, squareSize / 2, 0, 0);
                Up.hoverText = Language.GetTextValue(SNSHelper.LocalizationDirectory + "GroupsMoveUp");
                Append(Up);
                var up_Image = new UIImage(ModContent.Request<Texture2D>("SimpleNPCStats2/Assets/Up", AssetRequestMode.ImmediateLoad))
                {
                    HAlign = 0.5f,
                    VAlign = 0.5f,
                };
                up_Image.NormalizedOrigin = new Vector2(0.5f, 0.5f);
                Up.Append(up_Image);

                Down = new SmartUIPanel();
                Down.CopyStyle(Up);
                Down.VAlign = 1;
                Down.hoverText = Language.GetTextValue(SNSHelper.LocalizationDirectory + "GroupsMoveDown");
                Append(Down);
                var down_Image = new UIImage(ModContent.Request<Texture2D>("SimpleNPCStats2/Assets/Down", AssetRequestMode.ImmediateLoad))
                {
                    HAlign = 0.5f,
                    VAlign = 0.5f,
                };
                down_Image.NormalizedOrigin = new Vector2(0.5f, 0.5f);
                Down.Append(down_Image);
            }
        }
    }
}
