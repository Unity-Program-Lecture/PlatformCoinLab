using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace ContentServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // 제공할 파일 폴더 하나를 정합니다. Unity 프로젝트 전체를 지정하지 않습니다.
            string folder = builder.Configuration["FilesRoot"] ?? "Content";
            string root = Path.GetFullPath(folder, builder.Environment.ContentRootPath);
            if (!Directory.Exists(root))
            {
                throw new DirectoryNotFoundException("제공할 폴더가 없습니다: " + root);
            }

            using PhysicalFileProvider files = new PhysicalFileProvider(root);

            // 수업에서 쓰는 형식만 제공합니다. .cs, .csproj 등은 제공하지 않습니다.
            FileExtensionContentTypeProvider types = new FileExtensionContentTypeProvider();
            types.Mappings.Clear();
            types.Mappings[".bundle"] = "application/octet-stream";
            types.Mappings[".hash"] = "text/plain";
            types.Mappings[".json"] = "application/json";
            types.Mappings[".html"] = "text/html";
            types.Mappings[".js"] = "application/javascript";
            types.Mappings[".css"] = "text/css";
            types.Mappings[".wasm"] = "application/wasm";
            types.Mappings[".data"] = "application/octet-stream";
            types.Mappings[".png"] = "image/png";
            types.Mappings[".ico"] = "image/x-icon";
            types.Mappings[".svg"] = "image/svg+xml";

            WebApplication app = builder.Build();
            // Web 빌드의 index.html도 같은 서버로 열 수 있게 합니다.
            app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = files,
                ContentTypeProvider = types,
                OnPrepareResponse = context =>
                    context.Context.Response.Headers.CacheControl = "no-cache"
            });

            app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

            Console.WriteLine("파일 제공 폴더: " + root);
            // 네트워크 과정에서 준비한 localhost 개발 인증서를 사용합니다.
            app.Run("https://localhost:5443");
        }
    }
}
