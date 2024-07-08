using Game.Math_WPF.WPF;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Core.WordMarquee
{
    public partial class WordMarqueeWindow : Window
    {
        #region record: LaneCanvas

        private record LaneCanvas
        {
            public Lane Lane { get; init; }
            public Brush Brush { get; init; }
            public double Canvas_Y { get; init; }
            public Queue<Word> Pending { get; init; }
            public List<WordShowing> Showing { get; init; }
        }

        #endregion
        #region record: WordShowing

        private record WordShowing
        {
            public Word Word { get; init; }

            public OutlinedTextBlock TextBlock { get; init; }
            public Size Size { get; init; }

            // This gets updated each tick
            public Point Position { get; set; }
        }

        #endregion

        #region Declaration Section

        //private const double SPEED = -250;        // pixels per second
        private const double SPEED = -50;

        private const double FONTSIZE_MIN = 18;
        private const double FONTSIZE_MAX = 48;

        private const double BLUR_MIN = 2;
        private const double BLUR_MAX = 6;

        private const double GAP_PERCENTHEIGHT = 0.333;     // gap between words is height * this percent

        private readonly List<LaneCanvas> _lanes = new List<LaneCanvas>();

        #endregion

        #region Constructor

        public WordMarqueeWindow()
        {
            InitializeComponent();

            Background = SystemColors.ControlBrush;
        }

        #endregion

        #region Public Methods

        public void AddLane(Lane lane)
        {
            try
            {
                // Report
                string newline = txtReport.Text.Length > 0 ? "\r\n" : "";
                txtReport.Text += $"{newline}DefineLane: {lane}";

                // Create Canvas
                int insert_index = GetInsertIndex(lane);

                Color forecolor = UtilityWPF.ColorFromHex(lane.Color);
                //Color backcolor = UtilityWPF.OppositeColor(forecolor);        // all the opposites are nearly black
                Color backcolor = UtilityWPF.GetRandomColor(64, 64, 192);

                Canvas canvas = new Canvas()
                {
                    //Background = new SolidColorBrush(Color.FromArgb(32, backcolor.R, backcolor.G, backcolor.B)),
                    Background = new SolidColorBrush(backcolor),
                };
                panel.Children.Insert(insert_index, canvas);

                // Figure out the height
                TextBlock textblock = new TextBlock()
                {
                    Foreground = Brushes.Transparent,
                    FontSize = FONTSIZE_MAX,
                    Text = "Hello There",
                };
                canvas.Children.Add(textblock);

                textblock.Measure(new Size(1000, 1000));
                double canvas_height = textblock.DesiredSize.Height;        // there is already some vertical padding in this height, so no need to add more

                canvas.Children.Clear();

                canvas.Height = canvas_height;

                _lanes.Insert(insert_index, new LaneCanvas()
                {
                    Lane = lane,
                    Brush = new SolidColorBrush(forecolor),
                    Canvas_Y = canvas_height / 2,
                    Pending = new Queue<Word>(),
                    Showing = new List<WordShowing>(),
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void AddWord(Word word)
        {
            try
            {
                string newline = txtReport.Text.Length > 0 ? "\r\n" : "";
                txtReport.Text += $"{newline}AddWord: {word}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Private Methods

        private int GetInsertIndex(Lane lane)
        {
            for (int i = 0; i < _lanes.Count; i++)
            {
                if (lane.SortOrder < _lanes[i].Lane.SortOrder)
                    return i;

                // If the sort orders are the same, then make this the last entry of the set with that sort order.  Just need to
                // wait until on an entry greater than the sort order (or end of list)
            }

            return _lanes.Count;
        }

        #endregion
    }
}
