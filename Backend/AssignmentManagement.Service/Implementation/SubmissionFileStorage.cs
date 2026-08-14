using AssignmentManagement.Core.Exceptions;
using AssignmentManagement.Service.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AssignmentManagement.Service.Implementation;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string? RootPath { get; set; }
}

public sealed class SubmissionFileStorage : ISubmissionFileStorage
{
    public const long MaximumFileSize = 10 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string> AllowedTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".pdf"] = "application/pdf"
        };

    private readonly string _rootPath;

    public SubmissionFileStorage(
        IOptions<FileStorageOptions> options,
        IHostEnvironment environment)
    {
        var configuredPath = options.Value.RootPath;
        _rootPath = Path.GetFullPath(string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "SubmissionFiles")
            : configuredPath);
    }

    public async Task<StoredSubmissionFile> SaveAsync(
        SubmissionFileUpload file,
        long institutionId,
        long assignmentId,
        CancellationToken cancellationToken = default)
    {
        ValidateFile(file);

        var originalFileName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var contentType = AllowedTypes[extension];
        var relativePath = Path.Combine(
            institutionId.ToString(),
            assignmentId.ToString(),
            $"{Guid.NewGuid():N}{extension}");
        var fullPath = ResolveSafePath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        try
        {
            await using var destination = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous);

            var signature = new byte[8];
            var signatureLength = 0;
            while (signatureLength < signature.Length)
            {
                var bytesRead = await file.Content.ReadAsync(
                    signature.AsMemory(signatureLength, signature.Length - signatureLength),
                    cancellationToken);
                if (bytesRead == 0)
                    break;
                signatureLength += bytesRead;
            }
            if (!HasValidSignature(extension, signature.AsSpan(0, signatureLength)))
                throw new AppException(400, "The uploaded file content does not match its extension.");

            await destination.WriteAsync(signature.AsMemory(0, signatureLength), cancellationToken);
            await file.Content.CopyToAsync(destination, cancellationToken);
        }
        catch
        {
            if (File.Exists(fullPath))
                File.Delete(fullPath);
            throw;
        }

        return new StoredSubmissionFile(
            originalFileName,
            relativePath.Replace('\\', '/'),
            contentType,
            file.Length);
    }

    public Task<Stream> OpenReadAsync(
        string storedFilePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = ResolveSafePath(storedFilePath);
        if (!File.Exists(fullPath))
            throw new AppException(404, "The submitted file was not found on the server.");

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(
        string? storedFilePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(storedFilePath))
            return Task.CompletedTask;

        var fullPath = ResolveSafePath(storedFilePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    private static void ValidateFile(SubmissionFileUpload file)
    {
        if (file.Length <= 0)
            throw new AppException(400, "The uploaded file is empty.");
        if (file.Length > MaximumFileSize)
            throw new AppException(400, "The file cannot be larger than 10 MB.");

        var extension = Path.GetExtension(Path.GetFileName(file.FileName));
        if (string.IsNullOrWhiteSpace(extension) || !AllowedTypes.ContainsKey(extension))
            throw new AppException(400, "Only JPG, JPEG, PNG, and PDF files are allowed.");
        if (Path.GetFileName(file.FileName).Length > 255)
            throw new AppException(400, "The file name cannot be longer than 255 characters.");
    }

    private static bool HasValidSignature(string extension, ReadOnlySpan<byte> bytes) =>
        extension switch
        {
            ".jpg" or ".jpeg" => bytes.Length >= 3 &&
                bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
            ".png" => bytes.Length >= 8 &&
                bytes.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".pdf" => bytes.Length >= 5 &&
                bytes[..5].SequenceEqual("%PDF-"u8),
            _ => false
        };

    private string ResolveSafePath(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
            throw new AppException(400, "The stored file path is invalid.");

        var fullPath = Path.GetFullPath(Path.Combine(
            _rootPath,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = _rootPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new AppException(400, "The stored file path is invalid.");
        return fullPath;
    }
}
