using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimpleNPCStats2.Common.Config.UI.Core;
using SimpleNPCStats2.Common.Core;
using SimpleNPCStats2.Common.Core.UIField;
using SimpleNPCStats2.Common.Core.UISlider;
using SimpleNPCStats2.Common.Junk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;
using static SimpleNPCStats2.Common.Config.ConfigData.NPCGroup.StatSet;

namespace SimpleNPCStats2.Common.Config.UI.NPCUI
{
    public class NPCStats : SmartUIPanel
    {
        public ConfigData.NPCGroup.StatSet data;

        public NPCStats(ConfigData.NPCGroup.StatSet data)
        {
            this.data = data;
        }

        private bool _canSave = true;
        public override void SafeOnInitialize()
        {
            List<IResetValue> resetValues = new();

            UIElement CreateStatElement<T>(T value, Action<T> setValue, string text, UISlider<T> slider, UIField<T> field) where T : IEquatable<T>
            {
                var panel = new SmartUIPanel();
                panel.SetPadding(4);
                panel.muted = true;
                panel.hoverStatus = UIHoverStatus.NoHover;

                var statText = new UIText(text, 0.7f);
                statText.SetSize(0, 0, 1, 0.2f);
                statText.TextOriginX = 0.5f;
                statText.TextOriginY = 0.5f;
                panel.Append(statText);

                slider.SetSize(0, 0, 1, 0.35f);
                slider.VAlign = 0.4f;
                panel.Append(slider);

                var field_panel = new SmartUIPanel();
                field_panel.SetPadding(2);
                field_panel.SetSize(-22, 0, 1, 0.35f);
                field_panel.VAlign = 1;
                panel.Append(field_panel);

                field.SetSize(0, 0, 1, 1);
                field.OnValueChanged += (T newValue) =>
                {
                    slider.Value = newValue;
                    setValue(newValue);
                    if (_canSave)
                    {
                        ConfigDataElement.Instance.Save();
                    }
                };
                field_panel.Append(field);

                slider.OnValueChanged += (T newValue) =>
                {
                    field.Value = newValue;
                    setValue(newValue);
                    if (_canSave)
                    {
                        ConfigDataElement.Instance.Save();
                    }
                };

                var reset_Panel = new SmartUIPanel();
                reset_Panel.SetSize(22, 0, 0, 0.35f);
                reset_Panel.SetPadding(0);
                reset_Panel.VAlign = 1;
                reset_Panel.HAlign = 1;
                reset_Panel.hoverText = Language.GetTextValue(SNSHelper.LocalizationDirectory + "StatReset");
                reset_Panel.OnLeftClick += delegate
                {
                    if (!field.Value.Equals(field.DefaultValue))
                    {
                        field.ResetValue(false);
                        slider.ResetValue(false);
                        setValue(field.DefaultValue);
                        ConfigDataElement.Instance.Save();
                    }
                };
                panel.Append(reset_Panel);

                var reset_Image = new UIImage(ModContent.Request<Texture2D>("Terraria/Images/UI/InfoIcon_8"))
                {
                    HAlign = 0.5f,
                    VAlign = 0.5f,
                };
                reset_Image.NormalizedOrigin = new Vector2(0.5f, 0.5f);
                reset_Panel.Append(reset_Image);

                slider.Value = value;
                field.Value = value;

                resetValues.Add(slider);
                resetValues.Add(field);

                return panel;
            }

            UIElement CreateStatPanel(string title, params UIElement[] stats)
            {
                var panel = new SmartUIPanel();
                panel.SetSize(0, 100, 1f, 0f);
                panel.SetPadding(4);
                panel.muted = true;
                panel.hoverStatus = UIHoverStatus.NoHover;

                var text = new UIText(title, 0.8f);
                text.TextOriginX = 0.5f;
                text.TextOriginY = 0.5f;
                text.SetSize(0, 0, 1, 0.2f);
                panel.Append(text);

                if (stats.Length == 1)
                {
                    var stat = stats[0];

                    stat.SetSize(0, 0, 1, 0.8f);
                    stat.Top.Set(0, 0.2f);

                    panel.Append(stat);
                }
                else
                {
                    for (int i = 0; i < stats.Length; i++)
                    {
                        var stat = stats[i];

                        var width = (1f / stats.Length) - 0.02f;
                        stat.SetSize(0, 0, width, 0.8f);
                        stat.Top.Set(0, 0.2f);
                        stat.HAlign = (float)i / (stats.Length - 1);

                        panel.Append(stat);
                    }
                }

                return panel;
            }

            string baseText = Language.GetTextValue(SNSHelper.LocalizationDirectory + "StatBase");
            string multText = Language.GetTextValue(SNSHelper.LocalizationDirectory + "StatMult");
            string flatText = Language.GetTextValue(SNSHelper.LocalizationDirectory + "StatFlat");
            string overrideText = Language.GetTextValue(SNSHelper.LocalizationDirectory + "StatOverride");

            List<UIElement> elements =
            [
                CreateStatPanel(Language.GetTextValue(SNSHelper.LocalizationDirectory + "StatHealth"),
                    CreateStatElement(data.life.baseValue, (value) => data.life.baseValue = value, baseText, new UIIntSlider(0, -500, 500, 10), new UIIntField()),
                    CreateStatElement(data.life.multValue, (value) => data.life.multValue = value, multText, new UIFloatSlider(1, 0.01f, 10, 0.2f), new UIFloatField(1, min: 0.01f, max: 10000)),
                    CreateStatElement(data.life.flatValue, (value) => data.life.flatValue = value, flatText, new UIIntSlider(0, -500, 500, 10), new UIIntField()),
                    CreateStatElement(data.life.overrideValue, (value) => data.life.overrideValue = value, overrideText, new UIIntSlider(min:0, max:1000, rounding:10), new UIIntField())),

                CreateStatPanel(Language.GetTextValue(SNSHelper.LocalizationDirectory + "StatDamage"),
                    CreateStatElement(data.damage.baseValue, (value) => data.damage.baseValue = value, baseText, new UIIntSlider(0, -500, 500, 10), new UIIntField()),
                    CreateStatElement(data.damage.multValue, (value) => data.damage.multValue = value, multText, new UIFloatSlider(1, 0, 10, 0.2f), new UIFloatField(1, min: 0, max: 10000)),
                    CreateStatElement(data.damage.flatValue, (value) => data.damage.flatValue = value, flatText, new UIIntSlider(0, -500, 500, 10), new UIIntField()),
                    CreateStatElement(data.damage.overrideValue, (value) => data.damage.overrideValue = value, overrideText, new UIIntSlider(min:0, max:1000, rounding:10), new UIIntField())),

                CreateStatPanel(Language.GetTextValue(SNSHelper.LocalizationDirectory + "StatDefense"),
                    CreateStatElement(data.defense.baseValue, (value) => data.defense.baseValue = value, baseText, new UIIntSlider(0, -200, 200, 2), new UIIntField()),
                    CreateStatElement(data.defense.multValue, (value) => data.defense.multValue = value, multText, new UIFloatSlider(1, -5, 5, 0.2f), new UIFloatField(1, min: -10000, max: 10000)),
                    CreateStatElement(data.defense.flatValue, (value) => data.defense.flatValue = value, flatText, new UIIntSlider(0, -200, 200, 2), new UIIntField()),
                    CreateStatElement(data.defense.overrideValue, (value) => data.defense.overrideValue = value, overrideText, new UIIntSlider(min:-200, max:200, rounding:10), new UIIntField())),

                CreateStatPanel(Language.GetTextValue(SNSHelper.LocalizationDirectory + "StatRegen"),
                    CreateStatElement(data.regen.baseValue, (value) => data.regen.baseValue = value, baseText, new UIIntSlider(0, -100, 100, 1), new UIIntField()),
                    CreateStatElement(data.regen.multValue, (value) => data.regen.multValue = value, multText, new UIFloatSlider(1, -5, 5, 0.2f), new UIFloatField(1, min: 0, max: 10000)),
                    CreateStatElement(data.regen.flatValue, (value) => data.regen.flatValue = value, flatText, new UIIntSlider(0, -100, 100, 1), new UIIntField()),
                    CreateStatElement(data.regenLifeMaxPercent, (value) => data.regenLifeMaxPercent = value, Language.GetTextValue(SNSHelper.LocalizationDirectory + "StatRegenPercent"), new UIFloatSlider(0, -1, 1, 0.02f), new UIFloatField()),
                    CreateStatElement(data.regen.overrideValue, (value) => data.regen.overrideValue = value, overrideText, new UIIntSlider(min:-100, max:100, rounding:1), new UIIntField())),

                CreateStatPanel(Language.GetTextValue(SNSHelper.LocalizationDirectory + "StatKnockback"),
                    CreateStatElement(data.knockback.baseValue, (value) => data.knockback.baseValue = value, baseText, new UIFloatSlider(0, 0, 4, 0.1f), new UIFloatField(min: 0, max: 10000)),
                    CreateStatElement(data.knockback.multValue, (value) => data.knockback.multValue = value, multText, new UIFloatSlider(1, 0, 10, 0.2f), new UIFloatField(1, min: 0, max: 1000)),
                    CreateStatElement(data.knockback.flatValue, (value) => data.knockback.flatValue = value, flatText, new UIFloatSlider(0, 0, 4, 0.1f), new UIFloatField(min: 0, max: 10000)),
                    CreateStatElement(data.knockback.overrideValue, (value) => data.knockback.overrideValue = value, overrideText, new UIFloatSlider(min:0, max:4, rounding:0.1f), new UIFloatField(min:0, max: 10000))),

                CreateStatPanel(Language.GetTextValue(SNSHelper.LocalizationDirectory + "StatSize"),
                    CreateStatElement(data.scale.multValue, (value) => data.scale.multValue = value, multText, new UIFloatSlider(1, 0.01f, 2, 0.05f), new UIFloatField(1, min: 0.01f, max: 100))),

                    CreateStatPanel(Language.GetTextValue(SNSHelper.LocalizationDirectory + "StatGravity"),
                    CreateStatElement(data.gravity.multValue, (value) => data.gravity.multValue = value, multText, new UIFloatSlider(1, -2, 2, 0.05f), new UIFloatField(1, min: -100, max: 100))),

                CreateStatPanel(Language.GetTextValue(SNSHelper.LocalizationDirectory + "StatMovement"),
                    CreateStatElement(data.movement.multValue, (value) => data.movement.multValue = value, multText, new UIFloatSlider(1, -2, 2, 0.1f), new UIFloatField(1, min: -100, max: 100))),

                CreateStatPanel(Language.GetTextValue(SNSHelper.LocalizationDirectory + "StatAISpeed"),
                    CreateStatElement(data.aiSpeed.multValue, (value) => data.aiSpeed.multValue = value, multText, new UIFloatSlider(1, 0, 4, 0.1f), new UIFloatField(1, min: 0, max: 100))),

                //CreateStatPanel(Language.GetTextValue(SNSHelper.LocalizationDirectory + "StatSpawnRate"),
                //    CreateStatElement(data.spawnRateMultiplier, (value) => data.spawnRateMultiplier = value, multText, new UIFloatSlider(1, 0, 10, 0.1f), new UIFloatField(1, min: 0, max: 10000))),
            ];

            elements[5].Width.Set(0, 0.5f);
            elements[6].Width.Set(0, 0.5f);
            elements[7].Width.Set(0, 0.5f);
            elements[8].Width.Set(0, 0.5f);

            SetPadding(4);

            var panel = new SmartUIElement();
            panel.SetSize(-22, 0, 1f, 1f);
            Append(panel);

            var upperElement = new SmartUIElement();
            upperElement.SetSize(0, 0, 1, 0.12f);
            panel.Append(upperElement);

            var titleElement = new UIText(Language.GetTextValue(SNSHelper.LocalizationDirectory + "StatTitle"), 0.8f);
            titleElement.SetSize(-22, 0, 0, 1);
            titleElement.HAlign = 0.5f;
            titleElement.TextOriginX = 0.5f;
            titleElement.TextOriginY = 0.5f;
            upperElement.Append(titleElement);

            var titleImageElement = new SmartUIElement();
            titleImageElement.mouseOverSound = SoundID.MenuTick;
            titleImageElement.SetPadding(0);
            titleImageElement.SetSize(20, 20, 0, 0);
            titleImageElement.SetAlign(0f, 0.5f);
            titleImageElement.hoverText = Language.GetTextValue(SNSHelper.LocalizationDirectory + "StatTutorial");
            upperElement.Append(titleImageElement);

            var titleImage = new UIImage(ModContent.Request<Texture2D>("SimpleNPCStats2/Assets/Info"));
            titleImage.NormalizedOrigin = new Vector2(0.5f, 0.5f);
            titleImage.SetSize(0, 0, 1, 1);
            titleImage.HAlign = 0.5f;
            titleImage.VAlign = 0.5f;
            titleImageElement.Append(titleImage);

            var reset_Panel = new SmartUIPanel();
            reset_Panel.SetSize(20, 20, 0, 0);
            reset_Panel.SetPadding(0);
            reset_Panel.SetAlign(1f, 0.5f);
            reset_Panel.hoverText = Language.GetTextValue(SNSHelper.LocalizationDirectory + "StatResetAll");
            reset_Panel.OnLeftClick += delegate
            {
                bool difference = false;
                foreach (var r in resetValues)
                {
                    _canSave = false;
                    if (r.ResetValue(true))
                    {
                        difference = true;
                    }
                    _canSave = true;
                }
                if (difference)
                {
                    ConfigDataElement.Instance.Save();
                }
            };
            upperElement.Append(reset_Panel);

            var reset_Image = new UIImage(ModContent.Request<Texture2D>("Terraria/Images/UI/InfoIcon_8"))
            {
                HAlign = 0.5f,
                VAlign = 0.5f,
            };
            reset_Image.NormalizedOrigin = new Vector2(0.5f, 0.5f);
            reset_Panel.Append(reset_Image);

            var scrollBar = new UIScrollbar();
            scrollBar.SetSize(0, -10, 1, 1);
            scrollBar.SetPosition(0, 5, 0, 0);
            scrollBar.HAlign = 1f;
            Append(scrollBar);

            var grid_Panel = new SmartUIPanel();
            grid_Panel.SetSize(0, 0, 1f, 0.88f);
            grid_Panel.VAlign = 1;
            grid_Panel.PaddingLeft = 0;
            grid_Panel.PaddingRight = 0;
            grid_Panel.PaddingTop = 2;
            grid_Panel.PaddingBottom = 2;
            grid_Panel.muted = true;
            grid_Panel.hoverStatus = UIHoverStatus.NoHover;
            panel.Append(grid_Panel);

            var grid = new UIGridWithScrollSpeed();
            grid.SetSize(0, 0, 1f, 1f);
            grid.scrollSpeed = 0.2f;
            grid.ListPadding = 0;
            grid.SetScrollbar(scrollBar);
            grid_Panel.Append(grid);
            grid.AddRange(elements);
        }
    }
}
