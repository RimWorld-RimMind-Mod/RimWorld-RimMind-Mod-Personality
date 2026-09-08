using System.Collections.Generic;

namespace Verse
{
    public struct TaggedString
    {
        public string Value;
        public static implicit operator string(TaggedString ts) => ts.Value;
        public static implicit operator TaggedString(string s) => new TaggedString { Value = s };
        public override string ToString() => Value ?? "";
    }

    public interface IExposable
    {
        void ExposeData();
    }

    public class ModSettings
    {
        public virtual void ExposeData() { }
    }

    public static class ScribeRecorder
    {
        public static readonly List<string> Labels = new List<string>();
        public static readonly List<(string Label, object? DefaultValue)> ValueCalls =
            new List<(string, object?)>();
        public static readonly List<(string Label, LookMode KeyMode, LookMode ValueMode)> CollectionCalls =
            new List<(string, LookMode, LookMode)>();

        public static void Reset()
        {
            Labels.Clear();
            ValueCalls.Clear();
            CollectionCalls.Clear();
            Scribe_Collections.AssignNullOnLook = false;
            Scribe_Deep.AssignNullOnLook = false;
        }
    }

    public static class Scribe_Values
    {
        public static void Look<T>(ref T value, string label, T? defaultValue = default!)
        {
            ScribeRecorder.Labels.Add(label);
            ScribeRecorder.ValueCalls.Add((label, defaultValue));
        }
    }

    public static class Scribe_Collections
    {
        public static bool AssignNullOnLook { get; set; }

        public static void Look<T>(ref List<T> list, string label, LookMode lookMode)
        {
            ScribeRecorder.Labels.Add(label);
            ScribeRecorder.CollectionCalls.Add((label, LookMode.Undef, lookMode));
            if (AssignNullOnLook) list = null!;
        }

        public static void Look<T>(ref List<T> list, string label)
        {
            ScribeRecorder.Labels.Add(label);
            ScribeRecorder.CollectionCalls.Add((label, LookMode.Undef, LookMode.Undef));
            if (AssignNullOnLook) list = null!;
        }

        public static void Look<TKey, TValue>(
            ref Dictionary<TKey, TValue> dict,
            string label,
            LookMode keyLookMode,
            LookMode valueLookMode)
            where TKey : notnull
        {
            ScribeRecorder.Labels.Add(label);
            ScribeRecorder.CollectionCalls.Add((label, keyLookMode, valueLookMode));
            if (AssignNullOnLook) dict = null!;
        }
    }

    public static class Scribe_Deep
    {
        public static bool AssignNullOnLook { get; set; }

        public static void Look<T>(ref T target, string label)
        {
            ScribeRecorder.Labels.Add(label);
            if (AssignNullOnLook) target = default!;
        }
    }

    public enum LookMode { Undef, Value, Deep }

    public static class Log
    {
        public static void Warning(string msg) { }
        public static void Message(string msg) { }
        public static void Error(string msg) { }
    }

    public static class Extensions
    {
        public static bool NullOrEmpty(this string? s) => string.IsNullOrEmpty(s);
    }
}

namespace RimWorld.Planet
{
    public class WorldComponent
    {
        public WorldComponent(World world) { }
        public virtual void ExposeData() { }
    }

    public class World { }
}
