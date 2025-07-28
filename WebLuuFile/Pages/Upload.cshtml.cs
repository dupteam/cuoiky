using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebLuuFile.Data;
using WebLuuFile.Models;
using Microsoft.Research.SEAL;
using System.Data;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace WebLuuFile.Pages
{
    public class UploadModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public UploadModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
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

        public async Task<IActionResult> OnPostAsync()
        {
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
            var filePath = Path.Combine(uploadsFolder, Guid.NewGuid() + "_" + Path.GetFileName(UploadFile.FileName));
            await System.IO.File.WriteAllBytesAsync(filePath, encryptedData);


            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            if (string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError(string.Empty, "Người dùng chưa đăng nhập.");
                return Page();
            }

            string encryptedWatermark = EncryptWatermarkHomomorphic(WatermarkText);

            _context.Files.Add(new FileModel
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FileName = UploadFile.FileName,
                FilePath = filePath,
                FileSize = UploadFile.Length,
                FileType = UploadFile.ContentType,
                UploadedBy = user.UserName,
                UploadDate = DateTime.Now,
                EncryptionKey = Convert.ToBase64String(iv),
                WatermarkText = WatermarkText,
                IsProtected = true
            });

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

            Microsoft.Research.SEAL.PublicKey publicKey;
            keygen.CreatePublicKey(out publicKey);

            Encryptor encryptor = new Encryptor(context, publicKey);

            int watermarkValue = 0;
            foreach (char c in watermark)
            {
                watermarkValue += (int)c;
            }

            Plaintext plain = new Plaintext(watermarkValue.ToString());
            Ciphertext encrypted = new Ciphertext();
            encryptor.Encrypt(plain, encrypted);

            using (var ms = new MemoryStream())
            {
                encrypted.Save(ms);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

    }
}
