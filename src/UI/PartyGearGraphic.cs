using UnityEngine;
using UnityEngine.UI;

namespace KingdomPartyNode
{
    /// <summary>Pixel-art 16x16 gear icon rendered directly into a UI VertexHelper.</summary>
    public sealed class PartyGearGraphic : MaskableGraphic
    {
        private static readonly string[] Pixels =
        {
            "0000001111000000",
            "0000001111000000",
            "0011001111001100",
            "0011111111111100",
            "0001111111111000",
            "0001111001111000",
            "1111110000111111",
            "1111100000011111",
            "1111100000011111",
            "1111110000111111",
            "0001111001111000",
            "0001111111111000",
            "0011111111111100",
            "0011001111001100",
            "0000001111000000",
            "0000001111000000"
        };

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect rect = rectTransform.rect;
            float cellWidth = rect.width / 16f;
            float cellHeight = rect.height / 16f;
            Color32 vertexColor = color;

            for (int row = 0; row < 16; row++)
            {
                for (int col = 0; col < 16; col++)
                {
                    if (Pixels[row][col] != '1')
                        continue;

                    int startVertex = helper.currentVertCount;
                    float left = rect.xMin + col * cellWidth;
                    float top = rect.yMax - row * cellHeight;
                    helper.AddVert(new Vector3(left, top), vertexColor, Vector4.zero);
                    helper.AddVert(new Vector3(left + cellWidth, top), vertexColor, Vector4.zero);
                    helper.AddVert(new Vector3(left + cellWidth, top - cellHeight), vertexColor, Vector4.zero);
                    helper.AddVert(new Vector3(left, top - cellHeight), vertexColor, Vector4.zero);
                    helper.AddTriangle(startVertex, startVertex + 1, startVertex + 2);
                    helper.AddTriangle(startVertex, startVertex + 2, startVertex + 3);
                }
            }
        }
    }
}
