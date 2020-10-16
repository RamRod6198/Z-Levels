using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ZLevels
{
    internal class SectionLayer_LowerTerrain : SectionLayer
    {
        public override bool Visible
        {
            get
            {
                return DebugViewSettings.drawTerrain;
            }
        }

        public SectionLayer_LowerTerrain(Section section) : base(section)
        {
            this.relevantChangeTypes = MapMeshFlag.Terrain;
        }

        public virtual Material GetMaterialFor(TerrainDef terrain)
        {
            return terrain.DrawMatSingle;
        }

        public bool AllowRenderingFor(TerrainDef terrain)
        {
            return DebugViewSettings.drawTerrainWater || !terrain.HasTag("Water");
        }

        public override void Regenerate()
        {
            try
            {
                var ZTracker = ZUtils.ZTracker;
                int curLevel = ZTracker.GetZIndexFor(base.Map);
                base.ClearSubMeshes(MeshParts.All);
                foreach (var map in ZTracker.GetAllMaps(base.Map.Tile)
                    .OrderByDescending(x => ZTracker.GetZIndexFor(x)))
                {
                    int baseLevel = ZTracker.GetZIndexFor(map);
                    if (curLevel > baseLevel && baseLevel >= 0)
                    {
                        TerrainGrid terrainGrid = map.terrainGrid;
                        CellRect cellRect = this.section.CellRect;
                        TerrainDef[] array = new TerrainDef[8];
                        HashSet<TerrainDef> hashSet = new HashSet<TerrainDef>();
                        bool[] array2 = new bool[8];
                        foreach (IntVec3 intVec in cellRect)
                        {
                            if (base.Map.terrainGrid.TerrainAt(intVec) == ZLevelsDefOf.ZL_OutsideTerrain)
                            {
                                hashSet.Clear();
                                float terrainLevelDiff = ((float)(curLevel - baseLevel)) / 2f;
                                TerrainDef terrainDef = terrainGrid.TerrainAt(intVec);
                                LayerSubMesh subMesh = base.GetSubMesh(this.GetMaterialFor(terrainDef));
                                if (subMesh != null && this.AllowRenderingFor(terrainDef))
                                {
                                    int count = subMesh.verts.Count;
                                    subMesh.verts.Add(new Vector3((float)intVec.x, 0f, (float)intVec.z - terrainLevelDiff));
                                    subMesh.verts.Add(new Vector3((float)intVec.x, 0f, (float)(intVec.z + 1 - terrainLevelDiff)));
                                    subMesh.verts.Add(new Vector3((float)(intVec.x + 1), 0f, (float)(intVec.z + 1 - terrainLevelDiff)));
                                    subMesh.verts.Add(new Vector3((float)(intVec.x + 1), 0f, (float)intVec.z - terrainLevelDiff));
                                    subMesh.colors.Add(SectionLayer_LowerTerrain.ColorWhite);
                                    subMesh.colors.Add(SectionLayer_LowerTerrain.ColorWhite);
                                    subMesh.colors.Add(SectionLayer_LowerTerrain.ColorWhite);
                                    subMesh.colors.Add(SectionLayer_LowerTerrain.ColorWhite);
                                    subMesh.tris.Add(count);
                                    subMesh.tris.Add(count + 1);
                                    subMesh.tris.Add(count + 2);
                                    subMesh.tris.Add(count);
                                    subMesh.tris.Add(count + 2);
                                    subMesh.tris.Add(count + 3);
                                }

                                for (int i = 0; i < 8; i++)
                                {
                                    IntVec3 c = intVec + GenAdj.AdjacentCellsAroundBottom[i];
                                    if (!c.InBounds(map))
                                    {
                                        array[i] = terrainDef;
                                    }
                                    else
                                    {
                                        TerrainDef terrainDef2 = terrainGrid.TerrainAt(c);
                                        Thing edifice = c.GetEdifice(map);
                                        if (edifice != null && edifice.def.coversFloor)
                                        {
                                            terrainDef2 = TerrainDefOf.Underwall;
                                        }
                                        array[i] = terrainDef2;
                                        if (terrainDef2 != terrainDef && terrainDef2.edgeType
                                            != TerrainDef.TerrainEdgeType.Hard
                                            && terrainDef2.renderPrecedence >= terrainDef.renderPrecedence
                                            && !hashSet.Contains(terrainDef2))
                                        {
                                            hashSet.Add(terrainDef2);
                                        }
                                    }
                                }
                                foreach (TerrainDef terrainDef3 in hashSet)
                                {
                                    LayerSubMesh subMesh2 = base.GetSubMesh(this.GetMaterialFor(terrainDef3));
                                    if (subMesh2 != null && this.AllowRenderingFor(terrainDef3))
                                    {
                                        int count = subMesh2.verts.Count;
                                        subMesh2.verts.Add(new Vector3((float)intVec.x + 0.5f, 0f, (float)intVec.z - terrainLevelDiff));
                                        subMesh2.verts.Add(new Vector3((float)intVec.x, 0f, (float)intVec.z - terrainLevelDiff));
                                        subMesh2.verts.Add(new Vector3((float)intVec.x, 0f, (float)(intVec.z + 0.5f) - terrainLevelDiff));
                                        subMesh2.verts.Add(new Vector3((float)intVec.x, 0f, (float)(intVec.z + 1) - terrainLevelDiff));
                                        subMesh2.verts.Add(new Vector3((float)intVec.x + 0.5f, 0f, (float)(intVec.z + 1) - terrainLevelDiff));
                                        subMesh2.verts.Add(new Vector3((float)(intVec.x + 1), 0f, (float)(intVec.z + 1) - terrainLevelDiff));
                                        subMesh2.verts.Add(new Vector3((float)(intVec.x + 1), 0f, (float)(intVec.z + 0.5f) - terrainLevelDiff));
                                        subMesh2.verts.Add(new Vector3((float)(intVec.x + 1), 0f, (float)intVec.z - terrainLevelDiff));
                                        subMesh2.verts.Add(new Vector3((float)intVec.x + 0.5f, 0f, (float)(intVec.z + 0.5f) - terrainLevelDiff));
                                        for (int j = 0; j < 8; j++)
                                        {
                                            array2[j] = false;
                                        }
                                        for (int k = 0; k < 8; k++)
                                        {
                                            if (k % 2 == 0)
                                            {
                                                if (array[k] == terrainDef3)
                                                {
                                                    array2[(k - 1 + 8) % 8] = true;
                                                    array2[k] = true;
                                                    array2[(k + 1) % 8] = true;
                                                }
                                            }
                                            else if (array[k] == terrainDef3)
                                            {
                                                array2[k] = true;
                                            }
                                        }
                                        for (int l = 0; l < 8; l++)
                                        {
                                            if (array2[l])
                                            {
                                                subMesh2.colors.Add(SectionLayer_LowerTerrain.ColorWhite);
                                            }
                                            else
                                            {
                                                subMesh2.colors.Add(SectionLayer_LowerTerrain.ColorClear);
                                            }
                                        }
                                        subMesh2.colors.Add(SectionLayer_LowerTerrain.ColorClear);
                                        for (int m = 0; m < 8; m++)
                                        {
                                            subMesh2.tris.Add(count + m);
                                            subMesh2.tris.Add(count + (m + 1) % 8);
                                            subMesh2.tris.Add(count + 8);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                base.FinalizeMesh(MeshParts.All);
            }
            catch { };

        }

        private static readonly Color32 ColorWhite = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

        private static readonly Color32 ColorClear = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, 0);
    }
}

