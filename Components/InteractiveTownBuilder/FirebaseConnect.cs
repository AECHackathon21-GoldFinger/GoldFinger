using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rhino.Geometry;

namespace InteractiveTownBuilder
{
    public class FirebaseConnect
    {
        public static IFirebaseClient CreateClient()
        {
            var config = Environment.GetEnvironmentVariable("firebaseConfig");
            if (string.IsNullOrEmpty(config))
            {
                return null;
            }
            
            var configDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(config);
            var firebaseConfig = new FirebaseConfig
            {
                AuthSecret = configDict["apiKey"],
                BasePath = $"https://{configDict["authDomain"]}/"
            };
            return new FirebaseClient(firebaseConfig);
        }

        public static async void GetCubes()
        {
            var client = CreateClient();
            if (client == null)
            {
                return;
            }
            var firebaseResponse = await client.GetAsync("cubes");
            if (firebaseResponse.StatusCode == HttpStatusCode.OK)
            {
                var cubes = ParseFirebaseCubes(firebaseResponse.Body);
            }
        }
        
        public static async void PushCubes(List<Point3d> data)
        {
            var client = CreateClient();
            if (client == null)
            {
                return;
            }
            var points = ParsePointsToVue(data);
            
            var firebaseResponse = await client.SetAsync("cubes", points);
        }
        
        private static List<Box> ParseFirebaseCubes(string data)
        {
            var cubes = new List<Box>();
            var jsonData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(data);
            foreach (var key in jsonData.Keys)
            {
                var _data = jsonData[key]["position"] as JObject;
                var point = _data.ToObject<Dictionary<string, int>>();
                var plane = new Plane(
                    new Point3d(point["x"], point["z"], point["y"]), new Vector3d(0.0, 0.0, 1.0));
                cubes.Add(new Box(plane, new List<Point3d>{new Point3d(point["x"] + 25, point["z"] + 25, point["y"] + 25), new Point3d(point["x"] - 25, point["z"] - 25, point["y"] - 25)}));
            }

            return cubes;
        }
        private static List<Dictionary<string, Dictionary<string, double>>> ParsePointsToVue(List<Point3d> points)
        {
            return points.Select(point => new Dictionary<string, Dictionary<string, double>>
            {
                {
                    GenerateRandomString(), new Dictionary<string, double>
                    {
                        {"x", point.X},
                        {"y", point.Z},
                        {"z", point.Y},
                    }
                }
            }).ToList();
        }

        private static string GenerateRandomString(int length = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            var random = new Random();
            var randomString = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            return randomString;
        }
    }
}