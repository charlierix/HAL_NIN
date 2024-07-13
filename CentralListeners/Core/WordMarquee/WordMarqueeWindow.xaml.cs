using Game.Math_WPF.Mathematics;
using Game.Math_WPF.WPF;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

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
            public Canvas Canvas { get; init; }
            public Queue<Word> Pending { get; init; }
            public List<WordShowing> Showing { get; init; }

            /// <summary>
            /// This keeps increasing when pending list is too large, slowly goes down to baseline when count is zero
            /// </summary>
            public double CurrentSpeed { get; set; }

            public void AdjustSpeed(Settings settings, double elapsed_seconds)
            {
                const int ZEROTHRESH_MINCOUNT = 1;
                const int ZEROTHRESH_MAXCOUNT = 3;

                const double OVER_SLOPE = -18;     // speed increases by this * count per second (when count > ZEROTHRESH_MAXCOUNT) --- negative because speed is negative (words go from right to left)
                const double UNDER_SLOPE = 6;

                int count = Pending.Count;

                double rate;
                if (count < ZEROTHRESH_MINCOUNT)
                    rate = (ZEROTHRESH_MINCOUNT - count) * UNDER_SLOPE;

                else if (count > ZEROTHRESH_MAXCOUNT)
                    rate = (count - ZEROTHRESH_MAXCOUNT) * OVER_SLOPE;

                else
                    return;

                CurrentSpeed = Math.Min(settings.Speed, CurrentSpeed + rate * elapsed_seconds);
            }
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

        private const double GAP_PERCENTHEIGHT = 0.333;     // gap between words is height * this percent

        private readonly Settings _settings;

        private readonly List<LaneCanvas> _lanes = [];
        private readonly string _unmatched_lane_name = Guid.NewGuid().ToString();

        private readonly DispatcherTimer _timer;

        private DateTime? _prev_tick = null;

        #endregion

        #region Constructor

        public WordMarqueeWindow(Settings settings)
        {
            InitializeComponent();

            _settings = settings;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(1);
            _timer.Tick += Timer_Tick;
        }

        #endregion

        #region Public Methods

        public void AddLane(Lane lane)
        {
            try
            {
                // Create Canvas
                if (_lanes.Any(o => o.Lane.Name == lane.Name))
                    throw new ArgumentException($"Lane name already exists: '{lane.Name}'");

                int insert_index = GetInsertIndex(lane);

                Color forecolor = UtilityWPF.ColorFromHex(lane.Color);
                //Color backcolor = UtilityWPF.GetRandomColor(32, 64, 192);

                Canvas canvas = new Canvas()
                {
                    //Background = new SolidColorBrush(backcolor),
                    ClipToBounds = true,
                    IsHitTestVisible = false,
                };
                panel.Children.Insert(insert_index, canvas);

                // Figure out the height
                TextBlock textblock = new TextBlock()
                {
                    Foreground = Brushes.Transparent,
                    FontSize = _settings.FontSize_Max,
                    Text = "Hello There",
                };
                canvas.Children.Add(textblock);

                textblock.Measure(new Size(10000, 10000));
                double canvas_height = textblock.DesiredSize.Height + _settings.Vertical_Padding * 2;

                canvas.Children.Clear();

                canvas.Height = canvas_height;

                _lanes.Insert(insert_index, new LaneCanvas()
                {
                    Lane = lane,
                    Brush = new SolidColorBrush(forecolor),
                    Canvas_Y = canvas_height / 2,
                    Canvas = canvas,
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
                LaneCanvas lane = _lanes.FirstOrDefault(o => o.Lane.Name == word.LaneName);
                if (lane == null)
                    lane = GetUnmatchedLane();

                lane.Pending.Enqueue(word);

                _timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Event Listeners

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                PlaceWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                if (e.HeightChanged)
                    PlaceWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_lanes.Count == 0 || _lanes.All(o => o.Pending.Count == 0 && o.Showing.Count == 0))
                {
                    _prev_tick = null;
                    _timer.Stop();
                    return;
                }

                DateTime now = DateTime.UtcNow;

                if (_prev_tick != null)
                {
                    double elapsed_seconds = (now - _prev_tick.Value).TotalSeconds;

                    foreach (var lane in _lanes)
                        MoveWords(lane, elapsed_seconds);
                }

                foreach (var lane in _lanes)
                    TryRenderNewWord(lane);

                _prev_tick = now;
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

        /// <summary>
        /// Slides the positions of the words
        /// </summary>
        private void MoveWords(LaneCanvas lane, double elapsed_seconds)
        {
            if (_settings.Speed >= 0)
                throw new InvalidOperationException($"This function is hardcoded for negative speeds: {_settings.Speed}");

            elapsed_seconds = Math.Min(0.5, elapsed_seconds);       // capping time in case there's a lag spike

            lane.AdjustSpeed(_settings, elapsed_seconds);
            double delta = lane.CurrentSpeed * elapsed_seconds;

            int index = 0;

            while (index < lane.Showing.Count)
            {
                WordShowing word = lane.Showing[index];

                word.Position = new Point(word.Position.X + delta, word.Position.Y);

                if (word.Position.X + word.Size.Width < 0)
                {
                    lane.Canvas.Children.Remove(word.TextBlock);
                    lane.Showing.RemoveAt(index);
                }
                else
                {
                    // TODO: test if doing a translate transform has difference in performance

                    Canvas.SetLeft(word.TextBlock, word.Position.X);
                    //Canvas.SetTop(word.TextBlock, word.Position.Y);       // start setting this if it could move vertically
                    index++;
                }
            }
        }

        private void TryRenderNewWord(LaneCanvas lane)
        {
            if (!CanAddWord(lane))
                return;

            Word word = lane.Pending.Dequeue();

            var textblock = new OutlinedTextBlock()
            {
                Text = word.Text,
                FontWeight = FontWeights.Black,
                Fill = lane.Brush,
                Stroke = new SolidColorBrush(Color.FromArgb(192, 0, 0, 0)),
                StrokeThickness = 1,
                FontSize = UtilityMath.GetScaledValue_Capped(_settings.FontSize_Min, _settings.FontSize_Max, 0, 1, word.Probability),
                //Opacity = GetScaledValue(OPACITY_MIN, OPACITY_MAX, 0, 1, word.Probability),       // can't use semitransparent, since dropshadow effect will darken it
                IsHitTestVisible = false,
                Effect = new DropShadowEffect()
                {
                    Color = Colors.Black,
                    Direction = 0,
                    ShadowDepth = 0,
                    BlurRadius = UtilityMath.GetScaledValue_Capped(_settings.Blur_Min, _settings.Blur_Max, 0, 1, word.Probability),
                },
            };

            lane.Canvas.Children.Add(textblock);

            //textblock.Measure(lane.Canvas.RenderSize);        // when fontsize gets huge, long words could exceed screen width.  The under measurment of width would make the move method remove the word early
            textblock.Measure(new Size(50000, 10000));
            Size size = textblock.DesiredSize;

            double x = lane.Canvas.ActualWidth + 10;
            if (lane.Showing.Count > 0)
            {
                var prev = lane.Showing[^1];

                double prev_right = prev.Position.X + prev.Size.Width;
                double gap = (prev.Size.Height * GAP_PERCENTHEIGHT + size.Height * GAP_PERCENTHEIGHT) / 2;

                x = Math.Max(x, prev_right + gap);
            }

            Canvas.SetLeft(textblock, x);

            double y = lane.Canvas_Y - size.Height / 2;
            Canvas.SetTop(textblock, y);

            lane.Showing.Add(new WordShowing()
            {
                Word = word,
                TextBlock = textblock,
                Size = size,
                Position = new Point(x, y),
            });
        }

        private void PlaceWindow()
        {
            Rect screen = UtilityWPF.GetCurrentScreen(PointToScreen(new Point()));

            Left = screen.Left;
            Width = screen.Width;

            if (ActualHeight > screen.Height)
                Top = 0;

            else if (ActualHeight > screen.Height - _settings.Screen_Bottom_Margin)
                Top = screen.Height / 2 - ActualHeight / 2;     // if the font is that big, just center it

            else
                Top = screen.Bottom - ActualHeight - _settings.Screen_Bottom_Margin;
        }

        /// <summary>
        /// Creates unmatched lane if it doesn't exist, then returns the unmatched lane
        /// </summary>
        private LaneCanvas GetUnmatchedLane()
        {
            LaneCanvas lane = _lanes.FirstOrDefault(o => o.Lane.Name == _unmatched_lane_name);
            if (lane != null)
                return lane;

            AddLane(new Lane()
            {
                Name = _unmatched_lane_name,
                Color = "F0F",      // Colors.Magenta
                SortOrder = int.MaxValue,
            });

            return _lanes.First(o => o.Lane.Name == _unmatched_lane_name);      // should be _lanes[^1], but search just to be safe
        }

        private static bool CanAddWord(LaneCanvas lane)
        {
            if (lane.Pending.Count == 0)
                return false;

            if (lane.Showing.Count == 0)
                return true;

            double rightmost_pos = lane.Showing[^1].Position.X + lane.Showing[^1].Size.Width;

            // Don't wait until it's fully on the screen.  Allow new words to be added to the right
            return rightmost_pos <= lane.Canvas.ActualWidth + (lane.Showing[^1].Size.Width * 1.5);
        }

        #endregion
    }
}
