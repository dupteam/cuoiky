using System.ComponentModel.DataAnnotations.Schema;

namespace WebLuuFile.Models
{
    public class FileVersion
    {
        public Guid Id { get; set; }

        [ForeignKey("FileModel")]
        public Guid FileId { get; set; }
        public int VersionNumber { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadDate { get; set; }

        public virtual FileModel FileModel { get; set; }
    }

}
