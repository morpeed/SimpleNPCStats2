using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using SimpleNPCStats2.Common.Junk;
using SimpleNPCStats2.Common.Core;
using Terraria;
using Terraria.GameContent.UI.Elements;
using SimpleNPCStats2.Common.Core.UIField;

namespace SimpleNPCStats2.Common.Config.UI.NPCUI
{
    public enum NPCFilterMode
    {
        Unknown = -1,
        None,
        Active,
        Inactive,
        Friendly,
        Hostile,
        Boss
    }

    public class NPCBrowser : SmartUIPanel
    {
        /// <summary>
        /// All NPC entries to use for reference for filtering.
        /// </summary>
        public ReadOnlyCollection<NPCEntry> entries;

        /// <summary>
        /// The grid of NPCs that shows all NPCs that have been filtered.
        /// </summary>
        public UIGridWithScrollSpeed npcGrid;

        /// <summary>
        /// The mode of filtering NPCs.
        /// </summary>
        public NPCFilterMode filterMode;

        /// <summary>
        /// String field for filtering NPCs by name or mod.
        /// </summary>
        public UIField filter_Field;

        /// <summary>
        /// String field for modifying the current NPC groups' name.
        /// </summary>
        public UIField name_Field;

        private readonly ConfigData.NPCGroup data;

        public NPCBrowser(ConfigData.NPCGroup data)
        {
            this.data = data;
            hoverStatus = UIHoverStatus.NoHover;
            muted = true;
        }

        public const int MAX_NAME_LENGTH = 30;

