using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Core.WordMarquee
{
    public partial class WordMarqueeWindow : Window
    {
        public WordMarqueeWindow()
        {
            InitializeComponent();

            Background = SystemColors.ControlBrush;
        }

        public void AddLane(Lane lane)
        {
            try
            {
                string newline = txtReport.Text.Length > 0 ? "\r\n" : "";
                txtReport.Text += $"{newline}DefineLane: {lane}";
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
    }
}
