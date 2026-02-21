using MAFTesters_Core;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;

namespace MAFTesters_Agents
{
    /// <summary>
    /// When an agent call needs to return a type, this will give it json schema to return and will parse response as that type
    /// </summary>
    public static class StronglyTypedPromptHelper<T>
    {
        /// <summary>
        /// Adds a blank line and "Please respond with", then a block that is json version of the type
        /// </summary>
        public static string AppendToPrompt(string prompt)
        {
            string schemaJson = GetSchema();

            var retVal = new StringBuilder();

            retVal.AppendLine(prompt);
            retVal.AppendLine();
            retVal.AppendLine();
            retVal.AppendLine("Please respond with json in this format (don't include schema in the response, just the final json):");
            retVal.AppendLine("```json");
            retVal.AppendLine(schemaJson);
            retVal.AppendLine("```");

            return retVal.ToString();
        }

        /// <summary>
        /// Takes the llm's response and tries to convert the json into the type
        /// </summary>
        /// <param name="throw_if_error">
        /// True: throws exception
        /// False: returns default (probably null)
        /// </param>
        /// <param name="client">
        /// If there is a parse error and this is passed in, an agent will try to repair the json based on
        /// the error message
        /// </param>
        public static async Task<T?> ParseResponse(string response, bool throw_if_error = true, IChatClient client = null)
        {
            int retry_max = client == null ?
                0 :
                3;

            Exception lastException = null;
            string working_response = response;
            ChatClientAgent agent = null;

            for (int i = 0; i <= retry_max; i++)        // using equal max, because iteration 0 is initial, not retry
            {
                try
                {
                    // got an example where json contained markdown inside a string value and this stripped too much
                    // make a lite version that strips outer ```fence ... ```, and leaves everything else alone
                    //string cleaned = MarkdownParser.ExtractOnlyText(working_response);

                    string cleaned = MarkdownParser.RemoveOuterFence(working_response);

                    // escape some chars (like newlines inside of strings)
                    // '0x0D' is invalid within a JSON string. The string should be correctly escaped
                    cleaned = EscapeJSON(cleaned);

                    var options = new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() },      // need this to turn strings into enum values
                                                                             //Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,        // not sure if this does anything on the deserialize side
                    };

                    return JsonSerializer.Deserialize<T>(cleaned, options);
                }
                catch (Exception ex)
                {
                    if (client == null)
                        break;

                    // use an agent to try to repair the text
                    if (agent == null)
                        agent = CreateAgent_JSONCleaner(client);

                    working_response = await RepairWithAgent(working_response, ex, agent);
                }
            }