        private static bool _textureLoading;
        public override void SafeOnInitialize()
        {
            // Start loading all NPC textures for the NPC grid
            if (!_textureLoading)
            {
                Task.Run(() => {
                    for (int i = 1; i < NPCID.Count; i++)
                    {
                        Main.instance.LoadNPC(i);
                    }
                });
            }
            _textureLoading = true;

            // Panel to go back to previous page
            var back_Panel = new SmartUIPanel();
            back_Panel.SetSize(0, 40, 1f, 0f);
            back_Panel.SetPosition(0, -40, 0, 1f);
            back_Panel.leftClickSound = SoundID.MenuClose;
            back_Panel.OnLeftClick += delegate
            {
                ConfigDataElement.instance.SetElement(new NPCGroups());
            };
            Append(back_Panel);
            var back_Text = new UIText(Language.GetTextValue(SNSHelper.LocalizationDirectory + "BrowserBack"));
            back_Text.HAlign = 0.5f;
            back_Text.VAlign = 0.5f;
            back_Text.TextOriginX = 0.5f;
            back_Text.TextOriginY = 0.5f;
            back_Panel.Append(back_Text);

            // Name panel and field
            var name_Panel = new SmartUIPanel();
            name_Panel.SetSize(0, 40, 1f, 0f);
            name_Panel.SetPadding(0);
            Append(name_Panel);

            var name_Field = new UIField("", MAX_NAME_LENGTH);
            name_Field.defaultDisplayString = Language.GetTextValue(SNSHelper.LocalizationDirectory + "BrowserNameDefault");
            name_Field.SetSize(0, 0, 1f, 1f);
            name_Field.hoverText = Language.GetTextValue(SNSHelper.LocalizationDirectory + "BrowserNameHover");
            name_Field.Value = data.name;
            name_Field.OnValueChanged += delegate
            {
                data.name = name_Field.Value;
                ConfigDataElement.instance.Save();
            };
            name_Field.SetPadding(4);
            name_Panel.Append(name_Field);

            // Panel for everything else
            var lower_Element = new SmartUIElement();
            lower_Element.SetSize(0, -120, 1f, 1);
            lower_Element.SetPosition(0, 80, 0, 0);
            Append(lower_Element);

            var grid_Panel = new SmartUIPanel();
            grid_Panel.SetPadding(4);
            grid_Panel.SetSize(0, 0, 1f, 0.5f);
            grid_Panel.muted = true;
            grid_Panel.hoverStatus = UIHoverStatus.NoHover;
            lower_Element.Append(grid_Panel);

            // NPC grid initialisation
            npcGrid = new();
            npcGrid.scrollSpeed = 0.2f;
            npcGrid.SetSize(-20, 0, 1, 1);
            npcGrid.ListPadding = 2f;
            grid_Panel.Append(npcGrid);

            var scrollbar = new UIScrollbar();
            scrollbar.HAlign = 1;
            scrollbar.VAlign = 0.5f;
            scrollbar.Height.Set(-10, 1);
            npcGrid.SetScrollbar(scrollbar);
            grid_Panel.Append(scrollbar);

            var npcList = new List<NPCEntry>();
            var activeNpcs = data.GetNPCs();
            for (int i = NPCID.NegativeIDCount + 1; i < NPCLoader.NPCCount; i++)
            {
                if (i == 0)
                {
                    continue;
                }

                var entry = new NPCEntry(i);
                entry.SetSize(36, 36, 0, 0);
                entry.SetPadding(3);
                entry.OnLeftClick += delegate
                {
                    entry.Active = !entry.Active;
                    FilterGrid();

                    ToggleEntryData(entry);
                    ConfigDataElement.instance.Save();
                };
                if (activeNpcs.Contains(i))
                {
                    entry.Active = true;
                }

                npcList.Add(entry);
            }

            entries = new ReadOnlyCollection<NPCEntry>(npcList);
            npcGrid.AddRange(entries);

            // Filter panel and buttons
            var filter_Panel = new SmartUIPanel();
            filter_Panel.SetSize(-150, 40, 1, 0);
            filter_Panel.SetPosition(0, 40, 0, 0);
            filter_Panel.SetPadding(0);
            Append(filter_Panel);

            filter_Field = new UIField("", 30);
            filter_Field.defaultDisplayString = Language.GetTextValue(SNSHelper.LocalizationDirectory + "BrowserSearchDefault");
            filter_Field.SetSize(0, 0, 1, 1);
            filter_Field.hoverText = Language.GetTextValue(SNSHelper.LocalizationDirectory + "BrowserSearchHover");
            filter_Field.OnValueChanged += delegate
            {
                FilterGrid();
            };
            filter_Field.SetPadding(4);
            filter_Panel.Append(filter_Field);

            var buttons_Panel = new SmartUIPanel();
            buttons_Panel.SetSize(150, 40, 0, 0);
            buttons_Panel.SetPosition(0, 40, 0, 0);
            buttons_Panel.SetPadding(4);
            buttons_Panel.HAlign = 1;
            buttons_Panel.muted = true;
            buttons_Panel.hoverStatus = UIHoverStatus.NoHover;
            Append(buttons_Panel);

            int someCounterForCalculatingWhereToPutTheButtonLol = 0;

            void SetUI(SmartUIElement element)
            {
                element.SetSize(buttons_Panel.GetInnerDimensions().Height, buttons_Panel.GetInnerDimensions().Height, 0, 0);

                element.HAlign = someCounterForCalculatingWhereToPutTheButtonLol / 3f;
                element.SetPadding(0);

                someCounterForCalculatingWhereToPutTheButtonLol++;
            }

            var toggleAll_Panel = new SmartUIPanel();
            toggleAll_Panel.leftClickSound = SoundID.Item37;
            SetUI(toggleAll_Panel);
            toggleAll_Panel.hoverText = Language.GetTextValue(SNSHelper.LocalizationDirectory + "BrowserButtons", Language.GetTextValue(SNSHelper.LocalizationDirectory + "BrowserButtonsToggle"));
            toggleAll_Panel.OnLeftClick += delegate
            {
                for (int i = 0; i < npcGrid.Count; i++)
                {
                    var entry = (NPCEntry)npcGrid._items[i];
                    entry.Active = !entry.Active;
                    ToggleEntryData(entry);
                }
                FilterGrid();
                ConfigDataElement.instance.Save();
            };
            buttons_Panel.Append(toggleAll_Panel);
            var toggleAll_Image = new UIImage(ModContent.Request<Texture2D>("Terraria/Images/UI/Reforge_0"))
            {
                HAlign = 0.5f,
                VAlign = 0.5f,
            };
            toggleAll_Image.NormalizedOrigin = new Vector2(0.5f, 0.5f);
            toggleAll_Panel.Append(toggleAll_Image);

            var enableAll_Panel = new SmartUIPanel();
            enableAll_Panel.leftClickSound = SoundID.Item37;
            SetUI(enableAll_Panel);
            enableAll_Panel.hoverText = Language.GetTextValue(SNSHelper.LocalizationDirectory + "BrowserButtons", Language.GetTextValue(SNSHelper.LocalizationDirectory + "BrowserButtonsEnable"));
            enableAll_Panel.OnLeftClick += delegate
            {
                for (int i = 0; i < npcGrid.Count; i++)
                {
                    var entry = (NPCEntry)npcGrid._items[i];
                    entry.Active = true;
                    ToggleEntryData(entry);
                }
                FilterGrid();
                ConfigDataElement.instance.Save();
            };
            buttons_Panel.Append(enableAll_Panel);
            var enableAll_Image = new UIImage(ModContent.Request<Texture2D>("Terraria/Images/UI/Reforge_0"))
            {
                HAlign = 0.5f,
                VAlign = 0.5f,
            };
            enableAll_Image.NormalizedOrigin = new Vector2(0.5f, 0.5f);
            enableAll_Panel.Append(enableAll_Image);

            var disableAll_Panel = new SmartUIPanel();
            disableAll_Panel.leftClickSound = SoundID.Item37;
            SetUI(disableAll_Panel);
            disableAll_Panel.hoverText = Language.GetTextValue(SNSHelper.LocalizationDirectory + "BrowserButtons", Language.GetTextValue(SNSHelper.LocalizationDirectory + "BrowserButtonsDisable"));
            disableAll_Panel.OnLeftClick += delegate
            {
                for (int i = 0; i < npcGrid.Count; i++)
                {
                    var entry = (NPCEntry)npcGrid._items[i];
                    entry.Active = false;
                    ToggleEntryData(entry);
                }
                FilterGrid();
                ConfigDataElement.instance.Save();
            };
            buttons_Panel.Append(disableAll_Panel);
            var disableAll_Image = new UIImage(ModContent.Request<Texture2D>("Terraria/Images/UI/Reforge_0"))
            {
                HAlign = 0.5f,
                VAlign = 0.5f,
            };
            disableAll_Image.NormalizedOrigin = new Vector2(0.5f, 0.5f);
            disableAll_Panel.Append(disableAll_Image);

            var toggleFilter_Panel = new SmartUIPanel();
            toggleFilter_Panel.leftClickSound = SoundID.Item37;
            SetUI(toggleFilter_Panel);
            toggleFilter_Panel.OnLeftClick += delegate
            {
                filterMode = filterMode.CycleEnum(skipEnums: NPCFilterMode.Unknown);
                FilterGrid();
                UpdateToggleFilterText();
            };
            buttons_Panel.Append(toggleFilter_Panel);
            var toggleFilter_Image = new UIImage(ModContent.Request<Texture2D>("Terraria/Images/UI/Reforge_0"))
            {
                HAlign = 0.5f,
                VAlign = 0.5f,
            };
            toggleFilter_Image.NormalizedOrigin = new Vector2(0.5f, 0.5f);
            toggleFilter_Panel.Append(toggleFilter_Image);

            void UpdateToggleFilterText()
            {
                toggleFilter_Panel.hoverText = Language.GetTextValue(SNSHelper.LocalizationDirectory + "BrowserButtonsToggleFilter", Language.GetTextValue(SNSHelper.LocalizationDirectory + "BrowserButtonsToggleFilter_" + filterMode.ToString()));
            }

            UpdateToggleFilterText();

            var stats_Panel = new NPCStats(data.stats);
            stats_Panel.SetPosition(0, 0, 0, 0.5f);
            stats_Panel.SetSize(0, 0, 1, 0.5f);
            stats_Panel.muted = true;
            stats_Panel.hoverStatus = UIHoverStatus.NoHover;
            lower_Element.Append(stats_Panel);
        }

