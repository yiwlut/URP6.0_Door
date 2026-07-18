using UnityEngine;

namespace DoorPuzzle
{
    public static class RainVFX
    {
        public static void Build(Transform parent)
        {
            if (parent == null || parent.Find("Rainy Street VFX") != null) return;
            var root = new GameObject("Rainy Street VFX").transform;
            root.SetParent(parent, false);

            var rainShader = Shader.Find("Blues With You/Rain/Rain Streak");
            if (rainShader == null)
            {
                Debug.LogWarning("[DoorPuzzle] Rain HLSL shaders were not found.");
                return;
            }

            var rainMaterial = new Material(rainShader) { name = "Rain Streak HLSL Runtime" };
            SetFloat(rainMaterial, "_Intensity", 1.65f);
            SetFloat(rainMaterial, "_StreakSharpness", 2.7f);
            SetFloat(rainMaterial, "_SegmentFrequency", 5.5f);
            CreateRain(root, rainMaterial);
            CreateSplashes(root, rainMaterial);
            // Street lamps, their geometry, lights and reflection ribbons are
            // authored in Level Prototype/Lighting Rig. Rain must never spawn
            // level dressing at runtime.
        }

        private static void CreateRain(Transform parent, Material material)
        {
            var gameObject = new GameObject("Backlit Rain Streaks");
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = new Vector3(0f, 9f, 5f);
            var system = gameObject.AddComponent<ParticleSystem>();
            var main = system.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 1.05f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.038f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.64f, 0.82f, 0.12f),
                new Color(0.9f, 0.94f, 1f, 0.42f));
            main.maxParticles = 900;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = system.emission;
            emission.rateOverTime = 440f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(18f, 0.5f, 38f);
            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            // Unity 6 requires all three velocity axes to use the same curve mode.
            // Constant values retain the previous ranges' average direction without
            // producing a transient/persistent X-Y-Z mode mismatch at runtime.
            velocity.x = -0.35f;
            velocity.y = -18.5f;
            velocity.z = 0f;
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.03f;
            renderer.lengthScale = 3.6f;
            renderer.sharedMaterial = material;
        }

        private static void CreateSplashes(Transform parent, Material material)
        {
            var gameObject = new GameObject("Ground Rain Splashes");
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = new Vector3(0f, 0.08f, 5f);
            var system = gameObject.AddComponent<ParticleSystem>();
            var main = system.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.32f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.55f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.09f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.5f, 0.72f, 0.9f, 0.1f),
                new Color(0.9f, 0.95f, 1f, 0.35f));
            main.maxParticles = 180;
            var emission = system.emission;
            emission.rateOverTime = 65f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(16f, 0.05f, 34f);
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
        }

        private static void SetFloat(Material material, string property, float value)
        {
            if (material != null && material.HasProperty(property)) material.SetFloat(property, value);
        }
    }
}
