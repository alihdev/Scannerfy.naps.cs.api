using NAPS2.Scan;

namespace Scannerfy.Api.Dtos;

public class ScanOptionsDto
{
    public int Dpi { get; set; }
    public PaperSource PaperSource { get; set; }
    public ScanDevice? Device { get; set; }
}
