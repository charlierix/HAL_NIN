namespace WebApi.Models
{
    public class MarqueeText
    {
        public double Scale { get; init; }

        public double FontSize_Min { get; init; }
        public double FontSize_Max { get; init; }

        public double Blur_Min { get; init; }
        public double Blur_Max { get; init; }

        public double Vertical_Padding { get; init; }

        public double Speed { get; init; }

        public double Screen_Bottom_Margin { get; init; }

        public SpeechLane[] SpeechLanes { get; set; }
    }

    public class SpeechLane
    {
        public string Name { get; set; }
        public string Color { get; set; }
        public int SortOrder { get; set; }
    }
}
