using GalleryApi.Application.DTOs;
using GalleryApi.Application.UseCases.Photos;
using GalleryApi.Domain.Entities;
using GalleryApi.Domain.Interfaces;
using Moq;
using Xunit;

namespace GalleryApi.Tests.UseCases;

public class UploadPhotoUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_PalauttaaPhotoDto_KunLatausOnnistuu()
    {
        var albumId = Guid.NewGuid();
        using var stream = new MemoryStream([1, 2, 3]);
        var request = new UploadPhotoRequest(
            albumId,
            "Testikuva",
            stream,
            "photo.jpg",
            "image/jpeg",
            stream.Length);

        var albumRepository = new Mock<IAlbumRepository>();
        albumRepository
            .Setup(repository => repository.GetByIdAsync(albumId))
            .ReturnsAsync(new Album { Id = albumId, Name = "Albumi" });

        var photoRepository = new Mock<IPhotoRepository>();
        photoRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<Photo>()))
            .ReturnsAsync((Photo photo) => photo);

        var storageService = new Mock<IStorageService>();
        storageService
            .Setup(service => service.UploadAsync(stream, "photo.jpg", "image/jpeg", albumId))
            .ReturnsAsync($"/uploads/{albumId}/photo.jpg");

        var useCase = new UploadPhotoUseCase(photoRepository.Object, albumRepository.Object, storageService.Object);

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(albumId, result.Value!.AlbumId);
        Assert.Equal("Testikuva", result.Value.Title);
        Assert.Equal($"/uploads/{albumId}/photo.jpg", result.Value.ImageUrl);
        photoRepository.Verify(repository => repository.CreateAsync(It.IsAny<Photo>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_PalauttaaVirheen_KunAlbumiaEiLoydy()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var request = new UploadPhotoRequest(
            Guid.NewGuid(),
            "Testikuva",
            stream,
            "photo.jpg",
            "image/jpeg",
            stream.Length);

        var albumRepository = new Mock<IAlbumRepository>();
        albumRepository
            .Setup(repository => repository.GetByIdAsync(request.AlbumId))
            .ReturnsAsync((Album?)null);

        var photoRepository = new Mock<IPhotoRepository>();
        var storageService = new Mock<IStorageService>();
        var useCase = new UploadPhotoUseCase(photoRepository.Object, albumRepository.Object, storageService.Object);

        var result = await useCase.ExecuteAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("ei löydy", result.Error);
        storageService.Verify(
            service => service.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_PalauttaaVirheen_KunTiedostotyyppiEiOleSallittu()
    {
        var albumId = Guid.NewGuid();
        using var stream = new MemoryStream([1, 2, 3]);
        var request = new UploadPhotoRequest(
            albumId,
            "Testikuva",
            stream,
            "photo.pdf",
            "application/pdf",
            stream.Length);

        var albumRepository = new Mock<IAlbumRepository>();
        albumRepository
            .Setup(repository => repository.GetByIdAsync(albumId))
            .ReturnsAsync(new Album { Id = albumId, Name = "Albumi" });

        var photoRepository = new Mock<IPhotoRepository>();
        var storageService = new Mock<IStorageService>();
        var useCase = new UploadPhotoUseCase(photoRepository.Object, albumRepository.Object, storageService.Object);

        var result = await useCase.ExecuteAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("ei ole sallittu", result.Error);
        storageService.Verify(
            service => service.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_PalauttaaVirheen_KunTiedostoOnLiianSuuri()
    {
        var albumId = Guid.NewGuid();
        using var stream = new MemoryStream([1, 2, 3]);
        var request = new UploadPhotoRequest(
            albumId,
            "Testikuva",
            stream,
            "photo.jpg",
            "image/jpeg",
            10 * 1024 * 1024 + 1);

        var albumRepository = new Mock<IAlbumRepository>();
        albumRepository
            .Setup(repository => repository.GetByIdAsync(albumId))
            .ReturnsAsync(new Album { Id = albumId, Name = "Albumi" });

        var photoRepository = new Mock<IPhotoRepository>();
        var storageService = new Mock<IStorageService>();
        var useCase = new UploadPhotoUseCase(photoRepository.Object, albumRepository.Object, storageService.Object);

        var result = await useCase.ExecuteAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("liian suuri", result.Error);
        storageService.Verify(
            service => service.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()),
            Times.Never);
    }
}

public class DeletePhotoUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_PoistaaKuvanJaPalauttaaOnnistumisen()
    {
        var photoId = Guid.NewGuid();
        var albumId = Guid.NewGuid();
        var photo = new Photo
        {
            Id = photoId,
            AlbumId = albumId,
            FileName = "photo.jpg",
            ImageUrl = $"/uploads/{albumId}/photo.jpg"
        };

        var photoRepository = new Mock<IPhotoRepository>();
        photoRepository
            .Setup(repository => repository.GetByIdAsync(photoId))
            .ReturnsAsync(photo);

        var storageService = new Mock<IStorageService>();
        var useCase = new DeletePhotoUseCase(photoRepository.Object, storageService.Object);

        var result = await useCase.ExecuteAsync(photoId);

        Assert.True(result.IsSuccess);
        storageService.Verify(service => service.DeleteAsync("photo.jpg", albumId), Times.Once);
        photoRepository.Verify(repository => repository.DeleteAsync(photoId), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_PalauttaaVirheen_KunKuvaaEiLoydy()
    {
        var photoRepository = new Mock<IPhotoRepository>();
        photoRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Photo?)null);

        var storageService = new Mock<IStorageService>();
        var useCase = new DeletePhotoUseCase(photoRepository.Object, storageService.Object);

        var result = await useCase.ExecuteAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Contains("ei löydy", result.Error);
        storageService.Verify(service => service.DeleteAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
    }
}