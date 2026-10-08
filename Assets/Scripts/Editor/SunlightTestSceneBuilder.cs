using System.IO;
using SunlightSurvivor.Sunlight;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SunlightSurvivor.EditorTools
{
    /// <summary>
    /// Builds Assets/Scenes/Test_Sunlight.unity: a checkerboard tilemap arena, a dim global light
    /// and a SunlightManager wired to a SunBeamView. Rerun it any time to regenerate the scene.
    /// </summary>
    public static class SunlightTestSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Test_Sunlight.unity";
        const string SpriteFolder = "Assets/Art/Sprites";
        const string TileFolder = "Assets/Art/Tiles";
        const string SquareSpritePath = SpriteFolder + "/WhiteSquare.png";
        const string LitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";
        const string UnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        const int ArenaWidth = 20;
        const int ArenaHeight = 12;
        const float AmbientIntensity = 0.45f;

        [MenuItem("Tools/SunlightSurvivor/Build Sunlight Test Scene")]
        public static void Build()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Build Sunlight Test Scene", $"{ScenePath} already exists. Overwrite it?", "Overwrite", "Cancel"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // Open the new scene first: NewScene(Single) unloads unreferenced assets, which would
            // destroy tiles/sprites loaded beforehand and leave the tilemap empty.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Sprite square = GetOrCreateSquareSprite();
            Tile lightTile = GetOrCreateTile("GroundTile_Light", square, new Color(0.62f, 0.66f, 0.56f));
            Tile darkTile = GetOrCreateTile("GroundTile_Dark", square, new Color(0.52f, 0.56f, 0.47f));
            var litMaterial = AssetDatabase.LoadAssetAtPath<Material>(LitMaterialPath);
            var unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
            if (square == null || lightTile == null || darkTile == null)
            {
                Debug.LogError("Sunlight test scene: failed to load the square sprite or ground tiles.");
                return;
            }

            CreateCamera();
            CreateGlobalLight();
            Tilemap arena = CreateArena(lightTile, darkTile, litMaterial);
            CreateSunlight(arena, square, unlitMaterial);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Built sunlight test scene at {ScenePath}. Press Play to watch the sunlight scan.");
        }

        static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 0f, -10f);

            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = ArenaHeight * 0.5f + 1f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
        }

        static void CreateGlobalLight()
        {
            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.color = new Color(0.65f, 0.7f, 1f);   // cool "shade" tint so sunlight reads warm
            light.intensity = AmbientIntensity;
        }

        static Tilemap CreateArena(Tile lightTile, Tile darkTile, Material litMaterial)
        {
            var grid = new GameObject("Grid").AddComponent<Grid>();
            grid.cellSize = Vector3.one;

            var groundGo = new GameObject("Ground");
            groundGo.transform.SetParent(grid.transform, false);
            var tilemap = groundGo.AddComponent<Tilemap>();
            var tilemapRenderer = groundGo.AddComponent<TilemapRenderer>();
            if (litMaterial != null) tilemapRenderer.sharedMaterial = litMaterial;

            // Centre the arena on the world origin.
            int xMin = -ArenaWidth / 2;
            int yMin = -ArenaHeight / 2;
            for (int x = 0; x < ArenaWidth; x++)
            for (int y = 0; y < ArenaHeight; y++)
                tilemap.SetTile(new Vector3Int(xMin + x, yMin + y, 0), (x + y) % 2 == 0 ? lightTile : darkTile);

            tilemap.CompressBounds();
            if (tilemap.GetUsedTilesCount() == 0)
                Debug.LogError("Sunlight test scene: no tiles were placed on the arena tilemap.");
            return tilemap;
        }

        static void CreateSunlight(Tilemap arena, Sprite square, Material unlitMaterial)
        {
            var root = new GameObject("Sunlight");
            var manager = root.AddComponent<SunlightManager>();

            var beamGo = new GameObject("SunBeam");
            beamGo.transform.SetParent(root.transform, false);
            var view = beamGo.AddComponent<SunBeamView>();

            var overlayGo = new GameObject("WarningOverlay");
            overlayGo.transform.SetParent(beamGo.transform, false);
            var overlay = overlayGo.AddComponent<SpriteRenderer>();
            overlay.sprite = square;
            overlay.sortingOrder = 10;
            if (unlitMaterial != null) overlay.sharedMaterial = unlitMaterial;

            var lightGo = new GameObject("SunLight2D");
            lightGo.transform.SetParent(beamGo.transform, false);
            var sunLight = lightGo.AddComponent<Light2D>();
            sunLight.lightType = Light2D.LightType.Freeform;
            sunLight.shapeLightFalloffSize = 0.6f;

            SetReference(view, "warningOverlay", overlay);
            SetReference(view, "sunLight", sunLight);
            SetReference(manager, "arena", arena);
            SetReference(manager, "beamView", view);
        }

        static void SetReference(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Sprite GetOrCreateSquareSprite()
        {
            if (!File.Exists(SquareSpritePath))
            {
                Directory.CreateDirectory(SpriteFolder);
                const int size = 32;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
                tex.SetPixels32(pixels);
                File.WriteAllBytes(SquareSpritePath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(SquareSpritePath);

                var importer = (TextureImporter)AssetImporter.GetAtPath(SquareSpritePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = size;   // exactly one world unit
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
        }

        static Tile GetOrCreateTile(string name, Sprite sprite, Color color)
        {
            string path = $"{TileFolder}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile != null) return tile;

            Directory.CreateDirectory(TileFolder);
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = color;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }
    }
}
