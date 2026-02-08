using MAFTesters_Core;
using System.IO;
using System.Windows;

namespace MAFTesters_WPF
{
    public partial class UnitTestWindow : Window
    {
        public UnitTestWindow()
        {
            InitializeComponent();
        }

        private void EnsureVenv_Click(object sender, RoutedEventArgs e)
        {
            const string FOLDER = @"D:\temp\agent tests\a";

            try
            {
                string python_folder = Path.Combine(FOLDER, "PythonSandbox");

                PythonUtils.EnsurePythonFolderInitialized(python_folder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PyFlakes_Click(object sender, RoutedEventArgs e)
        {
            const string FOLDER = @"D:\temp\agent tests\a";

            try
            {
                string python_folder = Path.Combine(FOLDER, "PythonSandbox");

                string? error_valid = PythonUtils.CheckForErrors(python_folder, "example - valid.py");
                string? error_errors = PythonUtils.CheckForErrors(python_folder, "example - errors.py");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ParseMarkdown_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string text =
@"```python
def get_log_folder():
    retVal = 'logs/' + datetime.datetime.now().strftime('%Y-%m-%d %H-%M-%S')
    os.makedirs(retVal)
    return retVal
```";

                string parsed = MarkdownParser.ExtractOnlyText(text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
