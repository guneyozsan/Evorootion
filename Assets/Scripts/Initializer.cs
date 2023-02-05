using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace GuneyOzsan
{
    public class Initializer : MonoBehaviour
    {
        [Header("Parameters")]
        [SerializeField] private int landscapeY;
        [SerializeField] private int treePositionX;
        
        [Header("Palette")]
        [SerializeField] private Color borderColor;
        [SerializeField] private Color earthColor;
        [SerializeField] private Color rootColor;
        [SerializeField] private Color rootTipColor;
        [SerializeField] private Color skyColor;
        [SerializeField] private Color treeColor;
        
        [Header("References")]
        [SerializeField] private RenderTexture world;

        private void Start()
        {
            borderColor = GetRoundingSafeColor(borderColor);
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
            
            // Draw the borders.
            for (int i = 0; i < world.width; i++)
            {
                texture.SetPixel(i, 0, borderColor);
                texture.SetPixel(i, world.height - 1, borderColor);
            }
            
            for (int i = 0; i < world.height; i++)
            {
                texture.SetPixel(0, i, borderColor);
                texture.SetPixel(world.width - 1, i, borderColor);
            }

            // Draw the tree.
            texture.SetPixel(treePositionX -1, landscapeY - 1, rootTipColor);
            texture.SetPixel(treePositionX, landscapeY - 1, rootTipColor);
            texture.SetPixel(treePositionX + 1, landscapeY - 1, rootTipColor);

            // Render the texture.
            texture.Apply();
            Graphics.Blit(texture, world);
            Destroy(texture);
            yield return null;
            
            #endregion
            
            var setPixelQueue = new List<(int x, int y, Color color)>(); 
            
            while (true)
            {
                setPixelQueue.Clear();
                yield return new WaitForEndOfFrame();

                #region Root Growth

                texture = ScreenCapture.CaptureScreenshotAsTexture();

                // Update root.

                for (int x = 0; x < world.width; x++)
                {
                    for (int y = 0; y < world.height; y++)
                    {
                        if (texture.GetPixel(x, y) == rootTipColor)
                        {
                            int direction = Random.Range(0, 4);
                            int deltaX;
                            int deltaY;
                            
                            switch (direction)
                            {
                                case 0:
                                    deltaX = 1;
                                    deltaY = 0;
                                    break;
                                case 1:
                                    deltaX = 0;
                                    deltaY = 0;
                                    break;
                                case 2:
                                    deltaX = -1;
                                    deltaY = 0;
                                    break;
                                case 3:
                                    deltaX = 0;
                                    deltaY = -1;
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }
                            
                            int nextRootTipX = x + deltaX;
                            int nextRootTipY = y + deltaY;
                            
                            Color nextColor = texture.GetPixel(nextRootTipX, nextRootTipY);
                            
                            if (nextColor == earthColor)
                            {
                                setPixelQueue.Add((x, y, rootColor));
                                setPixelQueue.Add((nextRootTipX, nextRootTipY, rootTipColor));
                            }
                            else if (nextColor == borderColor)
                            {
                                setPixelQueue.Add((x, y, rootColor));
                            }
                        }
                    }
                }

                #endregion

                foreach ((int x, int y, Color color) parameters in setPixelQueue)
                {
                    texture.SetPixel(parameters.x, parameters.y, parameters.color);
                }
                
                texture.Apply();
                Graphics.Blit(texture, world);
                Destroy(texture);
            }
        }
    }
}