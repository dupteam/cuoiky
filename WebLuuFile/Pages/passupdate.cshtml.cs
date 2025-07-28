using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using WebLuuFile.Data;
using WebLuuFile.Models;

namespace WebLuuFile.Pages
{
    public class passupdateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public passupdateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string FilePassword { get; set; }

        public FileModel FileRecord { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            FileRecord = await _context.Files.FirstOrDefaultAsync(f => f.Id == id);
            if (FileRecord == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == id);
            if (file == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(FilePassword))
            {
                ModelState.AddModelError("FilePassword", "Vui lòng nhập mật khẩu.");
                return Page();
            }

            try
            {
           
                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(file.FilePath);
                byte[] testDecryption = DecryptData(fileBytes, FilePassword);
            }
            catch (CryptographicException)
            {
                ModelState.AddModelError("FilePassword", "Mật khẩu không chính xác.");
                return Page();
            }

      
            TempData["FilePassword"] = FilePassword;
            return RedirectToPage("Update", new { id = id });
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
            using var keyDerivation = new Rfc2898DeriveBytes(password, salt, 10000, System.Security.Cryptography.HashAlgorithmName.SHA256);
            return keyDerivation.GetBytes(32);
        }
    }
}
