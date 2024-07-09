namespace WebApi.Models
{
    public class MarqueeText
    {
        public SpeechLane[] SpeechLanes { get; set; }
    }

    public class SpeechLane
    {
        public string Name { get; set; }
        public string Color { get; set; }
        public int SortOrder { get; set; }
    }
}
