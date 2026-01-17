using System.ComponentModel;

namespace MAFTesters_Core.Tools
{
    public static class FileFinder
    {
        [Description("Finds files that match the description.  The matchingCriteria gets interpreted by an llm that will look at all files in the folder recursively and decide if that filename should be returned.  Returns a list of filenames that matched (full path)")]
        public static string[] FindFiles(
            [Description("Base folder containing files (files may live in subfolders)")]
            string folderName,
            [Description("High level description of the files to find.  Could be extension or type of file (like any image), filename pattern, content inside the file (specific strings, or pictures containing birds), etc.  This tool's llm will decide how to apply matchingCriteria against each file")]
            string matchingCriteria)
        {

            // the llm should look at the matching criteria and decide how to filter files

            // first filter layer is wildcard search: does it need to be *, or can it be limited to specific extentions, like *.json

            // [optional] second filter is a generated python script that has custom logic to filter each file based on filename.  could be any combination of regex, cascade of if statements, etc

            // [optional] third filter is a generated python script that reads through the file, looking for custom matching criteria

            // [optional] fourth filter is to give the filename and file contents to an llm to evaluate (llm may need to take chunks at a time if the file is too large)

            // the resulting list of filenames is returned as an array


            return [];
        }

    }
}
