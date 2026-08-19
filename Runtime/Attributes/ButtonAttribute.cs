using System;
using UnityEngine;

namespace ToolKit
{
    public enum ButtonColor
    {
        Green,
        Red,
        Yellow,
        Blue,
        White
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class ButtonAttribute : Attribute
    {
        public string Name;
        public ButtonColor Color;
        public int Order;
        public string Group;


        public ButtonAttribute(string name = null, ButtonColor color = ButtonColor.White, string group = "", int order = 0)
        {
            Color = color;
            Name = name;
            Group = group;
            Order = order;
        }
    }
}