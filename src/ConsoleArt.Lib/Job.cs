using System.Collections.Generic;
using System.Text;

namespace ConsoleArt.Lib
{
    public class Job
    {
        public string Root { get; set; }
        public List<Artist> Artists { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("Rotkatalog: ").AppendLine(Root);

            foreach (var artist in Artists)
            {
                sb.Append("Artist: ").AppendLine(artist.Name);
                foreach (var album in artist.Albums)
                {
                    sb.Append("  Album: ").AppendLine(album.Name);
                    foreach (var coverArt in album.CoverArt)
                    {
                        sb.Append("    ").AppendLine(coverArt);
                    }
                }
            }

            return sb.ToString();
        }
    }
}
