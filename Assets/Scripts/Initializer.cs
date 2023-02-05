using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;

namespace GuneyOzsan
{
    public class Initializer : MonoBehaviour
    {
        [Header("Parameters")]
        [SerializeField] private int landscapeY;
        [SerializeField] private int treePositionX;
        
        [Header("Palette")]
        [SerializeField] private Color earthColor;
        [SerializeField] private Color rootColor;
        [SerializeField] private Color rootTipColor;
        [SerializeField] private Color skyColor;
        [SerializeField] private Color treeColor;
        
        [Header("References")]
        [SerializeField] private RenderTexture world;

        private void Start()
        {
            earthColor = GetRoundingSafeColor(earthColor);
            rootColor = GetRoundingSafeColor(rootColor);
            rootTipColor = GetRoundingSafeColor(rootTipColor);
            skyColor = GetRoundingSafeColor(skyColor);
            treeColor = GetRoundingSafeColor(treeColor);

            StartCoroutine(Initialize());
        }

        private static Color GetRoundingSafeColor(Color color)
        {
            return new Color(
                (int) (color.r * 255) / 255f,
                (int) (color.g * 255) / 255f,
                (int) (color.b * 255) / 255f,
                (int) (color.a * 255) / 255f);
        }

        private IEnumerator Initialize()
        {
            yield return new WaitForEndOfFrame();

            #region Initialize

            Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();

            if (texture == null)
                yield break;

            // Draw the sky.
            for (int i = 0; i < world.width; i++)
            {
                for (int j = landscapeY; j < world.height; j++)
                {
                    texture.SetPixel(i, j, skyColor);
                }
            }

            // Draw the earth.
            for (int i = 0; i < world.width; i++)
            {
                for (int j = 0; j < landscapeY; j++)
                {
                    texture.SetPixel(i, j, earthColor);
                }
            }

            // Draw the tree.
            texture.SetPixel(treePositionX, landscapeY - 1, rootTipColor);

            // Render the texture.
            texture.Apply();
            Graphics.Blit(texture, world);
            Destroy(texture);
            yield return null;
            
            #endregion
            
            while (true)
            {
                yield return new WaitForEndOfFrame();

                #region Update

                texture = ScreenCapture.CaptureScreenshotAsTexture();

                // Update root.
                int previousRootTipX = 0;
                int previousRootY = 0;
                int nextRootTipX = 0;
                int nextRootTipY = 0;
                
                for (int i = 0; i < world.width; i++)
                {
                    for (int j = 0; j < world.height; j++)
                    {
                        if (texture.GetPixel(i, j) == rootTipColor)
                        {
                            Debug.Log("Root tip found." + i + " " + j);
                            previousRootTipX = i;
                            previousRootY = j;
                            nextRootTipX = Mathf.Clamp(i + Random.Range(0, 3) - 1, 0, world.width - 1);
                            nextRootTipY = Mathf.Clamp(j + Random.Range(0, 3) - 1, 0, world.height - 1);
                            Debug.Log("Root tip will move to." + nextRootTipX + " " + nextRootTipY);
                        }
                    }
                }
                
                if (texture.GetPixel(nextRootTipX, nextRootTipY) == earthColor)
                {
                    Debug.Log("Root tip hit the earth.");
                    texture.SetPixel(previousRootTipX, previousRootY, rootColor);
                    texture.SetPixel(nextRootTipX, nextRootTipY, rootTipColor);
                }
                
                #endregion
                
                texture.Apply();
                Graphics.Blit(texture, world);
                Destroy(texture);
                
                if (nextRootTipY == 0)
                    break;
            }
        }
    }
}