using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Newtonsoft.Json;
using APUS.Server.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using APUS.Server.Services.Interfaces;
using APUS.Server.Services.Implementations;
using APUS.Server.Data.Repositories.Implementations;
using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.Models;
using System.Diagnostics;
using APUS.Server.Configuration;


namespace APUS.Server
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			builder.Services.AddApplicationServices(builder.Configuration)
				.AddWebTokenServices(builder.Configuration.GetSection("Jwt"));

			var app = builder.Build();
			app.UseCors("AllowAngularDev");

			app.UseDefaultFiles();
			app.UseStaticFiles();


			// Optional: serve files from the uploads folder
			//var uploadPath = "\"C:\\APUSGpxFiles\"";
			//app.UseFileServer(new FileServerOptions
			//{
			//	FileProvider = new PhysicalFileProvider(uploadPath),
			//	RequestPath = "/gpx-files",
			//	EnableDirectoryBrowsing = false
			//});

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}
			app.UseHttpsRedirection();


			app.UseAuthentication();

			app.UseAuthorization();


			app.MapControllers();

			app.MapFallbackToFile("/index.html");

			app.Run();
		}
	}
}