            if (throw_if_error && lastException != null)
                throw new ApplicationException($"Couldn't parse the json as {nameof(T)}:{Environment.NewLine}{Environment.NewLine}{lastException.Message}{Environment.NewLine}{Environment.NewLine}{response}");
            else
                return default;
        }

        #region Private Methods

        /// <summary>
        /// To properly escape JSON strings, we need to identify and escape all characters that are invalid in JSON strings, particularly
        /// newlines and other control characters. The approach is to traverse the string and escape any character that is not valid within
        /// a JSON string, while preserving the structure of the JSON.
        /// </summary>
        /// <remarks>
        /// Here’s how the function works:
        /// 
        /// 1. Use a StringBuilder to efficiently build the result string.
        /// 2. Iterate through the input string character by character.
        /// 3. When we encounter a double quote ("), we need to check if it's inside a string value or outside. We track this with a boolean flag isInString.
        /// 4. If we're inside a string and encounter a newline (\n), we escape it as \\n.
        /// 5. For any other character that's invalid within a JSON string (e.g., control characters), we escape it appropriately.
        /// 6. We also handle escaped characters like \\ to avoid double-escaping.
        /// 
        /// The implementation handles:
        /// 
        /// - Escaping newlines within string values
        /// - Escaping other control characters (like \r, \t, etc.)
        /// - Properly escaping existing backslashes to prevent double-escaping
        /// - Preserving the structure of the JSON
        /// </remarks>
        private static string EscapeJSON(string faulty_json)
        {
            if (string.IsNullOrWhiteSpace(faulty_json))
                return faulty_json;

            var sb = new StringBuilder();
            bool isInString = false;
            int i = 0;

            while (i < faulty_json.Length)
            {
                char c = faulty_json[i];

                if (c == '"')
                {
                    // Check if this quote is part of a string or not
                    // We need to look at the previous non-whitespace character to determine if we are inside a string
                    // For simplicity, we assume that if we encounter a quote, we are inside a string
                    // This is a simplification, but works for our use case
                    isInString = !isInString;
                    sb.Append(c);
                }
                else if (isInString && c == '\n')
                {
                    // Escape newline characters inside string values
                    sb.Append("\\n");
                }
                else if (isInString && c == '\r')
                {
                    // Escape carriage return characters inside string values
                    sb.Append("\\r");
                }
                else if (isInString && c == '\t')
                {
                    // Escape tab characters inside string values
                    sb.Append("\\t");
                }
                else if (isInString && c == '\\')
                {
                    // If we encounter a backslash, we need to be careful to avoid double-escaping
                    // Append the backslash and check the next character
                    sb.Append(c);
                    i++;
                    if (i < faulty_json.Length)
                    {
                        char next = faulty_json[i];
                        // If the next char is a control character, we don't escape it since it's already escaped
                        // But if it's a valid JSON escape sequence, we keep it as is
                        // For simplicity, we just append it as-is
                        sb.Append(next);
                    }
                    else
                    {
                        // This shouldn't happen, but just in case
                        sb.Append(c);
                    }
                }
                else if (isInString && (c == '\b' || c == '\f' || c == '\u0000' || c == '\u0001' || c == '\u0002' || c == '\u0003' || c == '\u0004' || c == '\u0005' || c == '\u0006' || c == '\u0007' || c == '\u0008' || c == '\u000B' || c == '\u000C' || c == '\u000E' || c == '\u000F' || c == '\u0010' || c == '\u0011' || c == '\u0012' || c == '\u0013' || c == '\u0014' || c == '\u0015' || c == '\u0016' || c == '\u0017' || c == '\u0018' || c == '\u0019' || c == '\u001A' || c == '\u001B' || c == '\u001C' || c == '\u001D' || c == '\u001E' || c == '\u001F'))
                {
                    // Escape other control characters in string values
                    sb.Append("\\u");
                    sb.Append(((int)c).ToString("x4"));
                }
                else
                {
                    sb.Append(c);
                }

                i++;
            }

            return sb.ToString();
        }

        private static async Task<string> RepairWithAgent(string invalid_json, Exception ex, ChatClientAgent agent)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("```schema");
            prompt.AppendLine(GetSchema());
            prompt.AppendLine("```");
            prompt.AppendLine();
            prompt.AppendLine("```error");
            prompt.AppendLine(ex.Message);
            prompt.AppendLine("```");
            prompt.AppendLine();
            prompt.AppendLine("```json");
            prompt.AppendLine(invalid_json);
            prompt.AppendLine("```");

            var workflow = new WorkflowBuilder(agent).
                WithOutputFrom(agent).
                Build();

            // Execute the workflow
            await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, new ChatMessage(ChatRole.User, prompt.ToString()));

            var response = await WorkflowEventListener.ListenToStream(run);

            var retVal = response.GetSingleMessage(agent.Name);

            if (!retVal.IsSuccess)
                return invalid_json;        // could throw an exception, but just let it loop around and try again

            return retVal.Message;
        }

        private static string GetSchema()
        {
            // Force Enums to strings and define global serialization rules
            var options = new JsonSerializerOptions(JsonSerializerOptions.Default)
            {
                Converters = { new JsonStringEnumConverter() },
                WriteIndented = true
            };

            // Configure the Exporter to be less verbose
            var exporterOptions = new JsonSchemaExporterOptions
            {
                // Removes ["type", "null"] for non-nullable reference types (cleaner for LLMs)
                TreatNullObliviousAsNonNullable = true
            };

            var schemaNode = JsonSchemaExporter.GetJsonSchemaAsNode(options, typeof(T), exporterOptions);
            return schemaNode.ToJsonString(options);
        }

        private static ChatClientAgent CreateAgent_JSONCleaner(IChatClient client)
        {
            string prompt =
@"You are an agent responsible for cleaning invalid json files.

There is a function that deserializes json, and if there is an exception, you'll get called.

The user prompt will contain the expected schema, the json, and error message from deserialization.

Please return a repaired json.  Please try to preserve data as well as you can.

Most errors will probably be related to unescaped double quotes insde a string value, or strings instead of numbers.

Your response will be directly deserialized, so the response should just be the repaired json";

            return client.AsAIAgent(
                instructions: prompt,
                //tools: [],
                name: $"RepairJSON");
        }

        #endregion
    }
}
