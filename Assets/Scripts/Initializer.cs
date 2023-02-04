using System.Collections;
using UnityEngine;

namespace GuneyOzsan
{
    public class Initializer : MonoBehaviour
    {
        [Header("Parameters")]
        [SerializeField] private int landscapeY;
        [SerializeField] private int treePositionX;
        
        [Header("Palette")]
        [SerializeField] private Color worldColor;
        [SerializeField] private Color treeColor;
        [SerializeField] private Color rootColor;
        [SerializeField] private Color rootTipColor;
        [SerializeField] private Color landscapeColor;
        
        [Header("References")]
        [SerializeField] private RenderTexture world;

        private void Start()
        {
            StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            yield return new WaitForEndOfFrame();

            #region Initialize

            Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();

            if (texture == null)
                yield break;

            // Reset the texture.
            for (int j = 0; j < world.width; j++)
            {
                for (int i = 0; i < world.height; i++)
                {
                    texture.SetPixel(i, j, Color.white);
                }
            }

            // Draw the landscape.
            for (int i = 0; i < world.width; i++)
            {
                for (int j = 0; j < landscapeY; j++)
                {
                    texture.SetPixel(i, j, landscapeColor);
                }
            }

            // Draw the tree.
            texture.SetPixel(treePositionX, landscapeY - 1, rootColor);

            #endregion
            
            texture.Apply();
            Graphics.Blit(texture, world);
            Destroy(texture);
        }
    }
}