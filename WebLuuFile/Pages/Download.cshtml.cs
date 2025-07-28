using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using WebLuuFile.Data;
using WebLuuFile.Models;

namespace WebLuuFile.Pages
{
    public class DownloadModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DownloadModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public string FilePassword { get; set; }

        public FileModel FileRecord { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (file.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            FileRecord = file;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            if (file.UserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            if (!System.IO.File.Exists(file.FilePath))
            {
                ModelState.AddModelError(string.Empty, "File không tồn tại.");
                return Page();
            }

            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(file.FilePath);
            byte[] decryptedData;
            try
            {
                decryptedData = DecryptData(fileBytes, FilePassword);
            }
            catch (CryptographicException)
            {
                ModelState.AddModelError("FilePassword", "Mật khẩu không chính xác.");
                FileRecord = file;
                return Page();
            }

            var downloadLog = new DownloadLog
            {
                UserId = userId,
                FileId = file.Id,
                DownloadDate = DateTime.UtcNow
            };

            _context.DownloadLogs.Add(downloadLog);
            await _context.SaveChangesAsync();

            return File(decryptedData, file.FileType, file.FileName);
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

        private static byte[] DeriveKey(string password, byte[] salt)
        {
            using var keyDerivation = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            return keyDerivation.GetBytes(32);
        }
    }
}
