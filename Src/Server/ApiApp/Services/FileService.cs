using ApiApp.Exceptions;
using ApiApp.Helpers;
using Azure.Core;
using DataAccess.Context;
using DataAccess.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System.Diagnostics;
using System.Net;
using System.Reflection.Metadata;
using System.Security.Permissions;
using System.Text.RegularExpressions;

namespace ApiApp.Services;


public interface IFileService
{
    Task DownloadFileAsync(string filepath, Stream body, int idMemoryArea);
    Task UploadFileAsync(MultipartSection section, ContentDispositionHeaderValue contentDisposition,
        int idMemoryArea, string filepath);
    Task CreateMetadataOnDbAsync(MetadataModel metadata);
    Task DeleteFileAsync(string fileName, string filePath, int idMemoryArea);
    bool DoesMetadataExists(string filePath, string fileName, int idMemoryArea);
    Task<bool> VerifyMemoryAreaAccessAsync(int idMemoryArea, UserModel user);
    Task<(int idMemoryArea, string ContentDispositionHeader, MultipartSection section, ContentDispositionHeaderValue contentDisposition, string fileSection)> CheckUploadRequestValidity(HttpRequest request);
    string GetValidFilePathUploadRequest(string ContentDispositionHeader, string fileName);
    void DeleteFileIfCreated(string filename, string? filepath = null);
    Task DeleteMetadataIfCreatedAsync(string fileName, string? filepath);
}
public class FileService : IFileService
{
    private readonly DataContext _context;
    private readonly IAesEncryptionService _aesEncryptionService;
    public FileService(DataContext dataContext, IAesEncryptionService aesEncryptionService)
    {
        _context = dataContext;
        _aesEncryptionService = aesEncryptionService;
    }

    public async Task CreateMetadataOnDbAsync(MetadataModel metadata)
    {
        _context.Metadatas.Add(metadata);
        await _context.SaveChangesAsync();
    }

