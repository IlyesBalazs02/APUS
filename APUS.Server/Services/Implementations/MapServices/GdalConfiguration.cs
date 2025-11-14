using MaxRev.Gdal.Core;
using OSGeo.GDAL;
using OSGeo.OGR;

namespace APUS.Server.Services.Implementations.MapServices
{
	public static class GdalConfiguration
	{
		private static bool _configured;

		public static void Configure()
		{
			if (_configured) return;

			// sets GDAL_DATA, PROJ_LIB, loads native dlls, etc.
			GdalBase.ConfigureAll();

			Gdal.AllRegister();
			Ogr.RegisterAll();

			_configured = true;
		}
	}
}
