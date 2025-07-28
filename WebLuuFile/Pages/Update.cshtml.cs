using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Research.SEAL;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using WebLuuFile.Data;
using WebLuuFile.Models;

namespace WebLuuFile.Pages
{
    public class UpdateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public UpdateModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public IFormFile UploadFile { get; set; }

        [BindProperty]
        public string FilePassword { get; set; }

        [BindProperty]
        public string WatermarkText { get; set; }

        public FileModel CurrentFile { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            CurrentFile = await _context.Files.FirstOrDefaultAsync(f => f.Id == id);
            if (CurrentFile == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            var fileRecord = await _context.Files.FirstOrDefaultAsync(f => f.Id == id);
            if (fileRecord == null)
            {
                return NotFound();
            }

            if (UploadFile == null || UploadFile.Length == 0)
            {
                ModelState.AddModelError("UploadFile", "Vui lòng chọn file cần tải lên.");
                return Page();
            }
            if (string.IsNullOrWhiteSpace(FilePassword))
            {
                ModelState.AddModelError("FilePassword", "Vui lòng nhập mật khẩu.");
                return Page();
            }
            if (string.IsNullOrWhiteSpace(WatermarkText))
            {
                ModelState.AddModelError("WatermarkText", "Vui lòng nhập watermark.");
                return Page();
            }

            // Kiểm tra mật khẩu bằng cách thử giải mã file hiện tại
            try
            {
                byte[] existingFileBytes = await System.IO.File.ReadAllBytesAsync(fileRecord.FilePath);
                // Nếu mật khẩu sai, sẽ ném ra CryptographicException
                byte[] testDecryption = DecryptData(existingFileBytes, FilePassword);
            }
            catch (CryptographicException)
            {
                ModelState.AddModelError("FilePassword", "Mật khẩu không chính xác.");
                return Page();
            }

            // Xử lý file mới từ UploadFile
            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await UploadFile.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            bool isImage = UploadFile.ContentType.StartsWith("image");
            if (isImage)
            {
                fileBytes = AddWatermarkToImage(fileBytes, WatermarkText);
            }

            byte[] encryptedData = EncryptData(fileBytes, FilePassword, out byte[] iv);

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var newFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(UploadFile.FileName);
            var newFilePath = Path.Combine(uploadsFolder, newFileName);
            await System.IO.File.WriteAllBytesAsync(newFilePath, encryptedData);

            int newVersionNumber = 1;
            var latestVersion = await _context.FileVersions
                .Where(v => v.FileId == fileRecord.Id)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync();
            if (latestVersion != null)
            {
                newVersionNumber = latestVersion.VersionNumber + 1;
            }

            var fileVersion = new FileVersion
            {
                Id = Guid.NewGuid(),
                FileId = fileRecord.Id,
                VersionNumber = newVersionNumber,
                FilePath = newFilePath,
                UploadDate = DateTime.Now
            };

            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            _context.FileVersions.Add(fileVersion);

            string encryptedWatermark = EncryptWatermarkHomomorphic(WatermarkText);

            fileRecord.FileName = UploadFile.FileName;
            fileRecord.FilePath = newFilePath;
            fileRecord.FileSize = UploadFile.Length;
            fileRecord.FileType = UploadFile.ContentType;
            fileRecord.UploadedBy = user?.UserName;
            fileRecord.UploadDate = DateTime.Now;
            fileRecord.EncryptionKey = Convert.ToBase64String(iv);
            fileRecord.WatermarkText = WatermarkText;
            fileRecord.IsProtected = true;

            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }

        private byte[] EncryptData(byte[] data, string password, out byte[] iv)
        {
            using Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            aes.GenerateIV();
            iv = aes.IV;
            aes.Key = DeriveKey(password, aes.IV);

            using var encryptor = aes.CreateEncryptor();
            byte[] encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);

            byte[] combinedData = new byte[iv.Length + encryptedData.Length];
            Buffer.BlockCopy(iv, 0, combinedData, 0, iv.Length);
            Buffer.BlockCopy(encryptedData, 0, combinedData, iv.Length, encryptedData.Length);

            return combinedData;
        }

        private byte[] AddWatermarkToImage(byte[] imageData, string watermarkText)
        {
            using var memoryStream = new MemoryStream(imageData);
            using var image = Image.FromStream(memoryStream);
            using var graphics = Graphics.FromImage(image);

            var font = new Font("Arial", 24, FontStyle.Bold);
            var brush = new SolidBrush(Color.FromArgb(128, 255, 255, 255));
            var position = new PointF(image.Width * 0.05f, image.Height * 0.9f);

            graphics.DrawString(watermarkText, font, brush, position);

            using var outputMemoryStream = new MemoryStream();
            image.Save(outputMemoryStream, ImageFormat.Png);
            return outputMemoryStream.ToArray();
        }

        private static byte[] DeriveKey(string password, byte[] salt)
        {
            using var keyDerivation = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            return keyDerivation.GetBytes(32);
        }

        private string EncryptWatermarkHomomorphic(string watermark)
        {
            EncryptionParameters parms = new EncryptionParameters(SchemeType.BFV)
            {
                PolyModulusDegree = 4096,
                CoeffModulus = CoeffModulus.BFVDefault(4096),
                PlainModulus = new Modulus(1024)
            };

            SEALContext context = new SEALContext(parms);
            KeyGenerator keygen = new KeyGenerator(context);
            keygen.CreatePublicKey(out PublicKey publicKey);
            Encryptor encryptor = new Encryptor(context, publicKey);

            int watermarkValue = 0;
            foreach (char c in watermark)
            {
                watermarkValue += (int)c;
            }

            Plaintext plain = new Plaintext(watermarkValue.ToString());
            Ciphertext encrypted = new Ciphertext();
            encryptor.Encrypt(plain, encrypted);

            using var ms = new MemoryStream();
            encrypted.Save(ms);
            return Convert.ToBase64String(ms.ToArray());
        }

        private byte[] DecryptData(byte[] encryptedData, string password)
        {
            using Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            byte[] iv = new byte[16];
            byte[] cipherText = new byte[encryptedData.Length - iv.Length];

            Buffer.BlockCopy(encryptedData, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(encryptedData, iv.Length, cipherText, 0, cipherText.Length);

            aes.IV = iv;
            aes.Key = DeriveKey(password, iv);

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
        }
    }
}
