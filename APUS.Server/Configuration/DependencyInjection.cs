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
using APUS.Server.Services.Implementations.UserServices;
using APUS.Server.Services.Implementations.GroupServices;
using APUS.Server.Routing;
using APUS.Server.Services.Implementations.MapServices;

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
					options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
					options.SerializerSettings.PreserveReferencesHandling = PreserveReferencesHandling.None;
					options.SerializerSettings.TypeNameHandling = TypeNameHandling.None; // ← this stops $type / $values
					options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
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

			// Register PagedRoadGraph as a singleton (and dispose on shutdown)
			services.AddSingleton<PagedRoadGraph>(sp =>
			{
				var env = sp.GetRequiredService<IWebHostEnvironment>();

				// Adjust this if your graph_store lives elsewhere
				var rootDir = Path.Combine(env.ContentRootPath, "graph_store");

				// maxTilesInMem same as your earlier tests
				return new PagedRoadGraph(rootDir, maxTilesInMem: 8);
			});

			services.AddSingleton<IElevationSampler>(_ =>
			{
				GdalConfiguration.Configure();

				const string demPath = @"C:\EU-DEM\EU_DEM_mosaic_5deg\eudem_dem_4258_europe.tif";
				return new GdalElevationSampler(demPath);
			});


			var connectionString = configuration.GetConnectionString("DefaultConnection")
						  ?? "Server=(localdb)\\mssqllocaldb;Database=APUSActivityDbDev;Trusted_Connection=True;MultipleActiveResultSets=true";

			services.AddDbContext<AppDbContext>(opt =>
				opt.UseSqlServer(connectionString));

			services.AddEndpointsApiExplorer();
			services.AddSwaggerGen();
			services.AddScoped<IActivityRepository, ActivityRepository>();
			services.AddScoped<ISiteUserRepository, SiteUserRepository>();
			services.AddScoped<ISearchUsersService, SearchUsersService>();
			services.AddSingleton<IStorageService, StorageService>();
			services.AddScoped<IProfilePictureService, ProfilePictureService>();
			services.AddScoped<IFriendService, FriendService>();
			services.AddScoped<IUserRelationRepository, UserRelationRepository>();
			services.AddScoped<ISearchUsersService, SearchUsersService>();
			services.AddTransient<ITrackpointLoader, TcxXmlTrackpointLoader>();
			services.AddTransient<ICreateOsmMapPng, CreateOsmMapPng>();
			services.AddTransient<IRouteService, RouteService>();
			services.AddScoped<IGroupRepository, GroupRepository>();
			services.AddScoped<IGroupService, GroupService>();
			services.AddSingleton<IRandomRouteService, RandomRouteService>();
			services.AddSingleton<IRoutingService, RoutingService>();

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
