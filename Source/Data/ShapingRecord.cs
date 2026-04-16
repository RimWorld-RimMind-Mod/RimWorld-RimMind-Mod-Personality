using Verse;

namespace RimMind.Personality.Data
{
    public class ShapingRecord : IExposable
    {
        public string label = string.Empty;
        public string action = string.Empty;
        public int tick;

        public void ExposeData()
        {
#pragma warning disable CS8601
            Scribe_Values.Look(ref label, "label", string.Empty);
            Scribe_Values.Look(ref action, "action", string.Empty);
#pragma warning restore CS8601
            Scribe_Values.Look(ref tick, "tick");
        }
    }
}
