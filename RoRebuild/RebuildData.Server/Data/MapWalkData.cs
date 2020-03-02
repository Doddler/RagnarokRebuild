using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RebuildData.Server.Config;
using RebuildData.Server.Logging;

namespace RebuildData.Shared.Data
{
	public enum CellType
	{
		None = 0,
		Walkable = 1,
		Water = 2,
		Snipable = 4
	}

	public class MapWalkData
	{
		public int Width;
		public int Height;
		private byte[] cellData;
		
		public bool IsCellWalkable(int x, int y) => (cellData[x + y * Width] & 1) == 1;
		public bool IsCellWalkable(Position p) => (cellData[p.X + p.Y * Width] & 1) == 1;
		public bool IsCellSnipable(int x, int y) => (cellData[x + y * Width] & 2) == 2;
		public bool IsCellSnipable(Position p) => (cellData[p.X + p.Y * Width] & 2) == 2;

		public MapWalkData(string name)
		{
			var path = Path.Combine(ServerConfig.MapPath, name);

			//ServerLogger.Log("Loading path data from " + name);

			try
			{
				using var fs = new FileStream(path, FileMode.Open);
				using var br = new BinaryReader(fs);

				Width = br.ReadInt32();
				Height = br.ReadInt32();

				cellData = br.ReadBytes(Width * Height);
			}
			catch (Exception)
			{
				ServerLogger.LogError($"Failed to load map data for file {name}");
				throw;
			}
		}
	}
}
