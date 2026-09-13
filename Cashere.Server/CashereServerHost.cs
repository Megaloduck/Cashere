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

// Boots an embedded Kestrel server inside the Avalonia desktop process.
// Bind address/port are configurable (see ReceiptAdmin.ServerBindAddress /
// ServerPort) - "0.0.0.0" binds every network adapter so the Android app
// can reach it over LAN, which is the safe default; a specific address
// restricts which interface is reachable. Call StartAsync once on app
// startup and StopAsync on shutdown.
public class CashereServerHost
{
    private WebApplication? _app;

    public int Port { get; }
    public string BindAddress { get; }

    // Exposed so Cashere.Desktop's admin services - built separately in
    // Program.cs, outside this WebApplication's own DI container - can push
    // ProductCatalogChanged notifications through the same SignalR hub
    // connected mobile clients are listening on.
    public IProductCatalogChangeNotifier? CatalogChangeNotifier { get; private set; }

    // Lets Cashere.Desktop's AdminViewModel list/kick whichever mobile
    // clients PosSyncHub currently has connected, without a network round
    // trip - both live in this same process.
    public IConnectedDeviceService? ConnectedDevices { get; private set; }

    public CashereServerHost(int port = 5177, string bindAddress = "0.0.0.0")
    {
        Port = port;
        BindAddress = string.IsNullOrWhiteSpace(bindAddress) ? "0.0.0.0" : bindAddress;
    }

    public async Task StartAsync(string sqliteDbPath)
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.UseUrls($"http://{BindAddress}:{Port}");

        builder.Services.AddDbContext<CashereDbContext>(options =>
            options.UseSqlite($"Data Source={sqliteDbPath}"));

        builder.Services.AddSignalR();
        builder.Services.AddSingleton<ActiveCartService>();
        builder.Services.AddSingleton<IProductCatalogChangeNotifier, SignalRProductCatalogChangeNotifier>();

        // Registered as both the concrete type (so PosSyncHub can call the
        // Register*/mutator methods) and the interface (so anything outside
        // Cashere.Server - i.e. the desktop admin UI - only ever sees the
        // read-only/kick surface).
        builder.Services.AddSingleton<ConnectedDeviceService>();
        builder.Services.AddSingleton<IConnectedDeviceService>(sp => sp.GetRequiredService<ConnectedDeviceService>());

        builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
            p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

        var mediaRoot = Path.Combine(Path.GetDirectoryName(sqliteDbPath)!, "media");
        var photoStorage = new ProductPhotoStorage(mediaRoot);
        builder.Services.AddSingleton(photoStorage);

        _app = builder.Build();

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