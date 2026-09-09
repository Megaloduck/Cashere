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

namespace Cashere.Server;

// Boots an embedded Kestrel server inside the Avalonia desktop process,
// bound to every network interface so the Android app can reach it over
// LAN. Call StartAsync once on app startup and StopAsync on shutdown.
public class CashereServerHost
{
    private WebApplication? _app;

    public int Port { get; }

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