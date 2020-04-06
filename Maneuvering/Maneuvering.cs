using SFMF;
using System;
using System.IO;
using UnityEngine;

namespace Maneuvering
{
    public class Maneuvering : IMod
    {
        private const string SettingsPath = @".\SFMF\ModSettings\Maneuvering.csv";

        private float ForwardJerk { get; set; }
        private float BreakingJerk { get; set; }
        private float MaxAcceleration { get; set; }
        private float MaxBreaking { get; set; }
        private float Acceleration { get; set; }
        private float RamJetConstant{ get; set; }
        private float Vectoring { get; set; }
        private float RegularTopSpeed { get; set; }
        private float ExcessiveSpeedDragConstant { get; set; }

        private float DragGrowthPower { get; set; }

        private float RamJetGrowthPower { get; set; }

        private float SmoothnessConstant { get; set; }

        private int logInterval { get; set; }
        
        private KeyCode BoostKey { get; set; }
        private KeyCode BreakKey { get; set; }

        private void Start()
        {
            var settings = File.ReadAllLines(SettingsPath);

            foreach (var line in settings)
            {
                if (line == "")
                    continue;

                var parts = line.Split(',');

                if (parts[0] == "Setting")
                {
                    if (parts[1] == "ForwardJerk")
                    {
                        ForwardJerk = float.Parse(parts[2]);
                    } else if (parts[1] == "BreakingJerk")
                    {
                        BreakingJerk = float.Parse(parts[2]);
                    } else if (parts[1] == "MaxAcceleration")
                    {
                        MaxAcceleration = float.Parse(parts[2]);
                    } else if (parts[1] == "MaxBreaking")
                    {
                        MaxBreaking = float.Parse(parts[2]);
                    } else if (parts[1] == "Acceleration")
                    {
                        Acceleration = float.Parse(parts[2]);
                    } else if (parts[1] == "Vectoring")
                    {
                        Vectoring = float.Parse(parts[2]);
                    } else if (parts[1] == "RamJetConstant")
                    {
                        RamJetConstant = float.Parse(parts[2]);
                    } else if (parts[1] == "ExcessiveSpeedDragConstant")
                    {
                        ExcessiveSpeedDragConstant = float.Parse(parts[2]);
                    } else if (parts[1] == "RegularTopSpeed")
                    {
                        RegularTopSpeed = float.Parse(parts[2]);
                    } else if (parts[1] == "DragGrowthPower")
                    {
                        DragGrowthPower = float.Parse(parts[2]);
                    } else if (parts[1] == "RamJetGrowthPower")
                    {
                        RamJetGrowthPower = float.Parse(parts[2]);
                    } else if (parts[1] == "SmoothnessConstant")
                    {
                        SmoothnessConstant = float.Parse(parts[2]);
                    }
                } else
                {
                    if (parts[1] == "BoostKey")
                    {
                        BoostKey = (KeyCode)Enum.Parse(typeof(KeyCode), parts[2]);
                    } else if (parts[1] == "BreakKey")
                    {
                        BreakKey = (KeyCode)Enum.Parse(typeof(KeyCode), parts[2]);
                    }
                }
            }
            Acceleration = 0;
            logInterval = 0;
        }

        //SmoothnessConstant prevents sudden explosions in drag or acceleration that cause stuttering.
        private float SmoothedPower(float value, float power)
        {
            return (float)Math.Pow(value * Math.Pow(SmoothnessConstant, 1 / power), power);
        }

        //Returns RamJet modified values.
        private float RamJet(float jerk)
        {
            return Math.Max(jerk + jerk * SmoothedPower(PlayerMovement.Singleton.currentSpeed, RamJetGrowthPower) * RamJetConstant / RegularTopSpeed, 0);
        }

        private float ExcessiveSpeedDrag(float jerk)
        {
            float ExcessSpeed = Math.Max(PlayerMovement.Singleton.currentSpeed - RegularTopSpeed, 0);
            return Math.Max(jerk + jerk * SmoothedPower(ExcessSpeed, DragGrowthPower) * ExcessiveSpeedDragConstant / RegularTopSpeed, 0);
        }

        private void FixedUpdate()
        {
            var boost = Input.GetKey(BoostKey);
            var airBreak = Input.GetKey(BreakKey);

            if (boost)
            {
                Acceleration = Math.Min(Acceleration + RamJet(ForwardJerk), RamJet(MaxAcceleration));
            }
            if (airBreak)
            {
                Acceleration = Math.Max(Acceleration - ExcessiveSpeedDrag(BreakingJerk), -ExcessiveSpeedDrag(MaxBreaking));
            }
            if (!(boost || airBreak))
            {
                Acceleration = Acceleration > 0 ? Math.Max(Acceleration - RamJet(ForwardJerk) * 4, 0) : Math.Min(Acceleration + ExcessiveSpeedDrag(BreakingJerk) * 4, 0);
            }

            float ExcessSpeed = Math.Max(PlayerMovement.Singleton.currentSpeed - RegularTopSpeed, 0);
            float ExcessDrag = SmoothedPower(ExcessSpeed, DragGrowthPower) * ExcessiveSpeedDragConstant / RegularTopSpeed;
            float EffectiveAcceleration = Acceleration - ExcessDrag;
            
            PlayerMovement.Singleton.forwardSpeedLimits.max = Math.Max(PlayerMovement.Singleton.currentSpeed + EffectiveAcceleration, RegularTopSpeed);

            if (logInterval++ % 60 == 0)
            {
                Console.WriteLine("Drag: " + ExcessDrag + " Acceleration: " + Acceleration + " CurrentSpeed: " + PlayerMovement.Singleton.currentSpeed + "\nTopSpeed: " + PlayerMovement.Singleton.forwardSpeedLimits.max + " MaxAcceleration: " + RamJet(MaxAcceleration) + " RamJetConstant: " + RamJetConstant + "\n");
            }
            PlayerMovement.Singleton.currentSpeed += EffectiveAcceleration;
        }

    }
}
