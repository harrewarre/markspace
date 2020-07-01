using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Markdig;

namespace markspace
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.Use(next => async context =>
            {
                if (Path.HasExtension(context.Request.Path))
                {
                    await next(context);
                    return;
                }

                var potentialPage = getLocalPath(context.Request.Path, env);
                if (File.Exists(potentialPage))
                {
                    var content = await File.ReadAllTextAsync(potentialPage);

                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync(await GeneratePage(GetHtml(content), env));
                    return;
                }

                await next(context);
            });

            app.UseStaticFiles();

            app.Use(next => async context =>
            {
                if (Path.HasExtension(context.Request.Path))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    await context.Response.WriteAsync(string.Empty);
                    return;
                }

                var content = await File.ReadAllTextAsync(GetNotFoundPath(env));

                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.WriteAsync(await GeneratePage(GetHtml(content), env));
            });
        }

        private string getLocalPath(string requestPath, IWebHostEnvironment environment)
        {
            var root = environment.WebRootPath;

            if (requestPath.Equals("/"))
            {
                return Path.Join(root, "index.md");
            }

            return Path.Join(root, $"{requestPath}.md");
        }

        private string GetNotFoundPath(IWebHostEnvironment environment)
        {
            var root = environment.WebRootPath;
            return Path.Join(root, "404.md");
        }

        private string GetHtml(string markdown)
        {
            return Markdown.ToHtml(markdown);
        }

        private async Task<string> GeneratePage(string htmlContent, IWebHostEnvironment environment)
        {
            var root = environment.WebRootPath;
            var page = Path.Join(root, "assets", "doc.html");

            var content = await File.ReadAllTextAsync(page);
            return content.Replace("#{body}#", htmlContent);
        }
    }
}
