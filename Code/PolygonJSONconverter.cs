using System;
using NetTopologySuite.Geometries;
using System.Text.Json;
using System.IO;
using static System.Runtime.InteropServices.JavaScript.JSType;
public class Program
{
    public static void Main(string[] args)
    {
        Dictionary<int, List<double>> values = new(); 
        List<double> coords = new();

        try
        {
            StreamReader sr = new StreamReader("C:\\Users\\Moosdorf\\source\\repos\\TestPolygonLineIntersectr\\TestPolygonLineIntersectr\\newestcoords.txt");
            var line = "";
            while ((line = sr.ReadLine()) != null)
            {
                line = line.Trim();
                if (line == null || line.Length <= 0) continue;
                if (line[0] != '1' && line[0] != '5')
                {
                    if (line == "\"type\": \"Polygon\"")
                    {
                        values[values.Count] = coords;
                        coords = new();
                    }
                    continue;
                }
                if (line[0] == '1')
                {
                    line = line.Replace(",", "");
                }
                coords.Add(Convert.ToDouble(line));
            }
            sr.Close();
            string json = JsonSerializer.Serialize(values, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("cleanCoords.json", json);

        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: " + e.Message);
        }
    }
}