using MAFTesters_Core;
using MAFTesters_Core.Tools;
using MAFTesters_PythonSandboxMockService;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace MAFTesters_WPF
{
    public partial class UnitTestWindow : Window
    {
        private record DockerSessionArgs
        {
            public string Folder { get; init; }
        }

        public UnitTestWindow()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                // Use Process.Start to open the URI in the default web browser
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
        private void GetJSONSchema_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string prompt1 = "do what feels good";

                string prompt2 = StronglyTypedPromptHelper<PythonWriter.ValidatorResponse[]>.AppendToPrompt(prompt1);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void DeserializeLooseJSON_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string json =
@"```json
{
  ""NewPrompt"": ""
  Generate a Python script that calculates the **32nd Fibonacci number** using a **1-based indexing scheme** (where F(1) = 0, F(2) = 1, F(3) = 1, etc.).
  The script must include the following requirements:

  1. **Functionality**:
     - Implement a function `fibonacci(n)` that returns the nth Fibonacci number (1-based indexing).
     - The function must handle edge cases (e.g., `n = 1` returns `0`, `n = 2` returns `1`).
     - Use an **iterative approach** for efficiency (avoid recursion depth issues).

  2. **Error Handling**:
     - Raise a `ValueError` with a **clear, descriptive message** if `n` is not a positive integer (e.g., 'n must be a positive integer (n > 0)'). Ensure the error message explicitly states that `n` must be an integer (not a float or other type).
     - The error message should distinguish between invalid types (e.g., floats) and non-positive integers.

  3. **Modularity and Reusability**:
     - Avoid hardcoding the value of `n` in the main script. Instead, allow `n` to be passed as a **command-line argument** or **user input** (e.g., via `argparse` or `input()`).
     - Include a `main()` function to demonstrate usage, but ensure the script can be reused for other Fibonacci numbers without modification.

  4. **Documentation**:
     - Add a **clear docstring** to `fibonacci(n)` that explicitly states:
       - The 1-based indexing scheme (F(1) = 0, F(2) = 1, etc.).
       - The expected input type (`n` must be a positive integer).
       - The output type (integer).
     - Include a **usage example** in the docstring or comments.

  5. **Performance Considerations**:
     - The iterative approach is acceptable for typical use cases (e.g., `n <= 10^6`). However, if performance for very large `n` (e.g., `n > 10^6`) becomes a concern, consider optimizing further (e.g., matrix exponentiation or memoization), but this is optional for the initial implementation.

  6. **Testing**:
     - Include a test case in the script to verify correctness (e.g., `assert fibonacci(1) == 0`, `assert fibonacci(2) == 1`, `assert fibonacci(32) == 2178309`).
     - Handle invalid inputs (e.g., `n = 0`, `n = -5`, `n = 32.5`) to ensure the error messages are displayed correctly.

  7. **Output**:
     - Print the result in a user-friendly format (e.g., 'The 32nd Fibonacci number (1-based) is: 2178309').
     - If an error occurs, print the error message to the console and exit gracefully.

  Example Output:
  ```
  The 32nd Fibonacci number (1-based) is: 2178309
  ```
  Or, if invalid input:
  ```
  Error: n must be a positive integer (n > 0). Please provide a valid integer.
  ```"",

  ""SummarizedErrorList"": [
    {
      ""Severity"": ""Error"",
      ""Description"": ""The Fibonacci sequence definition in the docstring or implementation was ambiguous or contradictory. Explicitly clarify that the sequence uses **1-based indexing** (F(1) = 0, F(2) = 1) to avoid confusion with 0-based indexing.""
    },
    {
      ""Severity"": ""Error"",
      ""Description"": ""The error message for invalid inputs (e.g., non-integer or non-positive `n`) was unclear or insufficient. Ensure the error message explicitly states that `n` must be a **positive integer** and distinguishes between invalid types (e.g., floats) and values.""
    },
    {
      ""Severity"": ""Warning"",
      ""Description"": ""The script hardcoded `n = 32` in the main block, making it inflexible. Allow `n` to be passed as a **command-line argument** or **user input** for reusability.""
    },
    {
      ""Severity"": ""Warning"",
      ""Description"": ""The docstring for `fibonacci(n)` did not explicitly mention the **1-based indexing scheme** or the expected input/output types. Update the docstring to include this information.""
    },
    {
      ""Severity"": ""Warning"",
      ""Description"": ""The error handling in the main script printed errors to the console but did not raise exceptions or return non-zero exit codes. For integration with larger systems, consider raising exceptions or using proper exit codes.""
    },
    {
      ""Severity"": ""Warning"",
      ""Description"": ""While the iterative approach is efficient for most cases, it may not be optimal for extremely large `n` (e.g., `n > 10^6`). Consider adding a note about potential optimizations (e.g., matrix exponentiation) for future scalability.""
    }
  ]
}
```";

                var result = StronglyTypedPromptHelper<PythonWriter.PromptRefine>.ParseResponse(json);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RemoveOuterFence_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var test_cases1 = new (string input, string expected)[]
                {
                    (
                        input: "",
                        expected: ""
                    ),
                    (
                        input: "This is a normal text with no code blocks.",
                        expected: "This is a normal text with no code blocks."
                    ),
                    (
                        input: "```code\nSome code here\n```",
                        expected: "Some code here\n"
                    ),
                    (
                        input: "```\n```",
                        expected: ""
                    ),
                    (
                        input: "```code\n```",
                        expected: ""
                    ),
                    (
                        input: "```\nFirst block\n```\n```\nSecond block\n```",
                        expected: "First block\n```\n```\nSecond block\n"
                    ),
                    (
                        input: "```first\nFirst block\n```\n```second\nSecond block\n```",
                        expected: "First block\n```\n```second\nSecond block\n"
                    ),
                    (
                        input: "```\n```\ninner\n```\n```",
                        expected: "```\ninner\n```\n"
                    ),
                    (
                        input: "```\n```code\ninner\n```\n```",
                        expected: "```code\ninner\n```\n"
                    ),
                    (
                        input: "```outer\n```code\ninner\n```\n```",
                        expected: "```code\ninner\n```\n"
                    ),
                    (
                        input: "   \n```\nContent\n```",
                        expected: "Content\n"
                    ),
                    (
                        input: "   \n```cont\nContent\n```",
                        expected: "Content\n"
                    ),
                    (
                        input: "```\nLine1\nLine2\n```",
                        expected: "Line1\nLine2\n"
                    ),
                    (
                        input: "```lines\nLine1\nLine2\n```",
                        expected: "Line1\nLine2\n"
                    ),
                    (
                        input: "abc\n```\nContent\n```",
                        expected: "abc\n```\nContent\n```"
                    ),
                    (
                        input: "abc\n```later\nContent\n```",
                        expected: "abc\n```later\nContent\n```"
                    ),
                    (
                        input: "```\nContent\n```\nxyz",
                        expected: "```\nContent\n```\nxyz"
                    ),
                    (
                        input: "```\n```\nBlock2\n```\n```\nBlock3\n```",
                        expected: "```\nBlock2\n```\n```\nBlock3\n"
                    ),
                    (
                        input: "```\n```block\nBlock2\n```\n```lockb\nBlock3\n```",
                        expected: "```block\nBlock2\n```\n```lockb\nBlock3\n"
                    ),
                    (
                        input: "abc\n   \n```\nContent\n```",
                        expected: "abc\n   \n```\nContent\n```"
                    ),
                    (
                        input: "```\nContent\n```\nMore text",
                        expected: "```\nContent\n```\nMore text"
                    ),
                    (
                        input: "```\nBlock1\n```\n```\nBlock2\n```\n```\nBlock3\n```",
                        expected: "Block1\n```\n```\nBlock2\n```\n```\nBlock3\n"
                    ),
                    (
                        input: "```\n\n```",
                        expected: "\n"
                    ),
                    (
                        input: "```\n```\n```\ninner\n```\n```\n```",
                        expected: "```\n```\ninner\n```\n```\n"
                    ),
                    (
                        input: "```\n\n\n\n```",
                        expected: "\n\n\n"
                    )
                };

                var test_cases2 = test_cases1.
                    Select(o =>
                    (
                        input: o.input.Replace("\n", "\r\n"),
                        expected: o.expected.Replace("\n", "\r\n")
                    )).
                    ToArray();

                var test_cases = test_cases1.
                    Concat(test_cases2).
                    ToArray();



                var results = test_cases.
                    Select(o => new
                    {
                        input = o.input,
                        expected = o.expected,
                        result = MarkdownParser.RemoveOuterFence(o.input),
                    }).
                    ToArray();



                var mismatches = results.
                    Where(o => o.result != o.expected).
                    ToArray();


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Docker_Init_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var args = GetDockerSessionArgs();

                await PythonSandboxMockService.Init(args.Folder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void Docker_AddRemoveSession_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var args = GetDockerSessionArgs();

                await PythonSandboxMockService.Init(args.Folder);

                string session_name = "unit test add/remove session";

                var add_response = await PythonSandboxMockService.NewSession(session_name);

                var remove_response = await PythonSandboxMockService.RemoveSession(session_name);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DockerSessionArgs GetDockerSessionArgs()
        {
            if (!Directory.Exists(txtFolder.Text))
                throw new ApplicationException($"Docker folder doesn't exist: {txtFolder.Text}");

            return new DockerSessionArgs
            {
                Folder = txtFolder.Text,
            };
        }
    }
}
