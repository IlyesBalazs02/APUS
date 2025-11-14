using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace OSMGraphCreater
{
	public static class RouteGeoJsonExporter
	{
		public static string ExportRouteLineStringGeoJson(
			RoadGraph g,
			List<int> path,
			Snapper.SnapResult A,
			Snapper.SnapResult B)
		{
			if (path == null || path.Count == 0)
			{
				return JsonSerializer.Serialize(new
				{
					type = "FeatureCollection",
					features = Array.Empty<object>()
				});
			}

			var coords = new List<(double Lat, double Lon)>();

			// Start / goal edges and endpoints
			var eA = g.Adj[A.FromNode][A.EdgeIdx];
			int AU = A.FromNode;
			int AV = eA.To;

			var eB = g.Adj[B.FromNode][B.EdgeIdx];
			int BU = B.FromNode;
			int BV = eB.To;

			bool startAtAU = path[0] == AU;
			bool startAtAV = path[0] == AV;
			bool goalAtBU = path[^1] == BU;
			bool goalAtBV = path[^1] == BV;

			// START PARTIAL: snap A → first path node
			if (startAtAU || startAtAV)
			{
				// if path starts at AV, go snap→AV (toEnd = true), else snap→AU (toEnd = false)
				bool toEnd = path[0] == AV;
				var partial = PartialFromSnapToEndpoint(eA, A.SegIdx, A.T, toEnd);
				AppendRange(coords, partial); // this includes snap A
			}
			else
			{
				// fallback: just start at the first node
				var (lat, lon) = g.GetNodeLatLon(path[0]);
				coords.Add((lat, lon));
			}

			// INTERIOR EDGES
			for (int i = 0; i + 1 < path.Count; i++)
			{
				int u = path[i];
				int v = path[i + 1];

				// Is this edge actually the start or goal edge?
				bool isStartEdge =
					(u == AU && v == AV) || (u == AV && v == AU);
				bool isGoalEdge =
					(u == BU && v == BV) || (u == BV && v == BU);

				if (isStartEdge || isGoalEdge)
					continue;

				// Normal interior edge
				var edge = g.Adj[u].FirstOrDefault(ed => ed.To == v);
				if (edge == null || edge.Geometry == null || edge.Geometry.Count == 0)
					continue;

				if (coords.Count > 0)
				{
					// avoid duplicating the first point
					var last = coords[^1];
					var first = edge.Geometry[0];
					if (last.Lat == first.Lat && last.Lon == first.Lon)
					{
						for (int k = 1; k < edge.Geometry.Count; k++)
							coords.Add(edge.Geometry[k]);
					}
					else
					{
						coords.AddRange(edge.Geometry);
					}
				}
				else
				{
					coords.AddRange(edge.Geometry);
				}
			}

			// GOAL PARTIAL: last path node → snap B
			if (goalAtBU || goalAtBV)
			{
				// if we arrived at BV, snap is on the "end" side, otherwise on the "start" side
				bool toEnd = goalAtBV;
				var partial = PartialFromSnapToEndpoint(eB, B.SegIdx, B.T, toEnd);
				// partial is [snapB, ..., endpoint]; we need [endpoint, ..., snapB]
				partial.Reverse();
				AppendRange(coords, partial, skipFirstIfSameAsLast: true);
			}
			else
			{
				// fallback: just append snap B
				coords.Add((B.Lat, B.Lon));
			}

			// Build GeoJSON LineString
			var featureCollection = new
			{
				type = "FeatureCollection",
				features = new[]
				{
					new
					{
						type = "Feature",
						properties = new { },
						geometry = new
						{
							type = "LineString",
							coordinates = coords
								.Select(p => new[] { p.Lon, p.Lat })
								.ToArray()
						}
					}
				}
			};

			return JsonSerializer.Serialize(featureCollection);
		}

		/// <summary>
		/// Append src to dst, optionally skipping the first point if it's equal
		/// to the last point currently in dst (to avoid tiny loops/duplicates).
		/// </summary>
		private static void AppendRange(
			List<(double Lat, double Lon)> dst,
			List<(double Lat, double Lon)> src,
			bool skipFirstIfSameAsLast = false)
		{
			if (src == null || src.Count == 0) return;

			int startIndex = 0;
			if (skipFirstIfSameAsLast && dst.Count > 0)
			{
				var last = dst[^1];
				if (last.Lat == src[0].Lat && last.Lon == src[0].Lon)
					startIndex = 1;
			}

			for (int i = startIndex; i < src.Count; i++)
				dst.Add(src[i]);
		}

		/// <summary>
		/// Returns coordinates from the snap point to either the edge start or the edge end.
		/// If toEnd==true: returns [snap, ..., edge end].
		/// If toEnd==false: returns [snap, ..., edge start].
		/// </summary>
		private static List<(double Lat, double Lon)> PartialFromSnapToEndpoint(
			RoadGraph.Edge e,
			int segIdx,
			double t,
			bool toEnd)
		{
			var outCoords = new List<(double Lat, double Lon)>();

			if (e.Geometry == null || e.Geometry.Count < 2)
				return outCoords;

			// Clamp seg index just in case
			if (segIdx < 0) segIdx = 0;
			if (segIdx >= e.Geometry.Count - 1) segIdx = e.Geometry.Count - 2;

			// Segment endpoints
			var a = e.Geometry[segIdx];
			var b = e.Geometry[segIdx + 1];

			// Linear interpolation in lat/lon (OK for tiny segment)
			double snapLat = a.Lat + (b.Lat - a.Lat) * t;
			double snapLon = a.Lon + (b.Lon - a.Lon) * t;

			outCoords.Add((snapLat, snapLon));

			if (toEnd)
			{
				// snap → ... → edge end (e.To)
				for (int i = segIdx + 1; i < e.Geometry.Count; i++)
					outCoords.Add(e.Geometry[i]);
			}
			else
			{
				// snap → ... → edge start (e.From)
				for (int i = segIdx; i >= 0; i--)
					outCoords.Add(e.Geometry[i]);
			}

			return outCoords;
		}
	}
}
