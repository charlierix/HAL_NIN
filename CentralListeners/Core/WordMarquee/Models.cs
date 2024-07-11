using Game.Math_WPF.Mathematics;

namespace Core.WordMarquee
{
    public record Settings
    {
        public double FontSize_Min { get; init; }
        public double FontSize_Max { get; init; }

        public double Blur_Min { get; init; }
        public double Blur_Max { get; init; }

        public double Vertical_Padding { get; init; }

        public double Speed { get; init; }

        public double Screen_Bottom_Margin { get; init; }
    }

    /// <summary>
    /// Defines a slot that words will scroll through.  There can be multiple lanes visible at the same time
    /// </summary>
    public record Lane
    {
        /// <summary>
        /// Unique name, referenced from word
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Foreground color of the words.  All words will have a black outline
        /// </summary>
        /// <remarks>
        /// Since the lanes are transparent background, the words need to be visible over light or dark
        /// parts of the screen
        /// 
        /// The foreground color should be bright so that it shows over dark areas and the black outline
        /// will make it visible over bright spots
        /// </remarks>
        public string Color { get; init; }

        /// <summary>
        /// When multiple lanes are showing at the same time, they try to honor the sort order property
        /// (larger numbers go below smaller, same number are next to each other, but arbitrary relative
        /// to each other)
        /// </summary>
        public int SortOrder { get; init; }

        public override string ToString()
        {
            return $"{Name} | #{Color} | SortOrder: {SortOrder}";
        }
    }

    /// <summary>
    /// A word that gets displayed
    /// </summary>
    public record Word
    {
        /// <summary>
        /// Link to Lane.Name - will get an exception if there are no lanes defined with that name
        /// </summary>
        public string LaneName { get; init; }

        /// <summary>
        /// 0 to 1
        /// The probability that the word is actually what was said - affects font size
        /// </summary>
        public double Probability { get; init; }

        /// <summary>
        /// The text to show on screen
        /// </summary>
        public string Text { get; init; }

        public override string ToString()
        {
            return $"Lane: {LaneName} | Prob: {Probability.ToStringSignificantDigits(2)} | {Text}";
        }
    }
}
