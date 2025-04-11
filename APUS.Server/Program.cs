
using APUS.Server.Models.Activities;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Newtonsoft.Json;


namespace APUS.Server
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.


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
					policy.WithOrigins("https://localhost:54954") // Angular dev server
						  .AllowAnyHeader()
						  .AllowAnyMethod()
						  .AllowCredentials(); // optional, only if you're using cookies or authentication
				});
			});

			//builder.Services.AddControllers(); // System.Text.Json is used by default

			// end-region

			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();
			builder.Services.AddSingleton<ActivityService>();

			var app = builder.Build();
			app.UseCors("AllowAngularDev");

			app.UseDefaultFiles();
			app.UseStaticFiles();

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
