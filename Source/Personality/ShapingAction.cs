namespace RimMind.Personality
{
    /// <summary>
    /// Player shaping vote action for personality thoughts.
    /// Replaces string magic values "reinforce"/"suppress"/"ignored".
    /// </summary>
    public enum ShapingAction
    {
        Reinforce,
        Suppress,
        Ignore
    }

    public static class ShapingActionExtensions
    {
        /// <summary>
        /// Converts to the serialized string form used in ShapingRecord.action and UI callbacks.
        /// </summary>
        public static string ToActionString(this ShapingAction action) => action switch
        {
            ShapingAction.Reinforce => "reinforce",
            ShapingAction.Suppress => "suppress",
            _ => "ignored",
        };

        /// <summary>
        /// Parses a serialized string back to the enum. Unknown/null defaults to Ignore.
        /// </summary>
        public static ShapingAction FromString(string? value) => value switch
        {
            "reinforce" => ShapingAction.Reinforce,
            "suppress" => ShapingAction.Suppress,
            _ => ShapingAction.Ignore,
        };
    }
}
