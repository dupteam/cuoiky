using WebLuuFile; // ✅ DÒNG NÀY CỰC QUAN TRỌNG
using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Threading.Tasks;


public class FileServiceTests
{
    [Fact]
    public async Task UploadAsync_ValidFile_ReturnsFileName()
    {
        // Arrange
        var content = "Test file content";
        var fileName = "sample.txt";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(stream.Length);

        var service = new FileService();

        // Act
        var result = await service.UploadAsync(fileMock.Object);

        // Assert
        Assert.Equal(fileName, result);
    }
}
