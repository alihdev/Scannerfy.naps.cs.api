using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NAPS2.Images;
using NAPS2.Images.Gdi;
using NAPS2.Pdf;
using NAPS2.Scan;
using Scannerfy.Api.Dtos;
using Scannerfy.Api.Exceptions;
using Scannerfy.Api.Shared;

namespace Scannerfy.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ScannerfyController : ControllerBase
{
    private readonly ILogger<ScannerfyController> _logger;
    private static ScanningContext? Context { get; set; }

    public ScannerfyController(ILogger<ScannerfyController> logger)
    {
        _logger = logger;
    }

    [HttpGet("Devices")]
    public async Task<List<ScanDevice>> GetAllDevicesAsync()
    {
        var controller = GetScanController();

        List<ScanDevice> devices = await GetDeviceListFromSupportedDriverAsync(controller);

        return devices;
    }

    private static async Task<List<ScanDevice>> GetDeviceListFromSupportedDriverAsync(ScanController? controller)
    {
        if (controller == null) return [];

        var supportedDrivers = new[] { Driver.Wia, Driver.Twain };

        var devicesPerDriver = await Task.WhenAll(
            supportedDrivers.Select(driver => controller.GetDeviceList(driver))
        );

        List<ScanDevice> devices = devicesPerDriver.SelectMany(devices => devices).ToList();

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

        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

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

    [HttpPost("Scan-Images-As-Pdf")]
    public async Task<IActionResult> GetImagesAsPdfFromScanner(ScanOptionsDto scanOptions)
    {
        var images = await ScanAndGetImages(scanOptions);

        if (images.Count == 0)
        {
            throw new UserFriendlyException(RepsonseCode.IMAGES_404.ToString());
        }

        var outputDir = Path.Combine(Path.GetTempPath(), "ScannerfyOutput");

        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

        if (Context == null) throw new UserFriendlyException(RepsonseCode.CONTEXT_ISSUE.ToString());

        var pdfExporter = new PdfExporter(Context);
        var pdfPath = Path.Combine(outputDir, "ScannedDocument.pdf");

        await pdfExporter.Export(pdfPath, images);

        var fileBytes = await System.IO.File.ReadAllBytesAsync(pdfPath);

        foreach (string file in Directory.GetFiles(outputDir))
        {
            System.IO.File.Delete(file);
        }

        return File(fileBytes, "application/pdf", "ScannedDocument.pdf");
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

        // Set up the worker; this includes starting a worker process in the background so it will be ready to respond
        // when we need it, ref: https://github.com/cyanfish/naps2/blob/master/NAPS2.Sdk.Samples/TwainSample.cs
        scanningContext.SetUpWin32Worker();

        var controller = new ScanController(scanningContext);

        Context = scanningContext;

        return controller;
    }
}
