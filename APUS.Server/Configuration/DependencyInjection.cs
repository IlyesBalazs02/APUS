using APUS.Routing;
using APUS.Routing;
using APUS.Server.Data;
using APUS.Server.Data.Repositories.Implementations;
using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.Models;
using APUS.Server.Services.Implementations;
using APUS.Server.Services.Implementations.Activity;
using APUS.Server.Services.Implementations.FileServices;
using APUS.Server.Services.Implementations.GroupServices;
using APUS.Server.Services.Implementations.MapServices;
using APUS.Server.Services.Implementations.UserServices;
using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Npgsql;
using OSGeo.GDAL;
using System.Data;
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

			#region Routing

			services.AddSingleton<TiledRoadGraph>(sp =>
			{
				var env = sp.GetRequiredService<IWebHostEnvironment>();

				var rootDir = Path.Combine(env.ContentRootPath, "graph_store");

				return new TiledRoadGraph(rootDir, maxTilesInMem: 16);
			});

			services.AddSingleton<SegmentIndex>(sp =>
			{
				var graph = sp.GetRequiredService<TiledRoadGraph>();
				return Snapper.BuildGlobalIndex(graph, cellDegrees: 0.01);
			});

			services.AddSingleton<Snapper>(sp =>
			{
				var graph = sp.GetRequiredService<TiledRoadGraph>();
				var index = sp.GetRequiredService<SegmentIndex>();
				return new Snapper(graph, index);
			});

			services.AddSingleton<IElevationSampler>(sp =>
			{
				var configuration = sp.GetRequiredService<IConfiguration>();
				var demConnStr = configuration.GetConnectionString("DemConnection")
					?? throw new InvalidOperationException("Missing DemConnection connection string.");

				return new PostgisElevationSampler(
					demConnStr,
					tableName: "public.eu_dem",
					rasterSrid: 4258
				);
			});

			services.AddScoped<IDbConnection>(sp =>
			{
				var config = sp.GetRequiredService<IConfiguration>();
				var connString = config.GetConnectionString("GeoConnection");
				var conn = new NpgsqlConnection(connString);
				conn.Open();
				return conn;
			});


			#endregion



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
			services.AddTransient<ITrackpointLoader, TcxXmlTrackpointLoader>();
			services.AddTransient<ICreateOsmMapPng, CreateOsmMapPng>();
			services.AddTransient<IActivityService, ActivityService>();
			services.AddScoped<IGroupRepository, GroupRepository>();
			services.AddScoped<IGroupService, GroupService>();
			services.AddSingleton<IRoutingService, RoutingService>();
			services.AddScoped<IActivityImageRepository, ActivityImageRepository>();
			services.AddScoped<IActivityTrackLookupService, ActivityTrackLookupService>();
			services.AddSingleton<IHuberRegressor, HuberRegressor>();
			services.AddScoped<ISolarService, SolarService>();
			services.AddScoped<IActivityCommentRepository, ActivityCommentRepository>();
			services.AddScoped<ITrackFileService, TrackFileService>();

			services.AddTransient<ITCXFileService, TCXFileService>();
			services.AddTransient<IGPXFileService, GPXFileService>();

			services.AddTransient<Func<string, IActivityImportService>>(sp => ext =>
			{
				ext = ext?.Trim().ToLowerInvariant();
				return ext switch
				{
					".tcx" => sp.GetRequiredService<ITCXFileService>(),
					".gpx" => sp.GetRequiredService<IGPXFileService>(),
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
