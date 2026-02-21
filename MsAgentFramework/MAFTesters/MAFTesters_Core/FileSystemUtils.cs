using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MAFTesters_Core
{
    public static class FileSystemUtils
    {
        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static string EscapeFilename(string filename, bool containsFolder = false)
        {
            return IsWindows() ?
                EscapeFilename_Windows(filename, containsFolder) :
                EscapeFilename_Linux(filename, containsFolder);
        }
        /// <summary>
        /// This will replace invalid chars with underscores, there are also some reserved words that it adds underscore to
        /// </summary>
        /// <remarks>
        /// https://stackoverflow.com/questions/1976007/what-characters-are-forbidden-in-windows-and-linux-directory-names
        /// 
        /// copied from party people UtilityCore
        /// </remarks>
        /// <param name="containsFolder">Pass in true if filename represents a folder\file (passing true will allow slash)</param>
        public static string EscapeFilename_Windows(string filename, bool containsFolder = false)
        {
            StringBuilder builder = new StringBuilder(filename.Length + 12);

            int index = 0;

            // Allow colon if it's part of the drive letter
            if (containsFolder)
            {
                Match match = Regex.Match(filename, @"^\s*[A-Z]:\\", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    builder.Append(match.Value);
                    index = match.Length;
                }
            }

            // Character substitutions
            for (int cntr = index; cntr < filename.Length; cntr++)
            {
                char c = filename[cntr];

                switch (c)
                {
                    case '\u0000':
                    case '\u0001':
                    case '\u0002':
                    case '\u0003':
                    case '\u0004':
                    case '\u0005':
                    case '\u0006':
                    case '\u0007':
                    case '\u0008':
                    case '\u0009':
                    case '\u000A':
                    case '\u000B':
                    case '\u000C':
                    case '\u000D':
                    case '\u000E':
                    case '\u000F':
                    case '\u0010':
                    case '\u0011':
                    case '\u0012':
                    case '\u0013':
                    case '\u0014':
                    case '\u0015':
                    case '\u0016':
                    case '\u0017':
                    case '\u0018':
                    case '\u0019':
                    case '\u001A':
                    case '\u001B':
                    case '\u001C':
                    case '\u001D':
                    case '\u001E':
                    case '\u001F':

                    case '<':
                    case '>':
                    case ':':
                    case '"':
                    case '/':
                    case '|':
                    case '?':
                    case '*':
                        builder.Append('_');
                        break;

                    case '\\':
                        builder.Append(containsFolder ? c : '_');
                        break;

                    default:
                        builder.Append(c);
                        break;
                }
            }

            string built = builder.ToString();

            if (built == "")
                return "_";

            if (built.EndsWith(" ") || built.EndsWith("."))
                built = built.Substring(0, built.Length - 1) + "_";

            // These are reserved names, in either the folder or file name, but they are fine if following a dot
            // CON, PRN, AUX, NUL, COM0 .. COM9, LPT0 .. LPT9
            builder = new StringBuilder(built.Length + 12);
            index = 0;
            foreach (Match match in Regex.Matches(built, @"(^|\\)\s*(?<bad>CON|PRN|AUX|NUL|COM\d|LPT\d)\s*(\.|\\|$)", RegexOptions.IgnoreCase))
            {
                Group group = match.Groups["bad"];
                if (group.Index > index)
                    builder.Append(built.Substring(index, match.Index - index + 1));

                builder.Append(group.Value);
                builder.Append("_");        // putting an underscore after this keyword is enough to make it acceptable

                index = group.Index + group.Length;
            }

            if (index == 0)
                return built;

            if (index < built.Length - 1)
                builder.Append(built.Substring(index));

            return builder.ToString();
        }
        /// <summary>
        /// Escapes invalid characters for Linux filenames
        /// </summary>
        /// <remarks>
        /// - Replaces path separators ('/') and control characters with underscores
        /// - Handles reserved names/edge cases for safety
        /// </remarks>
        public static string EscapeFilename_Linux(string filename, bool containsFolder = false)
        {
            if (string.IsNullOrEmpty(filename))
                return "_";

            StringBuilder builder = new StringBuilder(filename.Length + 12);

            // Replace invalid characters
            foreach (char c in filename)
            {
                // Replace control characters (ASCII 0-31 and 127) and path separators
                if (c <= 0x1F || c == 0x7F || c == '/')
                    builder.Append('_');
                else
                    builder.Append(c);
            }

            string built = builder.ToString();

            // Handle empty string case
            if (string.IsNullOrEmpty(built))
                return "_";

            // Fix trailing spaces or dots
            if (built.EndsWith(" ") || built.EndsWith("."))
                built = built.Substring(0, built.Length - 1) + "_";

            return built;
        }

        /// <summary>
        /// This will try the filenames passed in.  If that already exists, it will add _digit
        /// WARNING: If there are multiple threads/processes using this same method on the same files, they'll all get the same
        /// "unique" name.  If you want to guarantee unique, have this return a filestream that was opened with createnew
        /// </summary>
        /// <remarks>
        /// copied from party people UtilityCore
        /// 
        /// This will make sure the same base name is used for all extensions passed in
        /// 
        /// example: "hello", "txt", "md"
        /// if hello.txt doesn't exist, but hello.md does, this will tack on a number and return hello_1.txt and hello_1.md
        /// </remarks>
        public static UniqueFilenameResponse GetUniqueFilename(string folder, string filename, params string[] extensions)
        {
            // Normalize folder: use it as-is if non-empty, default to current directory
            folder = !string.IsNullOrEmpty(folder) ?
                folder :
                ".";

            // Normalize the extensions by ensuring they start with a period
            string[] normalizedExtensions = GetNormalizedExtensions(extensions);

            // If extensions are provided and filename ends in one of them, set baseName equal to the portion in front
            // of that extension
            string baseName = GetBaseName(filename, normalizedExtensions);

            // Uniqueness: increment counter until a unique file is found
            int counter = 0;
            string uniqueName;
            string path_uniqueName;

            while (true)
            {
                counter++;

                uniqueName = counter == 1 ?
                    baseName :
                    $"{baseName}_{counter}";

                path_uniqueName = Path.Combine(folder, uniqueName);

                if (!normalizedExtensions.Any(o => File.Exists(path_uniqueName + o)))
                    break;
            }

            return new UniqueFilenameResponse
            {
                full_path = path_uniqueName,
                name_nofolder = uniqueName,
            };
        }

        #region Private Methods

        private static string[] GetNormalizedExtensions(string[] extensions)
        {
            if (extensions == null || extensions.Length == 0)
                return [];

            var retVal = new List<string>();

            foreach (string extension in extensions)
            {
                if (string.IsNullOrWhiteSpace(extension))
                    continue;

                retVal.Add(extension.StartsWith(".") ? extension : "." + extension);
            }

            return retVal.ToArray();
        }

        private static string GetBaseName(string filename, string[] extensions)
        {
            if (extensions.Length == 0)
                return filename;

            foreach (string extension in extensions)
            {
                if (filename.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                    return filename.Substring(0, filename.Length - extension.Length);
            }

            return filename;
        }

        #endregion
    }

    #region record: UniqueFilenameResponse

    public record UniqueFilenameResponse
    {
        public string name_nofolder { get; init; }
        public string full_path { get; init; }

        /// <summary>
        /// This function assumes the instance is the base name without extenstion and return an instance with
        /// extension applied.  it makes sure that there is a period before extention ("txt" passed in turns
        /// into ".txt"
        /// </summary>
        public UniqueFilenameResponse GetFinalName(string extension)
        {
            string final_extension = extension.StartsWith(".") ?
                extension :
                "." + extension;

            return new UniqueFilenameResponse
            {
                name_nofolder = name_nofolder + final_extension,
                full_path = full_path + final_extension,
            };
        }
    }

    #endregion
}
