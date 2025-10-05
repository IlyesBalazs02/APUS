using APUS.Server.Data.Repositories.Implementations;
using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Data;
using APUS.Server.Domain.Models;
using APUS.Server.Services.Implementations;
using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace APUS.Server.Configuration
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddApplicationServices(
	this IServiceCollection services,
	IConfiguration configuration)
		{
			services.AddControllers()
				.AddNewtonsoftJson(options =>
				{
					options.SerializerSettings.TypeNameHandling = TypeNameHandling.Auto;
				});


			services.AddCors(options =>
			{
				options.AddPolicy("AllowAngularDev", policy =>
				{
					policy.WithOrigins(
							"https://0.0.0.0:54954",              // local dev
							"http://192.168.1.174:54954",          // local network acces
							"http://localhost:54954")               // optional, just in case
						  .AllowAnyHeader()
						  .AllowAnyMethod()
						  .AllowCredentials();
					policy.WithOrigins(
							"https://0.0.0.0:54954",              // local dev
							"https://192.168.1.174:54954",          // local network acces
							"https://localhost:54954")               // optional, just in case
						  .AllowAnyHeader()
						  .AllowAnyMethod()
						  .AllowCredentials();
				});
			});

			var connectionString = configuration.GetConnectionString("DefaultConnection")
						  ?? "Server=(localdb)\\mssqllocaldb;Database=APUSActivityDbDev;Trusted_Connection=True;MultipleActiveResultSets=true";

			services.AddDbContext<AppDbContext>(opt =>
				opt.UseSqlServer(connectionString));

			services.AddEndpointsApiExplorer();
			services.AddSwaggerGen();
			services.AddScoped<IActivityRepository, ActivityRepository>();
			services.AddSingleton<IStorageService, StorageService>();
			services.AddTransient<ITrackpointLoader, TcxXmlTrackpointLoader>();
			services.AddTransient<ICreateOsmMapPng, CreateOsmMapPng>();
			services.AddTransient<IRouteService, RouteService>();

			services.AddTransient<ITCXFileService, TCXFileService>();
			services.AddTransient<IGPXFileService, GPXFileService>();

			services.AddTransient<Func<string, IActivityImportService>>(sp => ext =>
			{
				ext = ext?.Trim().ToLowerInvariant();
				return ext switch
				{
					".tcx" => sp.GetRequiredService<ITCXFileService>(),   // ← interface, not concrete
					".gpx" => sp.GetRequiredService<IGPXFileService>(),   // ← interface, not concrete
					_ => throw new NotSupportedException($"No importer for '{ext}'")
				};
			});


			services.AddIdentity<SiteUser, IdentityRole>(options =>
			{
				// Pasword settings
				options.Password.RequireDigit = true;   // have at least one number
				options.Password.RequireLowercase = false;  // no lowercase requirement
				options.Password.RequireUppercase = false;  // no uppercase requirement
				options.Password.RequireNonAlphanumeric = false;  // no symbol requirement
				options.Password.RequiredLength = 1;      // minimum length
			})
				.AddEntityFrameworkStores<AppDbContext>()
				.AddDefaultTokenProviders();

			return services;
		}

		public static IServiceCollection AddWebTokenServices(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			//JWT AUTH
			var keyBytes = Encoding.UTF8.GetBytes(configurationSection["Key"]!);

			services
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
						ValidIssuer = configurationSection["Issuer"],
						ValidateAudience = true,
						ValidAudience = configurationSection["Audience"],
						ValidateLifetime = true,
						ClockSkew = TimeSpan.Zero
					};
				});

			return services;
		}
	}
}
