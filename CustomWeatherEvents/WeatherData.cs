using HarmonyLib;
using SpaceCraft;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CustomWeatherEvents
{
    public class WeatherDataDict
    {
        public Dictionary<string, WeatherData> events { get; set; } = new Dictionary<string, WeatherData>();
    }
    public class WeatherData
    {
        public bool random { get; set; }
        public bool custom { get; set; } = false;
        public int weight { get; set; } = 100;

        public float duration { get; set; }
        public int rainEmission { get; set; }
        public float wetness { get; set; }
        public string startTerraformStage{ get; set; }
        public string stopTerraformStage{ get; set; }

        public AsteroidEventDataData asteroidEventData { get; set; }

        public WeatherData()
        {

        }
        internal WeatherData(MeteoEventData m, bool random)
        {
            this.random = random;
            duration = m.duration;
            rainEmission = m.rainEmission;
            wetness = m.wetness;
            startTerraformStage = m.startTerraformStage?.GetTerraId();
            stopTerraformStage = m.stopTerraformStage?.GetTerraId();

            var aed = m.asteroidEventData;
            if (aed != null)
            {
                asteroidEventData = new AsteroidEventDataData(aed);
            }
        }
    }

    public class AsteroidEventDataData
    {
        public float spawnOneEvery { get; set; }
        public int maxAsteroidsSimultaneous { get; set; }
        public int maxAsteroidsTotal { get; set; }
        public int distanceFromPlayer { get; set; }
        public AsteroidData asteroid { get; set; }
        public AsteroidEventDataData()
        {

        }
        internal AsteroidEventDataData(AsteroidEventData aed)
        {
            spawnOneEvery = aed.spawnOneEvery;
            maxAsteroidsSimultaneous = aed.maxAsteroidsSimultaneous;
            maxAsteroidsTotal = aed.maxAsteroidsTotal;
            distanceFromPlayer = aed.distanceFromPlayer;

            var a = aed.asteroidGameObject?.GetComponent<Asteroid>();
            if (a != null)
                asteroid = new AsteroidData(a);
        }
    }

    public class AsteroidData
    {
        public AsteroidData()
        {
        }
        internal AsteroidData(Asteroid a)
        {
            shakeOnImpact = a.shakeOnImpact;
            doDamage = a.doDamage;
            destroyTime = a.destroyTime;
            numberOfResourceInDebris = a.numberOfResourceInDebris;

            foreach (var g in a.groupsSelected)
            {
                groupsSelected.Add(g.id);
            }

            debrisNumber = a.debrisNumber;
            placeAsteroidBody = a.placeAsteroidBody;
            debrisDestroyTime = a.debrisDestroyTime;
            debrisSize = a.debrisSize;

            maxLiveTime = (float)AccessTools.Field(typeof(Asteroid), "maxLiveTime").GetValue(a);
            initialSpeed = (float)AccessTools.Field(typeof(Asteroid), "initialSpeed").GetValue(a);
            maxAcceleration = (float)AccessTools.Field(typeof(Asteroid), "maxAcceleration").GetValue(a);
            maxScale = (float)AccessTools.Field(typeof(Asteroid), "maxScale").GetValue(a);
        }
        public bool shakeOnImpact { get; set; }
        public bool doDamage { get; set; }
        public float destroyTime { get; set; } = 10f;
        public float numberOfResourceInDebris { get; set; }
        public List<string> groupsSelected { get; set; } = new List<string>();
        public int debrisNumber { get; set; }
        public bool placeAsteroidBody { get; set; }
        public float debrisDestroyTime { get; set; }
        public float debrisSize { get; set; }
        public float maxLiveTime { get; set; } = 180f;
        public float initialSpeed { get; set; }
        public float maxAcceleration { get; set; }
        public float maxScale { get; set; }
    }
}