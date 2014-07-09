﻿using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace IsBambooBuildBrokenReader
{
    public class ResultCheckpointer
    {
        private readonly string cacheFile;

        public ResultCheckpointer()
        {
            cacheFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                @"PreviousBambooPlanReading.cache");
        }

        public ResultCheckpoint GetLast()
        {
            if (!File.Exists(cacheFile))
            {
                var writer = File.CreateText(cacheFile);
                writer.Close();
            }
            var cacheStr = File.ReadAllText(cacheFile);
            var reading = JsonConvert.DeserializeObject<ResultCheckpoint>(cacheStr);
            return reading ?? new ResultCheckpoint(0);
        }

        public void Store(ResultCheckpoint checkpoint)
        {
            File.WriteAllText(cacheFile, JsonConvert.SerializeObject(checkpoint));
        }
    }
}