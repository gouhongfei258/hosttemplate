using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HostTemplate.UpdateService
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
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.SerializerOptions.PropertyNameCaseInsensitive = true;
            });

            var app = builder.Build();
            app.UseMiddleware<TokenAuthMiddleware>();
            UpdateApi.Map(app);
            app.Run();
        }
    }
}
