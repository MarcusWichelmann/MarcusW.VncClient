using BlazorVncClient.Components;
using BlazorVncClient.Services;
using BlazorVncClient.ViewModels;
using MarcusW.VncClient;
using MarcusW.VncClient.Blazor;
using MarcusW.VncClient.Blazor.Extensions;
using System.Linq;
using System.Runtime.ExceptionServices;

// Set up global exception handlers for background threads
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    var exception = e.ExceptionObject as Exception;
    if (exception is OperationCanceledException || exception is ObjectDisposedException)
    {
        // These are expected when cancelling VNC connections - just log them
        Console.WriteLine($"Expected exception during VNC disconnection: {exception?.GetType().Name} - {exception?.Message}");
        
        // For VNC-related cancellation exceptions, try to prevent termination
        if (exception.StackTrace?.Contains("MarcusW.VncClient") == true)
        {
            Console.WriteLine("Attempting to prevent termination for VNC-related cancellation...");
            // Unfortunately, we can't prevent termination from here in .NET 6+
        }
    }
    else
    {
        Console.WriteLine($"Unhandled exception: {exception?.Message}");
        Console.WriteLine($"Stack trace: {exception?.StackTrace}");
    }
};

TaskScheduler.UnobservedTaskException += (sender, e) =>
{
    if (e.Exception.InnerExceptions.Any(ex => ex is OperationCanceledException || ex is ObjectDisposedException))
    {
        // Mark as observed to prevent app termination
        e.SetObserved();
        Console.WriteLine("Observed expected task exception during VNC disconnection");
    }
    else
    {
        Console.WriteLine($"Unobserved task exception: {e.Exception.Message}");
    }
};

// Additional handler for first chance exceptions
AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
{
    if (e.Exception is OperationCanceledException && 
        e.Exception.StackTrace?.Contains("FramebufferUpdateMessageType") == true)
    {
        // This is the specific VNC framebuffer cancellation - we expect this
        Console.WriteLine("Intercepted expected VNC framebuffer cancellation exception");
    }
};

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add VNC client services
builder.Services.AddVncClientServices(options =>
{
    //options.EnableDirtyRectangleRendering = true;
    //options.MaxDirtyRectangles = 50;
    //options.EnableFramebufferCaching = true;
    //options.DefaultCanvasSize = new Size(800, 600);
    //options.DefaultDpi = 96.0;
});

// Add application-specific services
builder.Services.AddScoped<ConnectionManager>();
builder.Services.AddScoped<InteractiveAuthenticationHandler>();
builder.Services.AddScoped<MainWindowViewModel>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Commented out for development - uncomment if HTTPS certificate is configured
// app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
