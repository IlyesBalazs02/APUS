
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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using APUS.Server.Services.Interfaces;
using APUS.Server.Services.Implementations;


namespace APUS.Server
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

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
					policy.WithOrigins(
							"https://0.0.0.0:54954",              // local dev
							"https://192.168.1.174:54954",          // local network access
							"https://localhost:54954")               // optional, just in case
						  .AllowAnyHeader()
						  .AllowAnyMethod()
						  .AllowCredentials();
				});
			});

			builder.Services.AddDbContext<ActivityDbContext>(opt =>
			{
				opt.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=APUSActivityDbDev;Trusted_Connection=True;MultipleActiveResultSets=true");
			});

			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();
			builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
			builder.Services.AddSingleton<IStorageService, StorageService>();
			builder.Services.AddTransient<ITrackpointLoader, TcxXmlTrackpointLoader>();
			builder.Services.AddTransient<ICreateOsmMapPng, CreateOsmMapPng>();
			builder.Services.AddTransient<IRouteService, RouteService>();

			builder.Services.AddTransient<ITCXFileService, TCXFileService>();
			builder.Services.AddTransient<IGPXFileService, GPXFileService>();

			builder.Services.AddTransient<Func<string, IActivityImportService>>(sp => ext =>
			{
				ext = ext?.Trim().ToLowerInvariant();
				return ext switch
				{
					".tcx" => sp.GetRequiredService<ITCXFileService>(),   // ← interface, not concrete
					".gpx" => sp.GetRequiredService<IGPXFileService>(),   // ← interface, not concrete
					_ => throw new NotSupportedException($"No importer for '{ext}'")
				};
			});


			builder.Services.AddIdentity<SiteUser, IdentityRole>(options =>
			{
				// Password settings
				options.Password.RequireDigit = true;   // must have at least one number
				options.Password.RequireLowercase = false;  // no lowercase requirement
				options.Password.RequireUppercase = false;  // no uppercase requirement
				options.Password.RequireNonAlphanumeric = false;  // no symbol requirement
				options.Password.RequiredLength = 1;      // minimum length
			})
				.AddEntityFrameworkStores<ActivityDbContext>()
				.AddDefaultTokenProviders();


			//JWT AUTH
			var jwtSection = builder.Configuration.GetSection("Jwt");
			var keyBytes = Encoding.UTF8.GetBytes(jwtSection["Key"]!);

			builder.Services
				.AddAuthentication(options =>
				{
					options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
					options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
				})
				.AddJwtBearer(opts =>
				{
					opts.RequireHttpsMetadata = true;
					opts.SaveToken = true;
					opts.TokenValidationParameters = new TokenValidationParameters
					{
						ValidateIssuerSigningKey = true,
						IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
						ValidateIssuer = true,
						ValidIssuer = jwtSection["Issuer"],
						ValidateAudience = true,
						ValidAudience = jwtSection["Audience"],
						ValidateLifetime = true,
						ClockSkew = TimeSpan.Zero
					};
				});




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
