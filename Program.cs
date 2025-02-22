using Scalar.AspNetCore;
using Scannerfy.Api.Shared;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .MinimumLevel.Warning()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning) // Suppress Microsoft logs
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)   // Suppress System logs
    .WriteTo.Console()
    .Filter.ByExcluding(logEvent =>
    {
        if (logEvent.MessageTemplate.Text.Contains("Scannerfy!")) return false;

        // Do not exclude this log event
        return true;
    })
);

var app = builder.Build();

// Get the IConfiguration service
var configuration = app.Services.GetRequiredService<IConfiguration>();

// Set application url
var url = configuration["AppURL"] ?? throw new Exception("AppURL 404");
app.Urls.Add(url);

var port = url.Split(":").Last();

// Print logs
Log.Information(
    "Welcome to Scannerfy! " +
    "Listing on port: " + port +
    "\n" +
    //"\r\n   _____  _____          _   _ _   _ ______ _____  ________     __\r\n  / ____|/ ____|   /\\   | \\ | | \\ | |  ____|  __ \\|  ____\\ \\   / /\r\n | (___ | |       /  \\  |  \\| |  \\| | |__  | |__) | |__   \\ \\_/ / \r\n  \\___ \\| |      / /\\ \\ | . ` | . ` |  __| |  _  /|  __|   \\   /  \r\n  ____) | |____ / ____ \\| |\\  | |\\  | |____| | \\ \\| |       | |   \r\n |_____/ \\_____/_/    \\_|_| \\_|_| \\_|______|_|  \\_|_|       |_|   \r\n                                                                  \r\n                                                                  \r\n" +
    "\r\n  /******  /******  /****** /**   /**/**   /**/********/******* /********/**     /**\r\n /**__  **/**__  **/**__  *| *** | *| *** | *| **_____| **__  *| **_____|  **   /**/\r\n| **  \\__| **  \\__| **  \\ *| ****| *| ****| *| **     | **  \\ *| **      \\  ** /**/ \r\n|  ******| **     | *******| ** ** *| ** ** *| *****  | *******| *****    \\  ****/  \r\n \\____  *| **     | **__  *| **  ***| **  ***| **__/  | **__  *| **__/     \\  **/   \r\n /**  \\ *| **    *| **  | *| **\\  **| **\\  **| **     | **  \\ *| **         | **    \r\n|  ******|  ******| **  | *| ** \\  *| ** \\  *| *******| **  | *| **         | **    \r\n \\______/ \\______/|__/  |__|__/  \\__|__/  \\__|________|__/  |__|__/         |__/    \r\n" +
    "\n" +
    "Keep the application open while scanning."
);

var isPortUsed = Utils.IsPortInUse(int.Parse(port));

if (isPortUsed)
{
    Log.Error("Port is already in use. Please free the port and rerun Scannerfy! \n");
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
