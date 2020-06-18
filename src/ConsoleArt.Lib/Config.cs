using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ConsoleArt.Lib
{
    public class Config
    {
        public string Input { get; set; }
        public string Output { get; set; }
        public string SearchPattern { get; set; }
        public string ResultFileName { get; set; }
        public List<string> FileTypes { get; set; }

        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();
            if (!Directory.Exists(Input))
            {
                errors.Add($"Input-mappen '{Input}' finnes ikke");
            }

            if (!Directory.Exists(Input))
            {
                errors.Add($"Output-mappen '{Output}' finnes ikke");
            }

            if (string.IsNullOrWhiteSpace(SearchPattern))
            {
                errors.Add($"{nameof(SearchPattern)} '{SearchPattern}' mangler");
            }

            if (string.IsNullOrWhiteSpace(ResultFileName))
            {
                errors.Add($"{nameof(ResultFileName)} '{ResultFileName}' mangler");
            }

            if (FileTypes is null || FileTypes.Count < 1)
            {
                errors.Add("Vennligst oppgi minst en filtype");
            }

            return !errors.Any();
        }

        public override string ToString()
        {
            return $"{nameof(Input)}: '{Input}', {nameof(Output)}: '{Output}', {nameof(SearchPattern)}: '{SearchPattern}', {nameof(ResultFileName)}: '{ResultFileName}', {nameof(FileTypes)}: '{string.Join("', '", FileTypes)}'";
        }
    }
}
