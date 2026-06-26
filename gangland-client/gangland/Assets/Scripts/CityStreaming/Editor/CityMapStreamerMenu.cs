using Gangland.CityStreaming;
using UnityEditor;
using UnityEngine;

namespace Gangland.CityStreaming.Editor
{
    public static class CityMapStreamerMenu
    {
        [MenuItem("Gangland/City/Add City Map Streamer")]
        public static void AddCityMapStreamer()
        {
            var gameObject = new GameObject("City Map Streamer");
            gameObject.AddComponent<CityMapStreamer>();
            Selection.activeGameObject = gameObject;
            Undo.RegisterCreatedObjectUndo(gameObject, "Add City Map Streamer");
        }
    }
}