    public async Task UploadFileAsync(MultipartSection section,
                                      ContentDispositionHeaderValue contentDisposition,
                                      int idMemoryArea,
                                      string filepath)
    {
        if (filepath == "/")
            filepath = "";

        string filePath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, $"UploadedFiles/{idMemoryArea}", filepath));

        if (!Directory.Exists(filePath))
        {
            Directory.CreateDirectory(filePath);
        }

        using var fsIn = new FileStream(Path.Combine(filePath, contentDisposition.FileName.Value + ".enc"), FileMode.Create);

        // time diagnostic
        var watch = Stopwatch.StartNew();
        Debug.WriteLine("Start encryption");

        // Get encryption key of the specified memory area
        var key = getKeyFromMemoryArea(idMemoryArea);

        // Create Salt in cryptographic random way and write to dest file
        var salt = _aesEncryptionService.RandomByteArray(16);
        await fsIn.WriteAsync(salt, 0, salt.Length);

        try
        {
            await _aesEncryptionService.EncryptStreamAsync(section.Body, fsIn, key, salt);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            throw new ClientResponseException(ex.Message, HttpStatusCode.InternalServerError, ex);
        }

        // time diagnostic
        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        var elapsedDateTime = watch.Elapsed;
        Debug.WriteLine($"Elapsed time: {elapsedMs} ms");
        Debug.WriteLine($"Elapsed time: {elapsedDateTime}");
    }

    public async Task<(int idMemoryArea, string ContentDispositionHeader,MultipartSection section, ContentDispositionHeaderValue contentDisposition, string fileSection)> 
        CheckUploadRequestValidity(HttpRequest request)
    {
        int idMemoryArea;
        string ContentDispositionHeader;
        MultipartSection? section;
        ContentDispositionHeaderValue? contentDisposition;
        FileMultipartSection? fileSection;


        // Read the memory area id from the request header
        request.Headers.TryGetValue("idMemoryArea", out var idMemoryAreaStr);
        if (!int.TryParse(idMemoryAreaStr, out idMemoryArea))
        {
            throw new ClientResponseException("idMemoryArea is not a number", HttpStatusCode.BadRequest);
        }

        // Verify if the memoryArea exists and the user has access to it
        if (request.HttpContext.Items["User"] is not UserModel user)
        {
            throw new ClientResponseException(HttpStatusCode.Unauthorized);
        }

        if (!await VerifyMemoryAreaAccessAsync(idMemoryArea, user))
        {
            throw new ClientResponseException(HttpStatusCode.Unauthorized);
        }

        // Read the Content Dispostion
        if (!request.Headers.TryGetValue("Content-Disposition", out StringValues ContentDispositionHeaderStringValues) ||
            ContentDispositionHeaderStringValues == "")
        {
            throw new ClientResponseException("Content-Disposition is empty", HttpStatusCode.BadRequest);
        }

        ContentDispositionHeader = ContentDispositionHeaderStringValues.ToString();
        int bufferSize = 1024 * 1024; // 1MB
        
        // Get the boundary from the request
        // This is automatically set, it's used to split between parameters
        // Watch out, even if we don't pass many parameter, i tested that with large files without this it stop without finishing the load
        var boundary = HeaderUtilities.RemoveQuotes(
                MediaTypeHeaderValue.Parse(request.ContentType).Boundary
            ).Value;

        var reader = new MultipartReader(boundary, request.Body, bufferSize);
        section = await reader.ReadNextSectionAsync();
        if (section is null)
        {
            throw new ClientResponseException("No file has been found", HttpStatusCode.BadRequest);
        }

        if (!ContentDispositionHeaderValue.TryParse(
                    section.ContentDisposition, out contentDisposition)
                    || contentDisposition is null)
        {
            throw new ClientResponseException("Error Parsing the file provided. Try Again.", HttpStatusCode.BadRequest);
        }

        fileSection = section.AsFileSection();
        if (fileSection is null || string.IsNullOrEmpty(fileSection.FileName))
        {
            throw new ClientResponseException("Error Parsing the file provided. Try Again.", HttpStatusCode.BadRequest);
        }

        return
            (idMemoryArea,
            ContentDispositionHeader,
            section,
            contentDisposition,
            fileSection.FileName);
    }

    public string GetValidFilePathUploadRequest(string ContentDispositionHeader, string fileName)
    {
        string regexPattern = @"filepath=([^;]+)";
        Match match = Regex.Match(ContentDispositionHeader, regexPattern);
        string filepath;

        if (match.Success && match.Groups.Count > 1)
        {
            filepath = match.Groups[1].Value;
        }
        else
        {
            filepath = string.Empty;
        }

        if (filepath.EndsWith("\\"))
        {
            filepath = filepath.Substring(0, filepath.Length - 2);
        }


        if (filepath is null ||
            string.IsNullOrEmpty(fileName)
        )
            throw new ClientResponseException("Filepath not provided, try again.", HttpStatusCode.BadRequest);

        if (!filepath.EndsWith("/"))
        {
            filepath += "/";
        }

        return filepath;
    }

    private ContentDispositionHeaderValue CheckRequestValidity(MultipartSection section)
    {
        if (!ContentDispositionHeaderValue.TryParse(
                    section.ContentDisposition, out var contentDisposition)
                    || contentDisposition is null)
        {
            throw new ClientResponseException("Error Parsing the file provided. Try Again.", HttpStatusCode.BadRequest);
        }

        if (!contentDisposition.DispositionType.Equals("form-data") ||
        (string.IsNullOrEmpty(contentDisposition.FileName.Value) &&
        string.IsNullOrEmpty(contentDisposition.FileNameStar.Value)))
        {
            throw new ClientResponseException("Error Parsing the file provided. Try Again.", HttpStatusCode.BadRequest);
        }

        return contentDisposition;
    }


    private byte[] getKeyFromMemoryArea(int idMemoryArea)
    {
        var memoryArea = _context.MemoryAreas.Find(idMemoryArea);
        return memoryArea!.EncryptionKey;
    }


    public async Task DownloadFileAsync(string filepath, Stream body, int idMemoryArea)
    {
        string filePath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, $"UploadedFiles/{idMemoryArea}"));

        if (!Directory.Exists(filePath))
        {
            Debug.WriteLine("Directory not found");
            throw new ClientResponseException("Directory not found", HttpStatusCode.NotFound);
        }

        using var fsIn = new FileStream(Path.Combine(filePath, filepath + ".enc"), FileMode.Open, FileAccess.Read);

        var key = getKeyFromMemoryArea(idMemoryArea);

        byte[] salt = new byte[16];
        await fsIn.ReadAsync(salt, 0, salt.Length);

        // time diagnostic
        var watch = Stopwatch.StartNew();
        Debug.WriteLine("Start decryption");

        try
        {
            await _aesEncryptionService.DecryptStreamAsync(fsIn, body, key, salt);
            // body.Position = 0; // Removed since was throwing System.NotSupportedException, Seems to work fine anyway without it
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            throw new ClientResponseException(ex.Message, HttpStatusCode.InternalServerError, ex);
        }

        // time diagnostic
        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        var elapsedDateTime = watch.Elapsed;
        Debug.WriteLine($"Elapsed time: {elapsedMs} ms");
        Debug.WriteLine($"Elapsed time: {elapsedDateTime}");
    }
    public async Task DeleteFileAsync(string fileName, string filePath, int idMemoryArea)
    {
        string localFilePath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, $"UploadedFiles/{idMemoryArea}", filePath, fileName + ".enc"));

        if (!filePath.EndsWith("/")) filePath += "/";
        var metadata = await _context.Metadatas.FirstOrDefaultAsync(x => x.Path == filePath && x.Filename == fileName && x.MemoryAreaId == idMemoryArea);

        if (metadata is null)
            throw new ClientResponseException("Metadata not found", HttpStatusCode.NotFound);

        File.Delete(localFilePath);
        _context.Metadatas.Remove(metadata);
        _context.SaveChanges();
    }
    public void DeleteFileIfCreated(string filename, string? filepath = null)
    {
        // Delete file if created
        string filePath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "UploadedFiles", filepath ?? string.Empty));
        if (File.Exists(Path.Combine(filePath, filename + ".enc")))
        {
            File.Delete(Path.Combine(filePath, filename + ".enc"));
        }
    }


    //public bool HasMemoryAreaAccess(int idMemoryArea, string token)
    //{
    //    // test if user has access to id memory area

    //    int userId = JwtDecoder.GetIdFromToken(token);

    //    if (_context.MemoryAreas.Single((mem) => mem.Id == idMemoryArea).Users.Single((usr) => usr.Id == usr.Id) != null)
    //    {
    //        return true;
    //    }
    //    else
    //    {
    //        return false;
    //    }
    //}

    public async Task<bool> VerifyMemoryAreaAccessAsync(int idMemoryArea, UserModel user)
    {
        await _context
            .Entry(user)
            .Collection(u => u.MemoryAreasPartecipated)
            .LoadAsync();

        return user
            .MemoryAreasPartecipated?
            .SingleOrDefault((mem) => mem.Id == idMemoryArea)
            is not null;
    }

    public bool DoesMetadataExists(string filePath, string fileName, int idMemoryArea)
    {
        if (!filePath.EndsWith("/")) filePath += "/";
        return _context
            .Metadatas
            .FirstOrDefault(x => x.Path == filePath && x.Filename == fileName && x.MemoryAreaId == idMemoryArea)
            is not null;
    }

    public async Task DeleteMetadataIfCreatedAsync(string fileName, string? filepath)
    {
        if (string.IsNullOrEmpty(filepath))
            filepath = "/";

        var metadata = await _context.Metadatas.SingleOrDefaultAsync(m => m.Filename == fileName && m.Path == filepath);

        if (metadata is not null)
            _context.Metadatas.Remove(metadata);

        await _context.SaveChangesAsync();
    }
}