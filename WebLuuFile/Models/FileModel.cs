namespace WebLuuFile.Models
{
    public class FileModel
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string FileType { get; set; }
        public string UploadedBy { get; set; }
        public DateTime UploadDate { get; set; }
        public string EncryptionKey { get; set; }
        public string WatermarkText { get; set; }
        public bool IsProtected { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public ICollection<FileVersion> FileVersions { get; set; }

        public virtual ICollection<DownloadLog> DownloadLogs { get; set; }
        public string StoredFileName { get; set; }
    }

}
