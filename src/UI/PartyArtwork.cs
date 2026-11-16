using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace KingdomPartyNode
{
    /// <summary>Loads, crops, and disposes the embedded Party Meetup sprite assets.</summary>
    internal static class PartyArtwork
    {
        internal const string IconId = "PartyMeetup_Custom";
        internal const string RoomIconId = "PartyMeetupRoom_Custom";

        private static Texture2D texture;
        private static Texture2D roomTexture;
        private static Sprite sprite;
        private static Sprite roomSprite;

        internal static Sprite Sprite => sprite ? sprite : (sprite = Load("party-meetup-v1.png", IconId, out texture));
        internal static Sprite RoomSprite => roomSprite ? roomSprite : (roomSprite = Load("party-meetup-empty-v2.png", RoomIconId, out roomTexture));

        private static Sprite Load(string resource, string name, out Texture2D loadedTexture)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
            using (MemoryStream memoryStream = new MemoryStream())
            {
                if (stream == null)
                    throw new InvalidOperationException("Missing Party Meetup artwork.");

                stream.CopyTo(memoryStream);
                loadedTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(loadedTexture, memoryStream.ToArray()))
                    throw new InvalidDataException("Invalid Party Meetup artwork.");
            }

            loadedTexture.name = name;
            loadedTexture.filterMode = FilterMode.Point;
            loadedTexture.wrapMode = TextureWrapMode.Clamp;

            Rect bounds = FindOpaqueBounds(loadedTexture);
            Sprite created = Sprite.Create(loadedTexture, bounds, new Vector2(0.5f, 0.5f), 100f);
            created.name = name;
            return created;
        }

        private static Rect FindOpaqueBounds(Texture2D loadedTexture)
        {
            Color32[] pixels = loadedTexture.GetPixels32();
            int width = loadedTexture.width;
            int height = loadedTexture.height;
            int minX = width;
            int minY = height;
            int maxX = -1;
            int maxY = -1;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (pixels[y * width + x].a < 16)
                        continue;

                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                }
            }

            if (maxX < minX)
                throw new InvalidDataException("Party Meetup artwork is empty.");

            return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        internal static void Dispose()
        {
            if (sprite) UnityEngine.Object.Destroy(sprite);
            if (texture) UnityEngine.Object.Destroy(texture);
            if (roomSprite) UnityEngine.Object.Destroy(roomSprite);
            if (roomTexture) UnityEngine.Object.Destroy(roomTexture);
            sprite = null;
            texture = null;
            roomSprite = null;
            roomTexture = null;
        }
    }
}
