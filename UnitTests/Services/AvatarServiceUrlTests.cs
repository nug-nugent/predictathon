using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Predictathon.Application.Models;
using Predictathon.Application.Services;
using Predictathon.UnitTests.TestDoubles;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Predictathon.UnitTests.Services;

/// <summary>
/// Covers the URLs <see cref="AvatarService"/> hands out. An avatar's filename never changes, so
/// these URLs carry a version stamp taken from the file on disk - without it, a browser keeps
/// showing the picture it already cached after a user uploads a new one.
/// </summary>
public class AvatarServiceUrlTests : IDisposable
{
    private const string BaseUrl = "https://api.test";

    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), $"predictathon-avatar-tests-{Guid.NewGuid()}");
    private readonly Guid _userId = Guid.NewGuid();

    public AvatarServiceUrlTests()
    {
        Directory.CreateDirectory(_storagePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_storagePath))
        {
            Directory.Delete(_storagePath, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void GetAvatarUrl_NoImageUploaded_ReturnsNull()
    {
        WriteAvatarFile(small: true);

        var url = CreateService().GetAvatarUrl(_userId, imageUploaded: false);

        url.Should().BeNull();
    }

    [Fact]
    public void GetAvatarUrl_ImageUploadedButFileMissing_ReturnsNull()
    {
        var url = CreateService().GetAvatarUrl(_userId, imageUploaded: true);

        url.Should().BeNull();
    }

    [Fact]
    public void GetAvatarUrl_NoPublicBaseUrlConfigured_ReturnsNull()
    {
        WriteAvatarFile(small: true);

        var url = CreateService(baseUrl: null).GetAvatarUrl(_userId, imageUploaded: true);

        url.Should().BeNull();
    }

    [Fact]
    public void GetAvatarUrl_ThumbnailExists_ReturnsVersionStampedUrl()
    {
        WriteAvatarFile(small: true);

        var url = CreateService().GetAvatarUrl(_userId, imageUploaded: true);

        url.Should().StartWith($"{BaseUrl}/uploads/avatars/{_userId}_sm.jpg?v=");
    }

    [Fact]
    public void GetAvatarUrl_Large_ReturnsFullSizeFilesUrl()
    {
        WriteAvatarFile(small: true);
        WriteAvatarFile(small: false);

        var url = CreateService().GetAvatarUrl(_userId, imageUploaded: true, large: true);

        url.Should().StartWith($"{BaseUrl}/uploads/avatars/{_userId}.jpg?v=");
    }

    [Fact]
    public void GetAvatarUrl_LargeButOnlyThumbnailExists_FallsBackToThumbnail()
    {
        WriteAvatarFile(small: true);

        var url = CreateService().GetAvatarUrl(_userId, imageUploaded: true, large: true);

        url.Should().StartWith($"{BaseUrl}/uploads/avatars/{_userId}_sm.jpg?v=");
    }

    [Fact]
    public void GetAvatarUrl_FileReplaced_ReturnsADifferentUrl()
    {
        WriteAvatarFile(small: true, lastWriteUtc: new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var before = CreateService().GetAvatarUrl(_userId, imageUploaded: true);

        WriteAvatarFile(small: true, lastWriteUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var after = CreateService().GetAvatarUrl(_userId, imageUploaded: true);

        after.Should().NotBe(before);
    }

    [Fact]
    public async Task GetAvatarUrl_AfterUploadingOverAnOlderAvatar_ReturnsTheNewVersion()
    {
        // The version lookup is memoised per instance, so the URL handed back by the upload endpoint
        // would be the pre-upload one if uploading didn't discard that memo.
        WriteAvatarFile(small: true, lastWriteUtc: new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var service = CreateService();
        var before = service.GetAvatarUrl(_userId, imageUploaded: true);

        using var image = new Image<Rgba32>(200, 160);
        using var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;

        var result = await service.UploadAvatarAsync(_userId, stream, new AvatarCropRect { X = 0, Y = 0, Width = 200, Height = 160 });

        result.IsSuccess.Should().BeTrue();
        service.GetAvatarUrl(_userId, imageUploaded: true).Should().NotBe(before);
    }

    /// <summary>
    /// Creates the service under test against this test's temporary avatar folder.
    /// </summary>
    /// <param name="baseUrl">The configured public base URL, or null to leave it unset.</param>
    private AvatarService CreateService(string? baseUrl = BaseUrl)
    {
        var settings = new Dictionary<string, string?> { ["Avatars:StoragePath"] = _storagePath };
        if (baseUrl is not null)
        {
            settings["Avatars:PublicBaseUrl"] = baseUrl;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new AvatarService(MockUserManager.Create().Object, configuration);
    }

    /// <summary>
    /// Writes a placeholder avatar file - only its existence and timestamp matter to URL building.
    /// </summary>
    /// <param name="small">True for the thumbnail file, false for the full-size one.</param>
    /// <param name="lastWriteUtc">The last-write time to stamp on the file, or null to leave it as now.</param>
    private void WriteAvatarFile(bool small, DateTime? lastWriteUtc = null)
    {
        var path = Path.Combine(_storagePath, small ? $"{_userId}_sm.jpg" : $"{_userId}.jpg");
        File.WriteAllBytes(path, [0xFF, 0xD8, 0xFF]);

        if (lastWriteUtc is not null)
        {
            File.SetLastWriteTimeUtc(path, lastWriteUtc.Value);
        }
    }
}
