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

        // Nom unique pour le fichier
        var extension = Path.GetExtension(file.FileName);
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