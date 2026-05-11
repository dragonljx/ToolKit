using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ToolKits
{
    public static partial class TLog
    {
        private static readonly ILogger UnityLogger = Debug.unityLogger;
        private static readonly StringBuilder TagBuilder = new StringBuilder(32);
        private const string DefaultTag = "Tool Kits";
        private const string OrangeColorPrefix = "<color=orange>";
        private const string ColorSuffix = "</color>";
        private static bool ShowFrameInTag = false;

        public static void Log(object message)
        {
            UnityLogger.Log(FormatTag(DefaultTag), message);
        }

        public static void Log(string tag, string format, params object[] args)
        {
            UnityLogger.Log(FormatTag(tag), string.Format(format, args));
        }

        public static void Log(string tag, object message)
        {
            UnityLogger.Log(FormatTag(tag), message);
        }

        public static void Log(params object[] message)
        {
            var sb = new StringBuilder();
            foreach (var m in message)
            {
                sb.Append("[");
                sb.Append(m?.ToString());
                sb.Append("] ");
            }

            UnityLogger.Log(FormatTag(DefaultTag), sb.ToString().TrimEnd());
        }

        public static void LogWarning(string tag, string format, params object[] args)
        {
            UnityLogger.LogWarning(FormatTag(tag), string.Format(format, args));
        }

        public static void LogWarning(string tag, object message)
        {
            UnityLogger.LogWarning(FormatTag(tag), message);
        }

        public static void LogError(string tag, string format, params object[] args)
        {
            UnityLogger.LogError(FormatTag(tag), string.Format(format, args));
        }

        public static void LogError(string tag, object message)
        {
            UnityLogger.LogError(FormatTag(tag), message);
        }


        static string FormatTag(string tag)
        {
            TagBuilder.Clear();

            TagBuilder.Append(OrangeColorPrefix).Append('[').Append(tag).Append(']').Append(ColorSuffix).Append(' ');
            if (ShowFrameInTag)
            {
                TagBuilder.AppendFormat("[Frame: {0}]", Time.frameCount);
            }

            return TagBuilder.ToString();
        }

        public static void Log(Transform message)
        {
            StringBuilder str = new StringBuilder();
            str.Append(" <color=teal>[Transform]</color> P: ");
            str.Append(message.position.ToString("F3"));
            str.Append("  R: ");
            str.Append(message.rotation.ToString("F3"));
            str.Append("  S: ");
            str.Append(message.localScale.ToString("F3"));

            UnityLogger.Log(FormatTag(DefaultTag), str.ToString());
        }

        public static void LogWarning(object message)
        {
            UnityLogger.LogWarning(FormatTag(DefaultTag), ParsingObject(message));
        }

        public static void LogWarning(params object[] messages)
        {
            // string logMessage = string.Join("\n", messages);
            string logMessage = ParsingObjects(messages);
            UnityLogger.LogWarning(FormatTag(DefaultTag), logMessage);
        }

        public static void LogError(object message)
        {
            UnityLogger.LogError(FormatTag(DefaultTag), ParsingObject(message));
        }

        public static void LogError(params object[] messages)
        {
            // string logMessage = string.Join("\n", messages);
            string logMessage = ParsingObjects(messages);
            UnityLogger.LogError(FormatTag(DefaultTag), logMessage);
        }

        static string ParsingObjects(object[] messages)
        {
            StringBuilder logMessage = new StringBuilder();
            int count = messages.Length;
            for (int i = 0; i < messages.Length; i++)
            {
                ParsingObject(messages[i], logMessage, count - i);
            }


            return logMessage.ToString();
        }

        static object ParsingObject(object message)
        {
            StringBuilder logMessage = new StringBuilder();
            ParsingObject(message, logMessage);
            return logMessage;
        }

        static void ParsingObject(object message, StringBuilder logMessage, int index = -1, bool isLooped = true)
        {
            if (!message.GetType().IsPrimitive && !(message is string))
            {
                Type type = message.GetType();
                // Append type name and index
                if (index == -1)
                {
                    logMessage.Append($"{type.Name} : ");
                }
                else
                {
                    logMessage.Append($"[{index}] {type.Name} : ");
                }

                if (!isLooped)
                {
                    WrieMessage(type);
                    return;
                }

                // Handle arrays
                if (type.IsArray)
                {
                    var array = (IEnumerable)message;
                    int count = 0;
                    foreach (var element in array)
                    {
                        logMessage.Append($"<color=#2BBBD8> {count} </color>: ");
                        ParsingObject(element, logMessage, count, false);
                        count++;
                        logMessage.Append("\n");
                    }
                }
                // Handle List<>
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    int count = 0;
                    foreach (object element in (IEnumerable)message)
                    {
                        logMessage.Append($"<color=#2BBBD8>{count}</color>: ");
                        ParsingObject(element, logMessage, count, false);
                        count++;
                        logMessage.Append("\n");
                    }
                }
                // Handle Dictionary<,>
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    var dictionary = (IDictionary)message;
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        logMessage.Append($"<color=#2BBBD8> Key </color>: ");
                        ParsingObject(entry.Key, logMessage, isLooped: false);
                        logMessage.Append($"<color=#2BBBD8> Value </color>: ");
                        ParsingObject(entry.Value, logMessage, isLooped: false);
                        logMessage.Append("\n");
                    }
                }
                // Handle other complex objects
                else
                {
                    WrieMessage(type);
                }
            }
            else
            {
                logMessage.Append(message);
            }

            logMessage.Append("\n");

            void WrieMessage(Type type)
            {
                logMessage.Append("[ ");
                FieldInfo[] fields = type.GetFields();
                foreach (FieldInfo field in fields)
                {
                    try
                    {
                        var value = field.GetValue(message);
                        logMessage.Append($"<color=#2BBBD8> {field.Name} </color>: {value}");
                    }
                    catch (Exception ex)
                    {
                        logMessage.Append(
                            $"<color=#2BBBD8> {field.Name} </color>: Error accessing property: {ex.Message} ");
                    }
                }

                logMessage.Append(" ]");
            }
        }
    }
}