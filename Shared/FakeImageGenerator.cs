using System.Drawing;

namespace Scannerfy.Api.Shared;

public static class FakeImageGenerator
{
    private static readonly Random _random = new();

    public static Image GenerateFakeImage(int width = 200, int height = 200)
    {
        // Bitmap bitmap = new(width, height);
        // using var graphics = Graphics.FromImage(bitmap);
        // 
        // // Generate random background color
        // var backgroundColor = Color.FromArgb(_random.Next(256), _random.Next(256), _random.Next(256));
        // graphics.Clear(backgroundColor);
        // 
        // // Generate random number
        // string randomNumber = "AH" + _random.Next(1000, 9999); // 4-digit random number
        // 
        // // Draw the random number in the center
        // using var font = new Font("Arial", 24, FontStyle.Bold);
        // using var brush = new SolidBrush(Color.Black);
        // var textSize = graphics.MeasureString(randomNumber, font);
        // var position = new PointF((width - textSize.Width) / 2, (height - textSize.Height) / 2);
        // graphics.DrawString(randomNumber, font, brush, position);
        // 
        // return bitmap;

        throw new NotImplementedException();
    }
}