using Minio;
using Minio.DataModel.Args;

namespace FoodExpress.Restaurant.API.Services;

public class MinioFileStorageService : IFileStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private readonly string _publicUrl;
    private readonly ILogger<MinioFileStorageService> _logger;

    public MinioFileStorageService(IConfiguration config, ILogger<MinioFileStorageService> logger)
    {
        _bucketName = config["MinIO:BucketName"]!;
        _publicUrl = config["MinIO:PublicUrl"]!;
        _logger = logger;

        _minioClient = new MinioClient()
            .WithEndpoint(config["MinIO:Endpoint"])
            .WithCredentials(config["MinIO:AccessKey"], config["MinIO:SecretKey"])
            .WithSSL(bool.Parse(config["MinIO:UseSSL"]!))
            .Build();
    }

    private static readonly string[] AllowedExtensions =
        [".jpg", ".jpeg", ".png", ".webp", ".gif", ".avif"];

    private static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp", "image/gif", "image/avif"];

    public async Task<string> UploadFileAsync(IFormFile file, string folder = "")
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Fichier vide");

        // Vérifier si le bucket existe, sinon le créer
        var bucketExistsArgs = new BucketExistsArgs().WithBucket(_bucketName);
        var exists = await _minioClient.BucketExistsAsync(bucketExistsArgs);
        if (!exists)
        {
            var makeBucketArgs = new MakeBucketArgs().WithBucket(_bucketName);
            await _minioClient.MakeBucketAsync(makeBucketArgs);
        }

        // Sécurité : seulement des images, jamais des fichiers exécutables/html/etc.
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException(
                $"Extension interdite : {extension}. Formats autorisés : {string.Join(", ", AllowedExtensions)}");

        var contentType = file.ContentType.ToLowerInvariant();
        if (!AllowedContentTypes.Contains(contentType))
            throw new ArgumentException($"Type de contenu interdite : {contentType}");

        // Nom unique pour le fichier
        var fileName = string.IsNullOrEmpty(folder)
            ? $"{Guid.NewGuid()}{extension}"
            : $"{folder}/{Guid.NewGuid()}{extension}";

        // Upload
        using var stream = file.OpenReadStream();
        var putObjectArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(fileName)
            .WithStreamData(stream)
            .WithObjectSize(file.Length)
            .WithContentType(file.ContentType);

        await _minioClient.PutObjectAsync(putObjectArgs);

        _logger.LogInformation("File uploaded: {FileName}", fileName);

        // Retourne l'URL publique
        return $"{_publicUrl}/{_bucketName}/{fileName}";
    }

    public async Task DeleteFileAsync(string fileUrl)
    {
        try
        {
            var fileName = fileUrl.Replace($"{_publicUrl}/{_bucketName}/", "");
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName);

            await _minioClient.RemoveObjectAsync(removeObjectArgs);
            _logger.LogInformation("File deleted: {FileName}", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file: {Url}", fileUrl);
        }
    }
}