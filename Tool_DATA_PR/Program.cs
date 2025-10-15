using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;
using Tool_DATA_PR.Context;
using Tool_DATA_PR.Service;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                var connectionString = context.Configuration.GetConnectionString("ConnectionString");
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(connectionString));

                // DbContext cho PRODUCT_FORM (BK_PhoiThep)
                var productFormConnection = context.Configuration.GetConnectionString("ConnectionPRODUCT_FORM");
                services.AddDbContext<BkDbContext>(options =>
                    options.UseSqlServer(productFormConnection));

                services.AddTransient<TaoPhieuTuDongService>();
                services.AddTransient<DataBKMISService>();
            });

        var host = builder.Build();

        using (var scope = host.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var service = services.GetRequiredService<TaoPhieuTuDongService>();
                await service.RunAsync();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi xảy ra: {ex.Message}");
                // Ghi log nếu cần
            }
            try
            {
                var dataBkMisService = services.GetRequiredService<DataBKMISService>();
                await dataBkMisService.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi xảy ra ở DataBKMISService: {ex.Message}");
            }
        }
    }
}
