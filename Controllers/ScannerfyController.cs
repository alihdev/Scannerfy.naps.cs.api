using Microsoft.AspNetCore.Mvc;
using NAPS2.Images;
using NAPS2.Images.Gdi;
using NAPS2.Scan;
using Scannerfy.Api.Dtos;
using Scannerfy.Api.Exceptions;

namespace Scannerfy.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ScannerfyController : ControllerBase
{
    private readonly ILogger<ScannerfyController> _logger;

    public ScannerfyController(ILogger<ScannerfyController> logger)
    {
        _logger = logger;
    }

    [HttpGet("Devices")]
    public async Task<List<ScanDevice>> GetAllDevicesAsync()
    {
        var controller = GetScanController();

        List<ScanDevice> devices = await controller.GetDeviceList();

        return devices;
    }

    [HttpPost("Scan-Images")]
    public async Task<IActionResult> GetImagesFromScanner(ScanOptionsDto scanOptions)
    {
        var images = await ScanAndGetImages(scanOptions);

        if (images.Count == 0)
        {
            throw new UserFriendlyException(RepsonseCode.IMAGES_404.ToString());
        }

        var outputDir = Path.Combine(Path.GetTempPath(), "ScannerfyOutput");
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        foreach (var image in images)
        {
            var imagePage = images.IndexOf(image) + 1;
            var outputPath = Path.Combine(outputDir, $"page_{imagePage}.jpg");
            image.Save(outputPath);
        }

        // TODO: export multi file ?
        using var stream = new MemoryStream();
        images[0].Save(stream, ImageFileFormat.Jpeg);
        var fileBytes = stream.ToArray();

        foreach (string file in Directory.GetFiles(outputDir))
        {
            System.IO.File.Delete(file);
        }

        return File(fileBytes, "image/jpeg", "ScannedDocument.jpg");
    }

    private async Task<List<ProcessedImage>> ScanAndGetImages(ScanOptionsDto inputScanOptions)
    {
        var controller = GetScanController();

        var scanOptions = new ScanOptions
        {
            Device = inputScanOptions.Device,
            PaperSource = inputScanOptions.PaperSource,
            PageSize = PageSize.A4,
            Dpi = inputScanOptions.Dpi
        };

        try
        {
            var images = await controller.Scan(scanOptions).ToListAsync();

            return images;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);

            if (ex.Message.Contains("Offline", StringComparison.OrdinalIgnoreCase))
            {
                throw new UserFriendlyException(RepsonseCode.DEVICE_OFFLINE.ToString());
            }

            if (ex.Message.Contains("Device.ID", StringComparison.OrdinalIgnoreCase) && ex.Message.Contains("Specified", StringComparison.OrdinalIgnoreCase))
            {
                throw new UserFriendlyException(RepsonseCode.DEVICE_404.ToString());
            }

            throw new UserFriendlyException(RepsonseCode.DEVICE_ISSUE.ToString());
        }
    }

    private static ScanController GetScanController()
    {
        var imageContext = new GdiImageContext();
        using var scanningContext = new ScanningContext(imageContext);
        var controller = new ScanController(scanningContext);

        return controller;
    }
}
