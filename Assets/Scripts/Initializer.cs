using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GuneyOzsan
{
    public class Initializer : MonoBehaviour
    {
        [Header("Parameters")]
        [SerializeField] private int landscapeY;
        [SerializeField] private int treePositionX;
        [SerializeField] private float rootSplitProbability;
        
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
                            if (texture.GetPixel(x, y + 1) != earthColor &&
                                texture.GetPixel(x, y - 1) != earthColor && 
                                texture.GetPixel(x + 1, y) != earthColor && 
                                texture.GetPixel(x - 1, y) != earthColor)
                            {
                                // Root tip is dead.
                                setPixelQueue.Add((x, y, rootColor));
                            }
                            else
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
                            
                                int xNext = x + deltaX;
                                int yNext = y + deltaY;
                            
                                Color targetColor = texture.GetPixel(xNext, yNext);
                            
                                if (targetColor == borderColor)
                                {
                                    setPixelQueue.Add((x, y, rootColor));
                                }
                                else
                                {
                                    if (targetColor == earthColor)
                                    {
                                        setPixelQueue.Add((x, y, rootColor));
                                        setPixelQueue.Add((xNext, yNext, rootTipColor));
                                    }
                                
                                    bool split = Random.Range(0f, 1f) < rootSplitProbability;

                                    if (split)
                                    {
                                        var splitTargets = new List<(int x, int y)>();
                                        if (texture.GetPixel(x + 1, y) == earthColor &&
                                            xNext != x + 1 &&
                                            yNext != y)
                                        {
                                            splitTargets.Add((x + 1, y));
                                        }
                                        if (texture.GetPixel(x - 1, y) == earthColor &&
                                            xNext != x - 1 &&
                                            yNext != y)
                                        {
                                            splitTargets.Add((x - 1, y));
                                        }
                                        if (texture.GetPixel(x, y + 1) == earthColor &&
                                            xNext != x &&
                                            yNext != y + 1)
                                        {
                                            splitTargets.Add((x, y + 1));
                                        }
                                        if (texture.GetPixel(x, y - 1) == earthColor &&
                                            xNext != x &&
                                            yNext != y - 1)
                                        {
                                            splitTargets.Add((x, y - 1));
                                        }
                                    
                                        if (splitTargets.Count > 0)
                                        {
                                            (int x, int y) target = splitTargets[Random.Range(0, splitTargets.Count)];
                                            setPixelQueue.Add((target.x, target.y, rootTipColor));
                                        }
                                    }
                                }
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