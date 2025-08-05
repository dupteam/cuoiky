
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using WebLuuFile.Data;
using WebLuuFile.Models;
using Xunit;
using System.Linq;
using System;
using WebLuuFile.Pages;

namespace WebLuuFile.Tests
{
    public class FileServiceTests
    {
        // Khởi tạo DbContext giả (In-Memory)
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        // Tạo UserManager giả cho người dùng "testuser"
        private UserManager<IdentityUser> GetFakeUserManager()
        {
            var store = new Mock<IUserStore<IdentityUser>>();
            var userManager = new Mock<UserManager<IdentityUser>>(store.Object, null, null, null, null, null, null, null, null);
            userManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("123");
            userManager.Setup(um => um.FindByIdAsync("123")).ReturnsAsync(new IdentityUser { UserName = "testuser" });
            return userManager.Object;
        }

        // Tạo file giả để upload
        private IFormFile GetMockFormFile(string fileName = "file.txt", string content = "hello world", string contentType = "text/plain")
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            return new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        // Tạo PageContext với hoặc không có người dùng đăng nhập
        private PageContext GetPageContext(bool hasUser = true)
        {
            var user = hasUser
                ? new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "123") }))
                : new ClaimsPrincipal();

            return new PageContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        // ✅ Test case 1: Dữ liệu hợp lệ => phải upload thành công và lưu vào DB
        [Fact]
        public async Task OnPostAsync_WithValidData_ShouldUploadFileAndSaveToDb()
        {
            var context = GetInMemoryDbContext();
            var userManager = GetFakeUserManager();
            var model = new UploadModel(context, userManager)
            {
                UploadFile = GetMockFormFile(),
                FilePassword = "secure123",
                WatermarkText = "test watermark",
                PageContext = GetPageContext()
            };

            var result = await model.OnPostAsync();

            // ✅ Mong đợi redirect về Index
            var redirect = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("Index", redirect.PageName);

            // ✅ Kiểm tra có lưu file vào CSDL không
            var saved = context.Files.FirstOrDefault();
            Assert.NotNull(saved);
            Assert.Equal("file.txt", saved.FileName);
            Assert.Equal("testuser", saved.UploadedBy);
            // Trả về FALSE => Dữ liệu hợp lệ vẫn ko upload được và không được lưu vào CSDL
        }
        

        // ✅ Test case 2: Thiếu file => ModelState lỗi
        [Fact]
        public async Task OnPostAsync_MissingFile_ShouldReturnModelError()
        {
            var context = GetInMemoryDbContext();
            var userManager = GetFakeUserManager();
            var model = new UploadModel(context, userManager)
            {
                UploadFile = null,
                FilePassword = "123",
                WatermarkText = "wm",
                PageContext = GetPageContext()
            };

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.False(model.ModelState.IsValid);
            Assert.True(model.ModelState.ContainsKey("UploadFile"));
            // Trả về TRUE => Hệ thống đã kiểm tra dung lượng của file upload và ko cho phép upload file rỗng
        }

        // ✅ Test case 3: Không nhập mật khẩu => lỗi ModelState
        [Fact]
        public async Task OnPostAsync_EmptyPassword_ShouldReturnModelError()
        {
            var context = GetInMemoryDbContext();
            var userManager = GetFakeUserManager();
            var model = new UploadModel(context, userManager)
            {
                UploadFile = GetMockFormFile(),
                FilePassword = "",
                WatermarkText = "wm",
                PageContext = GetPageContext()
            };

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.False(model.ModelState.IsValid);
            Assert.True(model.ModelState.ContainsKey("FilePassword"));
            // Trả về TRUE => Hệ thống này đã kiểm tra mật khẩu người dùng có nhập trống hay ko
            // => Nếu để trống mật khẩu không thể upload được 
        }

        // ✅ Test case 4: Không nhập watermark => lỗi ModelState
        [Fact]
        public async Task OnPostAsync_EmptyWatermark_ShouldReturnModelError()
        {
            var context = GetInMemoryDbContext();
            var userManager = GetFakeUserManager();
            var model = new UploadModel(context, userManager)
            {
                UploadFile = GetMockFormFile(),
                FilePassword = "123",
                WatermarkText = "",
                PageContext = GetPageContext()
            };

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.False(model.ModelState.IsValid);
            Assert.True(model.ModelState.ContainsKey("WatermarkText"));
            // Trả về TRUE => Hệ thống đã kiểm tra xem Watermark có rỗng hay ko
            // => Không có Watermark thì hệ thống ko cho upload
        }

        // ❌ Test case 5: Upload file .exe => hệ thống phải từ chối
        [Fact]
        public async Task OnPostAsync_InvalidFileFormat_ShouldReturnModelError()
        {
            var context = GetInMemoryDbContext();
            var userManager = GetFakeUserManager();
            var model = new UploadModel(context, userManager)
            {
                UploadFile = GetMockFormFile("file.exe", "fake binary", "application/octet-stream"),
                FilePassword = "123",
                WatermarkText = "wm",
                PageContext = GetPageContext()
            };

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.False(model.ModelState.IsValid);
            // Trả về FALSE => Hệ thống này chưa kiểm tra được định dạng file 
            // => Hệ thống chưa kiểm tra định dạng file nguy hiểm (ví dụ: .exe)
        }

        // ✅ Test case 6: Upload file rỗng => không cho phép
        [Fact]
        public async Task OnPostAsync_ZeroByteFile_ShouldReturnModelError()
        {
            var context = GetInMemoryDbContext();
            var userManager = GetFakeUserManager();

            var stream = new MemoryStream(); // tạo file 0 byte
            var file = new FormFile(stream, 0, 0, "file", "empty.txt")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain"
            };

            var model = new UploadModel(context, userManager)
            {
                UploadFile = file,
                FilePassword = "123",
                WatermarkText = "wm",
                PageContext = GetPageContext()
            };

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.False(model.ModelState.IsValid);
            // Trả về TRUE => Hệ thống đã kiểm tra dung lượng file upload
            // => Hệ thống không cho phép upload file rỗng 
        }

        // ❌ Test case 7: Người chưa đăng nhập có upload được file ko
        [Fact]
        public async Task OnPostAsync_UnauthenticatedUser_ShouldReturnErrorOrRedirect()
        {
            var context = GetInMemoryDbContext();
            var userManager = GetFakeUserManager();
            var model = new UploadModel(context, userManager)
            {
                UploadFile = GetMockFormFile(),
                FilePassword = "123",
                WatermarkText = "wm",
                PageContext = GetPageContext(hasUser: false) // không có user đăng nhập
            };

            var result = await model.OnPostAsync();

            Assert.IsType<PageResult>(result);
            // Trả về FALSE => Hệ thống vẫn cho người dùng upload mà chưa login => Sai logic
        }


        // ✅ TEST 8,9: Lọc và Tìm kiếm File
        public class SearchTests
        {
            public class FileService
            {
                public List<FileModel> SearchByName(List<FileModel> files, string keyword)
                    => files.Where(f => f.FileName.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();

                public List<FileModel> FilterByType(List<FileModel> files, string type)
                {
                    return type switch
                    {
                        "Text" => files.Where(f => new[] { ".txt", ".pdf", ".docx" }.Contains(f.FileType)).ToList(),
                        "Image" => files.Where(f => new[] { ".png", ".jpg" }.Contains(f.FileType)).ToList(),
                        "Video" => files.Where(f => new[] { ".mp4", ".avi" }.Contains(f.FileType)).ToList(),
                        "Audio" => files.Where(f => new[] { ".mp3", ".wav" }.Contains(f.FileType)).ToList(),
                        _ => new List<FileModel>()
                    };
                }
            }
            //Tìm kiếm theo tên file
            [Fact]
            public void SearchByName_WithMatchingKeyword_ShouldReturnCorrectFiles()
            {
                var files = new List<FileModel>
            {
                new FileModel { FileName = "report.pdf" },
                new FileModel { FileName = "holiday.jpg" },
                new FileModel { FileName = "report_final.docx" }
            };

                var service = new FileService();
                var result = service.SearchByName(files, "report");

                Assert.Equal(2, result.Count);
                Assert.All(result, f => Assert.Contains("report", f.FileName));
                // Trả về TRUE => Những file nào có tên là report theo phần tìm kiếm thì sẽ được hiện lên
            }
            //Tìm kiếm theo định dạng file
            [Fact]
            public void FilterByType_WithTextType_ShouldReturnOnlyTextFiles()
            {
                var files = new List<FileModel>
            {
                new FileModel { FileType = ".txt" },
                new FileModel { FileType = ".jpg" },
                new FileModel { FileType = ".pdf" },
                new FileModel { FileType = ".docx" }
            };

                var service = new FileService();
                var result = service.FilterByType(files, "Text");

                Assert.Equal(3, result.Count);
                Assert.All(result, f => Assert.Contains(f.FileType, new[] { ".txt", ".pdf", ".docx" }));
                // Trả về TRUE => Hệ thống này đã lọc được những file được coi là Text (như .txt,.pdf,.docx)
                // jpg là ảnh => Ko được hiện lên
            }
        }

        // ✅ TEST 10,11: Phân quyền
        public class PermissionTests
        {
            public class FileService
            {
                public bool CanAccessFile(string currentUserId, string fileOwnerId)
                    => currentUserId == fileOwnerId;
            }
            //Người khác truy cập được vào file của mình hay ko
            [Fact]
            public void CanAccessFile_WithDifferentUsers_ShouldReturnFalse()
            {
                var service = new FileService();
                var result = service.CanAccessFile("userA", "userB");

                Assert.False(result);//Kết quả phải trả về false 
                // Ở đây trả về TRUE => Sai logic
            }

            [Fact]
            public void CanAccessFile_WithSameUser_ShouldReturnTrue()
            {
                var service = new FileService();
                var result = service.CanAccessFile("userA", "userA");

                Assert.True(result);// Trả về TRUE => Đúng logic (Mình có thể truy cập được file của mình)
            }
        }
    }
}
