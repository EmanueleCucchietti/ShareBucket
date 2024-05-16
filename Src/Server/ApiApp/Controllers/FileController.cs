using ApiApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Diagnostics;
using System.Text.RegularExpressions;
using DataAccess.Models;
using ApiApp.Exceptions;
using System.Net;
using Microsoft.Extensions.Primitives;
using Microsoft.CodeAnalysis;
using static System.Collections.Specialized.BitVector32;

namespace ApiApp.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class FileController : Controller
{
    private readonly IFileService _fileService;

    public FileController(IFileService fileService)
    {
        _fileService = fileService;
    }

    [Route("UploadFile")]
    [HttpPost]
    [RequestSizeLimit(5_000_000_000)]
    public async Task<IActionResult> UploadFile()
    {
        // Get Parameters from requests and check their validity, otherwise throws exception
        (int idMemoryArea,
         string ContentDispositionHeader,
         var section,
         var contentDisposition,
         var fileName) = await _fileService.CheckUploadRequestValidity(Request);

        // Get the filepath if found, otherwise throws exception with BadRequest 400
        string filepath = _fileService.GetValidFilePathUploadRequest(ContentDispositionHeader, fileName);

        // Verify if the metadata already exists
        if (_fileService.DoesMetadataExists(filepath, fileName, idMemoryArea))
        {
            return BadRequest("File already exists");
        }

        // Create the metadata
        var metadata = new MetadataModel
        {
            Filename = fileName,
            Path = filepath,
            FileExtension = Path.GetExtension(fileName),
            DataCreation = DateTime.Now,
            MemoryAreaId = idMemoryArea,
        };


        try
        {
            await _fileService.CreateMetadataOnDbAsync(metadata);

            await _fileService.UploadFileAsync(section, contentDisposition, idMemoryArea, filepath);
        }
        catch (Exception ex)
        {
            await _fileService.DeleteMetadataIfCreatedAsync(fileName, filepath);
            _fileService.DeleteFileIfCreated(fileName, filepath);
            throw new ClientResponseException("Error during file creation. Try Again.", HttpStatusCode.InternalServerError, ex);
        }

        return new EmptyResult();
    }


    [Route("DownloadFile")]
    [HttpPost]
    [RequestSizeLimit(5_000_000_000)]
    public async Task<IActionResult> DownloadFile([FromQuery] string fileName, [FromQuery] string? filePath)
    {
        // Read the memory area id from the request header
        Request.Headers.TryGetValue("idMemoryArea", out var idMemoryAreaStr);
        if (!int.TryParse(idMemoryAreaStr, out var idMemoryArea))
        {
            return BadRequest("idMemoryArea is not a number");
        }
        if (filePath is null)
        {
            filePath = string.Empty;
        }
        if (filePath.EndsWith("\\"))
        {
            filePath = filePath.Substring(0, filePath.Length - 2);
        }

        // Verify if the memoryArea exists and the user has access to it
        if (Request.HttpContext.Items["User"] is not UserModel user)
        {
            return Unauthorized();
        }
        if (!await _fileService.VerifyMemoryAreaAccessAsync(idMemoryArea, user))
        {
            return Unauthorized();
        }

        if (!_fileService.DoesMetadataExists(filePath, fileName, idMemoryArea))
        {
            return BadRequest("File doesn't exists");
        }

        string localFilePath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, $"UploadedFiles/{idMemoryArea}", filePath));

        if (!Directory.Exists(localFilePath))
        {
            Debug.WriteLine("Directory not found");
            return BadRequest("Directory not found");
        }
        string combinedPath = Path.Combine(localFilePath, fileName);
        if (!System.IO.File.Exists(combinedPath + ".enc"))
        {
            Debug.WriteLine("File not found");
            return BadRequest("File not found");
        }

        Response.Clear();
        Response.Headers.Append("Content-Type", "application/octet-stream");
        Response.Headers.Append("Content-Disposition", "attachment;filename=" + fileName);

        try
        {
            await _fileService.DownloadFileAsync(combinedPath, Response.Body, idMemoryArea);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            throw new ClientResponseException("Error during file download. Try Again.", HttpStatusCode.InternalServerError, ex);
        }

        return new EmptyResult();

    }

    [Route("DeleteFile")]
    [HttpDelete]
    public async Task<ActionResult> DeleteFile([FromQuery] string fileName, [FromQuery] string? filePath)
    {
        // Read the memory area id from the request header
        Request.Headers.TryGetValue("idMemoryArea", out var idMemoryAreaStr);
        if (!int.TryParse(idMemoryAreaStr, out var idMemoryArea))
        {
            return BadRequest("idMemoryArea is not a number");
        }
        if (filePath is null)
        {
            filePath = string.Empty;
        }
        if (filePath.EndsWith("\\"))
        {
            filePath = filePath.Substring(0, filePath.Length - 2);
        }

        // Verify if the memoryArea exists and the user has access to it
        if (Request.HttpContext.Items["User"] is not UserModel user)
        {
            return Unauthorized();
        }
        if (!await _fileService.VerifyMemoryAreaAccessAsync(idMemoryArea, user))
        {
            return Unauthorized();
        }

        string localFilePath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, $"UploadedFiles/{idMemoryArea}", filePath));

        if (!Directory.Exists(localFilePath))
        {
            Debug.WriteLine("Directory not found");
            return BadRequest("Directory not found");
        }
        string combinedPath = Path.Combine(localFilePath, fileName);
        if (!System.IO.File.Exists(combinedPath + ".enc"))
        {
            Debug.WriteLine("File not found");
            return BadRequest("File not found");
        }

        try
        {
            /// TODO

            await _fileService.DeleteFileAsync(fileName, filePath, idMemoryArea);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);

            throw new ClientResponseException("Error during deletion", HttpStatusCode.BadRequest, ex);

        }

        return Ok();
    }
}