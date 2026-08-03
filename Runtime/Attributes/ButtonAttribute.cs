using System;
using UnityEngine;

namespace ToolKit
{
    public enum ButtonColor
    {
        Green,
        Red,
        Yellow,
        Blue
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class ButtonAttribute : Attribute
    {
        public string Name;
        public Color Color;
        public int Order;

        public ButtonAttribute(string name = null, ButtonColor color = ButtonColor.Green, int order = 0)
        {
            Color = color switch
            {
                ButtonColor.Green => Color.green,
                ButtonColor.Red => Color.red,
                ButtonColor.Yellow => Color.yellow,
                ButtonColor.Blue => Color.blue,
                _ => Color.green
            };

            Name = name;

            Order = order;
        }
    }
}