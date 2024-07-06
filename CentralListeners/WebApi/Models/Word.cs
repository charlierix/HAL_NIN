namespace CentralListeners.WebApi.Models
{
    public class Word
    {
        public DateTime Start { get; set; }
        public DateTime Stop { get; set; }
        public float Probability { get; set; }
        public string Text { get; set; }
    }
}
