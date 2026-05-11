using System;
using System.Collections.Generic;
using UnityEngine;
namespace ToolKits
{

    public class CustomizeGUI
    {
        public enum LayoutType
        {
            Left = 0,
            Center = 1,
            Right = 2,
        }
        public abstract class GUIBase
        {
            protected string Text { set; get; }
            protected LayoutType Type { set; get; }

            protected GUIBase(string text, LayoutType type)
            {
                this.Type = type;
                this.Text = text;
                Type = type;
            }


            public abstract void OnDraw();
        }
        public class Label : GUIBase
        {

            public Label(string text, LayoutType type) : base(text, type)
            {
            }


            public override void OnDraw()
            {
                GUILayout.Label(Text);
            }


        }
        public class Button : GUIBase
        {
            private Action callback;

            public Button(string text, Action callback, LayoutType type) : base(text, type)
            {
                this.callback = callback;
            }


            public override void OnDraw()
            {
                if (GUILayout.Button(Text))
                {
                    callback?.Invoke();
                }
            }
        }
        public class TriggerButton : GUIBase
        {
            private Action<bool> callback;
            private bool currentValue = true;
            private string trueText;
            private string falseText;
            public TriggerButton(string text, Action<bool> callback, LayoutType type) : base(text, type)
            {
                this.callback = callback;
            }
            public TriggerButton(string trueText, string falseText, Action<bool> callback, LayoutType type) : base(trueText, type)
            {
                this.trueText = trueText;
                this.falseText = falseText;
                this.callback = callback;
            }

            public override void OnDraw()
            {
                if (GUILayout.Button(Text))
                {
                    callback?.Invoke(currentValue);
                    currentValue = !currentValue;
                    Text = currentValue ? trueText : falseText;
                }
            }
        }
        private List<GUIBase> drawList = new List<GUIBase>();
        public void AddLabel(string text, LayoutType type = LayoutType.Left)
        {

            AddGUI(new Label(text, type));

        }
        public void AddButton(string text, Action callback, LayoutType type = LayoutType.Left)
        {

            AddGUI(new Button(text, callback, type));

        }
        public void AddTriggerButton(string trueText, string falseText, Action<bool> callback, LayoutType type = LayoutType.Left)
        {

            AddGUI(new TriggerButton(trueText, falseText, callback, type));

        }
        public void AddGUI(GUIBase gui)
        {
            drawList.Add(gui);
        }
        public void Draw()
        {
            foreach (var gui in drawList)
            {
                gui.OnDraw();
            }
        }
    }
}