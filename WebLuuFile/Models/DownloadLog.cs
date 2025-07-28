namespace WebLuuFile.Models
{
    public class DownloadLog
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public Guid FileId { get; set; }
        public DateTime DownloadDate { get; set; }

        public virtual FileModel File { get; set; }
    }

}
