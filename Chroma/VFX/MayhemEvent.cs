﻿using IPA.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace Chroma.VFX
{
    internal class MayhemEvent
    {
        internal static List<LightWithId>[] LightWithIdManager
        {
            get
            {
                if (manager == null) manager = GameObject.Find("LightWithIdManager").GetComponent<LightWithIdManager>().GetPrivateField<List<LightWithId>[]>("_lights");
                return manager;
            }
        }

        private static List<LightWithId>[] manager;

        internal static void ClearManager()
        {
            manager = null;
        }

        internal static void ActivateTechnicolour(BeatmapEventData baseData, LightSwitchEventEffect lse)
        {
            LightWithId[] lights = LightWithIdManager[lse.LightsID].ToArray();
            for (int i = 0; i < lights.Length; i++) lights[i].ColorWasSet(ColourManager.GetTechnicolour(baseData.value > 3, baseData.time + lights[i].GetInstanceID(), Settings.ChromaConfig.TechnicolourLightsStyle));
        }

        internal static void ParticleTechnicolour(BeatmapEventData baseData, ParticleSystemEventEffect lse)
        {
            ParticleSystem.MainModule _mainmodule = lse.GetPrivateField<ParticleSystem.MainModule>("_mainModule");
            ParticleSystem.Particle[] _particles = lse.GetPrivateField<ParticleSystem.Particle[]>("_particles");
            ParticleSystem _particleSystem = lse.GetPrivateField<ParticleSystem>("_particleSystem");
            _mainmodule.startColor = ColourManager.GetTechnicolour(baseData.value > 3, baseData.time, Settings.ChromaConfig.TechnicolourLightsStyle);
            _particleSystem.GetParticles(_particles, _particles.Length);
            for (int i = 0; i < _particleSystem.particleCount; i++)
            {
                _particles[i].startColor = ColourManager.GetTechnicolour(baseData.value > 3, baseData.time + _particles[i].randomSeed, Settings.ChromaConfig.TechnicolourLightsStyle);
            }
            _particleSystem.SetParticles(_particles, _particleSystem.particleCount);
        }
    }
}