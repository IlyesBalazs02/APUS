
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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


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
					policy.WithOrigins(
							"https://0.0.0.0:54954",              // local dev
							"https://192.168.1.174:54954",          // local network access
							"https://localhost:54954")               // optional, just in case
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
			builder.Services.AddSingleton<IStorageService, StorageService>();
			builder.Services.AddScoped<ITrackpointLoader, TcxXmlTrackpointLoader>();

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
