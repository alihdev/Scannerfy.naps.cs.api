using Microsoft.AspNetCore.Mvc;
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

        IList<FileContentResult> exportedFiles = [];

        foreach (var image in images)
        {
            var imagePage = images.IndexOf(image) + 1;
            using var stream = new MemoryStream();

            // Save image to stream
            image.Save(stream, ImageFileFormat.Jpeg);

            // Reset stream position
            stream.Position = 0;

            var fileBytes = stream.ToArray();

            exportedFiles.Add(File(fileBytes, "image/jpeg", $"ScannedDocument_{imagePage}.jpg"));
        }

        return Ok(exportedFiles);
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
        var fileName = "ScannedDocument.pdf";
        var pdfPath = Path.Combine(outputDir, fileName);

        await pdfExporter.Export(pdfPath, images);

        var fileBytes = await System.IO.File.ReadAllBytesAsync(pdfPath);

        foreach (string file in Directory.GetFiles(outputDir))
        {
            System.IO.File.Delete(file);
        }

        return File(fileBytes, "application/pdf", fileName);
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
        // Note: When scanning with Canon scanners using the TWAIN protocol, an alert displaying "Unknown Error Code 27" as added below may appear after clicking OK. Despite this, the scanning process continues, and the scanned images are returned successfully. Additionally, Brother scanners operate without any errors.
        // Ref: https://github.com/cyanfish/naps2/blob/master/NAPS2.Sdk.Samples/TwainSample.cs
        scanningContext.SetUpWin32Worker();

        var controller = new ScanController(scanningContext);

        Context = scanningContext;

        return controller;
    }
}
