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

namespace TestApp
{
    public partial class MainWindow : Window
    {
        private readonly List<Lane> _lanes = new List<Lane>();

        public MainWindow()
        {
            InitializeComponent();

            Background = SystemColors.ControlBrush;
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

                WordMarqueeManager2.AddLane(_lanes[^1]);
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
                string lane_name = _lanes.Count > 0 ?
                    _lanes[StaticRandom.Next(_lanes.Count)].Name :
                    Guid.NewGuid().ToString();      // allow an invalid name so that lower code can be tested

                WordMarqueeManager2.AddWord(new Word()
                {
                    LaneName = lane_name,
                    Probability = StaticRandom.NextPow(0.3),
                    Text = GetRandomText(StaticRandom.GetRandomForThread()),
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string GetRandomText(Random rand)
        {
            const string POSSIBLE = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

            return Enumerable.Range(0, rand.Next(1, 9)).
                Select(_ => POSSIBLE[rand.Next(POSSIBLE.Length)].ToString()).
                ToJoin("");
        }
    }
}