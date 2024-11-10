using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria.GameContent;
using SimpleNPCStats2.Common.Junk;
using SimpleNPCStats2.Common.Core;
using Terraria.GameInput;

namespace SimpleNPCStats2.Common.Core.UIField
{
    public interface IResetValue
    {
        bool ResetValue(bool invoke);
    }

    public abstract class UIField<T> : SmartUIElement, IResetValue where T : IEquatable<T>
    {
        private T _value;
        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                valueString = FormatString(_value);
            }
        }

        public T DefaultValue { get; protected set; }
        protected int characterLimit;
        protected bool NoCharacterLimit => characterLimit <= 0;

        private bool isTyping;
        protected bool TypingState
        {
            set
            {
                isTyping = value;
                Main.blockInput = value;
            }
        }

        public event Action<T> OnValueChanged;

        public event Action<T> OnFinishTyping;

        public UIField(T defaultValue, int characterLimit)
        {
            this.DefaultValue = defaultValue;
            Value = defaultValue;
            this.characterLimit = characterLimit;
        }

        public Color color = Color.White;

        public override void SafeLeftMouseDown(UIMouseEvent evt, ref bool playSound)
        {
            TypingState = true;
        }

        protected virtual string FormatString(T value) => value.ToString();

        private string valueString;
        public override void SafeUpdate(GameTime gametime)
        {
            if (isTyping && Main.mouseLeft && !IsMouseHovering)
            {
                TypingState = false;

                T old = _value;
                if (TryParseValueString(valueString, out T result))
                {
                    _value = result;
                    if (!result.Equals(old))
                    {
                        OnValueChanged?.Invoke(_value);
                    }
                }
                else
                {
                    _value = DefaultValue;
                    if (!DefaultValue.Equals(old))
                    {
                        OnValueChanged?.Invoke(_value);
                    }
                }
                valueString = FormatString(_value);

                OnFinishTyping?.Invoke(_value);
                return;
            }

            if (isTyping)
            {
                PlayerInput.WritingText = true;
                Main.instance.HandleIME();

                string oldValueString = valueString;
                valueString = FilterValueString(Main.GetInputText(valueString));

                if (valueString.Length > characterLimit && !NoCharacterLimit)
                {
                    valueString = valueString[..characterLimit];
                }

                if (oldValueString != valueString)
                {
                    if (TryParseValueString(valueString, out T result))
                    {
                        _value = result;
                        OnValueChanged?.Invoke(_value);
                    }
                }
            }
        }

        public bool ResetValue(bool invoke)
        {
            if (!_value.Equals(DefaultValue))
            {
                Value = DefaultValue;
                if (invoke)
                {
                    OnValueChanged?.Invoke(_value);
                }
                return true;
            }
            return false;
        }

        protected abstract string FilterValueString(string value);
        protected abstract bool TryParseValueString(string value, out T result);

        public string defaultDisplayString = string.Empty;
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Color drawColor = color;

            string text = valueString;
            if (isTyping)
            {
                if ((text.Length < characterLimit || characterLimit == 0) && Main.GlobalTimeWrappedHourly % 1 > 0.5f)
                {
                    text += "|";
                }
            }
            else if (text == string.Empty)
            {
                text = defaultDisplayString;
                drawColor *= 0.5f;
            }

            Utils.DrawBorderString(spriteBatch, text, GetInnerDimensions().Position() + new Vector2(0, GetInnerDimensions().Height / 2f), drawColor, 1, 0f, 0.4f, 15);
        }
    }
}
