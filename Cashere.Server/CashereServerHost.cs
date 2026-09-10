using Cashere.Data;
using Cashere.Server.Endpoints;
using Cashere.Server.Hubs;
using Cashere.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Cashere.Sync.Dtos;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Cashere.Services; 

namespace Cashere.Server;

// Boots an embedded Kestrel server inside the Avalonia desktop process,
// bound to every network interface so the Android app can reach it over
// LAN. Call StartAsync once on app startup and StopAsync on shutdown.
public class CashereServerHost
{
    private WebApplication? _app;

    public int Port { get; }

    // Exposed so Cashere.Desktop's admin services - built separately in
    // Program.cs, outside this WebApplication's own DI container - can push
    // ProductCatalogChanged notifications through the same SignalR hub
    // connected mobile clients are listening on.
    public IProductCatalogChangeNotifier? CatalogChangeNotifier { get; private set; }

    // Same idea, but for reading rather than pushing: lets Cashere.Desktop's
    // AdminViewModel list whichever mobile clients PosSyncHub currently has
    // connected, without needing a network round trip - both live in this
    // same process.
    public IConnectedDeviceService? ConnectedDevices { get; private set; }

    public CashereServerHost(int port = 5177)
    {
        Port = port;
    }

    public async Task StartAsync(string sqliteDbPath)
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.UseUrls($"http://0.0.0.0:{Port}");

        builder.Services.AddDbContext<CashereDbContext>(options =>
            options.UseSqlite($"Data Source={sqliteDbPath}"));

        builder.Services.AddSignalR();
        builder.Services.AddSingleton<ActiveCartService>();
        builder.Services.AddSingleton<IProductCatalogChangeNotifier, SignalRProductCatalogChangeNotifier>();

        // Registered as both the concrete type (so PosSyncHub can call the
        // Register*/mutator methods) and the interface (so anything outside
        // Cashere.Server - i.e. the desktop admin UI - only ever sees the
        // read-only GetConnectedDevices()/DevicesChanged surface).
        builder.Services.AddSingleton<ConnectedDeviceService>();
        builder.Services.AddSingleton<IConnectedDeviceService>(sp => sp.GetRequiredService<ConnectedDeviceService>());

        builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
            p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

        // Product photos live alongside the SQLite db, under a sibling
        // "media" folder - e.g. %LocalAppData%\Cashere\media\products\17.jpg,
        // served back out at /media/products/17.jpg. Constructing the
        // singleton here (not just registering the type) ensures the folder
        // exists before UseStaticFiles below tries to serve from it.
        var mediaRoot = Path.Combine(Path.GetDirectoryName(sqliteDbPath)!, "media");
        var photoStorage = new ProductPhotoStorage(mediaRoot);
        builder.Services.AddSingleton(photoStorage);

        _app = builder.Build();

        // Grabbed once here, right after the container is built, so
        // Program.cs can hand these to the admin services / AdminViewModel.
        CatalogChangeNotifier = _app.Services.GetRequiredService<IProductCatalogChangeNotifier>();
        ConnectedDevices = _app.Services.GetRequiredService<IConnectedDeviceService>();

        _app.UseCors();

        _app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(mediaRoot),
            RequestPath = "/media"
        });

        _app.MapProductEndpoints();
        _app.MapProductPhotoEndpoints();
        _app.MapHub<PosSyncHub>("/hubs/pos-sync");

        await _app.StartAsync();
    }

    public async Task StopAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}