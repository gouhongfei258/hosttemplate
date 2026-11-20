using System.Text.Json;
using HostTemplate.Backend.Application;
using HostTemplate.Backend.Infrastructure;
using HostTemplate.Backend.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HostTemplate.Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<UpdateServiceOptions>(builder.Configuration.GetSection("UpdateService"));
            builder.Services.AddSingleton<FileUpdateStore>();
            builder.Services.AddSingleton<IVersionPolicy, NewestVersionPolicy>();
            builder.Services.AddHostedService<InboxImportService>();
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                });

            var app = builder.Build();
            app.UseMiddleware<TokenAuthMiddleware>();
            app.MapControllers();
            app.Run();
        }
    }
}
