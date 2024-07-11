using Core.WordMarquee;
using Game.Core;
using Game.Math_WPF.Mathematics;
using Game.Math_WPF.WPF;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TestApp
{
    public partial class MainWindow : Window
    {
        private readonly List<Lane> _lanes = new List<Lane>();

        private readonly DispatcherTimer _timer_delayed_word;
        private DateTime _delay_time;

        public MainWindow()
        {
            InitializeComponent();

            Background = SystemColors.ControlBrush;

            _timer_delayed_word = new DispatcherTimer();
            _timer_delayed_word.Interval = TimeSpan.FromMicroseconds(333);
            _timer_delayed_word.Tick += Timer_DelayedWord_Tick;

            SetMarqueeSettings();
        }

        private void WordMarquee_AddLane_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _lanes.Add(new Lane()
                {
                    Name = $"lane name {_lanes.Count + 1}",
                    Color = UtilityWPF.ColorToHex(UtilityWPF.GetRandomColor(192, 255), false, false),
                    SortOrder = StaticRandom.Next(0, 12),
                });

                WordMarqueeManager.AddLane(_lanes[^1]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void WordMarquee_AddWord_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddWord();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddWordTwoMins_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _delay_time = DateTime.UtcNow + TimeSpan.FromMinutes(2);
                _timer_delayed_word.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Timer_DelayedWord_Tick(object? sender, EventArgs e)
        {
            try
            {
                DateTime now = DateTime.UtcNow;

                if (now >= _delay_time)
                {
                    _timer_delayed_word.Stop();
                    lblTime.Visibility = Visibility.Collapsed;
                    AddWord();
                }
                else
                {
                    lblTime.Visibility = Visibility.Visible;
                    lblTime.Text = $"{(_delay_time - now).TotalSeconds.ToInt_Round()} seconds";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddWord()
        {
            string lane_name = _lanes.Count > 0 ?
                _lanes[StaticRandom.Next(_lanes.Count)].Name :
                Guid.NewGuid().ToString();      // allow an invalid name so that lower code can be tested

            WordMarqueeManager.AddWord(new Word()
            {
                LaneName = lane_name,
                Probability = StaticRandom.NextPow(0.3),
                Text = GetRandomText(StaticRandom.GetRandomForThread()),
            });
        }

        private static string GetRandomText(Random rand)
        {
            const string POSSIBLE = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

            return Enumerable.Range(0, rand.Next(1, 9)).
                Select(_ => POSSIBLE[rand.Next(POSSIBLE.Length)].ToString()).
                ToJoin("");
        }

        private static void SetMarqueeSettings()
        {
            WordMarqueeManager.StoreSettings(new Settings()
            {
                FontSize_Min = 18,
                FontSize_Max = 48,

                Blur_Min = 3,
                Blur_Max = 9,

                Vertical_Padding = 4,

                Speed = -250,

                Screen_Bottom_Margin = 66,
            });
        }
    }
}