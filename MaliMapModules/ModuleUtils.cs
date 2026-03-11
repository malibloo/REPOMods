using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MaliMapModules
{
    internal static class ModuleUtils
    {
        internal static bool MapReady => LevelGenerator.Instance.Generated;

        internal static void SetMarkerVisibility(MapCustomEntity m, bool show)
        {
            if (m == null || m.spriteRenderer == null) return;

            bool alive = m.Parent != null && m.Parent.gameObject.activeInHierarchy;

            m.spriteRenderer.enabled = show && alive;
        }

        internal static void RegisterMarkerIfMine(HashSet<MapCustomEntity> markers, MapCustomEntity ent, string name,  bool show)
        {
            if (ent == null) return;
            markers.Add(ent);
            ent.gameObject.name = $"[MMM] {name}";
            SetMarkerVisibility(ent, show);
        }

        internal static MapCustomEntity? TryCreateMarker(GameObject go, Sprite? sprite, Color color)
        {
            // Find MapCustom
            var mc = go.GetComponent<MapCustom>();
            // If none is found, add one
            if (mc == null)
            {
                mc = go.AddComponent<MapCustom>();
                mc.sprite = sprite;
                mc.color = color;
                mc.enabled = false; // Disable to prevent Start() to avoid duplicates
            }
            // Either MapCustom exists and has no marker or it was just added. Create marker if missing.
            if (mc.mapCustomEntity == null)
                Map.Instance.AddCustom(mc, sprite, mc.color);

            return mc.mapCustomEntity;
        }

        // Load in sprite from local file if it exists, otherwise fall back to the default sprite from the game assets.
        internal static Sprite? GetSprite(string? filePath = null)
        {
            if (filePath != null)
            {
                var path = Path.Combine(Path.GetDirectoryName(MaliMapModules.Instance.Info.Location) ?? "", filePath);
                if (File.Exists(path))
                {
                    var tex = new Texture2D(2, 2);
                    ImageConversion.LoadImage(tex, File.ReadAllBytes(path));
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
                else
                    MaliMapModules.Logger.LogInfo($"Sprite ({filePath}) not found, using default sprite.");
                
            }
            var mv = Map.Instance.ValuableObject?.GetComponent<MapValuable>();
            if (mv != null)
                return mv.spriteBig;
            return null;
        }

        internal static Color? ParseColor(string hex)
        {
            string h = hex.Trim();
            if (!h.StartsWith("#")) return null;

            try
            {
                byte r, g, b, a;
                r = Convert.ToByte(h[1..3], 16);
                g = Convert.ToByte(h[3..5], 16);
                b = Convert.ToByte(h[5..7], 16);
                a = (h.Length == 9) ? Convert.ToByte(h[7..9], 16) : (byte)255;
                return new Color32(r, g, b, a);
            }
            catch
            {
                return null;
            }
        }
    }
}
