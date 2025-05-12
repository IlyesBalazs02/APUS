
using APUS.Server.Models;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Newtonsoft.Json;
using APUS.Server.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using APUS.Server.Services;


namespace APUS.Server
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// region HTTTP POST Json
			builder.Services.AddControllers()
				.AddNewtonsoftJson(options => // Ensure the required package is installed
				{
					options.SerializerSettings.TypeNameHandling = TypeNameHandling.Auto;

				});
			

			builder.Services.AddCors(options =>
			{
				options.AddPolicy("AllowAngularDev", policy =>
				{
					policy.WithOrigins(
							"https://0.0.0.0:54954",              // local dev
							"http://192.168.1.174:54954",          // local network access
							"http://localhost:54954")               // optional, just in case
						  .AllowAnyHeader()
						  .AllowAnyMethod()
						  .AllowCredentials();
				});
			});
			// end-region

			builder.Services.AddDbContext<ActivityDbContext>(opt =>
			{
				//opt.UseInMemoryDatabase("ActivitiesDev");
				opt.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=APUSActivityDbDev;Trusted_Connection=True;MultipleActiveResultSets=true");
			});

			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();
			builder.Services.AddTransient<IActivityRepository, ActivityRepository>();
			builder.Services.AddSingleton<IActivityStorageService, ActivityStorageService>();

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

			app.UseAuthorization();

			app.MapControllers();

			app.MapFallbackToFile("/index.html");

			app.Run();
		}
	}
}
