namespace CentralListeners.WebApi.Models
{
    public class Word
    {
        /// <summary>
        /// Link to appsettings MarqueeText SpeechLane
        /// </summary>
        public string SpeakerName { get; set; }

        public DateTime Start { get; set; }
        public DateTime Stop { get; set; }

        public float Probability { get; set; }
        public string Text { get; set; }
    }
}
