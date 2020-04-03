using SFMF;
using System;
using System.IO;
using UnityEngine;

namespace Topspeed
{
    public class Topspeed : IMod
    {
        private const string SettingsPath = @".\SFMF\ModSettings\Topspeed.csv";

        private float newTopSpeed { get; set; }

        private void Start()
        {
            var settings = File.ReadAllLines(SettingsPath);

            foreach (var line in settings)
            {
                if (line == "")
                    continue;

                var parts = line.Split(',');

                if (parts[0] == "Setting")
                    newTopSpeed = float.Parse(parts[2]);
            }

            PlayerMovement.Singleton.forwardSpeedLimits.max = newTopSpeed;
        }

    }
}
