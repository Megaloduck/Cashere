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

        _app = builder.Build();

        _app.UseCors();
        _app.MapProductEndpoints();
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