        private void ToggleEntryData(NPCEntry entry)
        {
            if (entry.Active)
            {
                data.AddNPC(entry.npcNetId);
            }
            else
            {
                data.RemoveNPC(entry.npcNetId);
            }
        }

        private void FilterGrid()
        {
            npcGrid.Clear();

            bool Condition(NPCEntry entry)
            {
                if (filterMode == NPCFilterMode.Active && !entry.Active)
                {
                    return false;
                }
                else if (filterMode == NPCFilterMode.Inactive && entry.Active)
                {
                    return false;
                }
                else if (filterMode == NPCFilterMode.Friendly && !ContentSamples.NpcsByNetId[entry.npcNetId].friendly)
                {
                    return false;
                }
                else if (filterMode == NPCFilterMode.Hostile && ContentSamples.NpcsByNetId[entry.npcNetId].friendly)
                {
                    return false;
                }
                else if (filterMode == NPCFilterMode.Boss && !(ContentSamples.NpcsByNetId[entry.npcNetId].boss || ExtraBossTypes.Contains(entry.npcNetId)))
                {
                    return false;
                }
                if (filter_Field.Value == "" || entry.name.Contains(filter_Field.Value, StringComparison.CurrentCultureIgnoreCase) || entry.mod.Contains(filter_Field.Value, StringComparison.CurrentCultureIgnoreCase) || entry.npcNetId.ToString().Contains(filter_Field.Value, StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
                return false;
            };

            npcGrid.AddRange(entries.Where(Condition));
        }

        public readonly static HashSet<int> ExtraBossTypes =
        [
            NPCID.TheDestroyerBody,
            NPCID.TheDestroyerTail,
            NPCID.EaterofWorldsHead,
            NPCID.EaterofWorldsBody,
            NPCID.EaterofWorldsTail,
            NPCID.GolemFistLeft,
            NPCID.GolemFistRight,
            NPCID.GolemHead,
            NPCID.GolemHeadFree,
            NPCID.MartianSaucerCannon,
            NPCID.MartianSaucerTurret,
        ];
    }
}
