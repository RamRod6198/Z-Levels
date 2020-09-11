using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ZLevels
{
    public class PawnRendererScaled
    {
        private RotDrawMode CurRotDrawMode
        {
            get
            {
                if (this.pawn.Dead && this.pawn.Corpse != null)
                {
                    return this.pawn.Corpse.CurRotDrawMode;
                }
                return RotDrawMode.Fresh;
            }
        }

        public PawnRendererScaled(Pawn pawn, PawnDownedWiggler oldWiggler)
        {
            this.pawn = pawn;
            this.wiggler = oldWiggler;
            this.statusOverlays = new PawnHeadOverlays(pawn);
            this.woundOverlays = new PawnWoundDrawer(pawn);
            this.graphics = new PawnGraphicSet(pawn);
        }

        public void RenderPawnAt(Vector3 drawLoc, int curLevel, int baseLevel)
        {
            this.RenderPawnAt(drawLoc, this.CurRotDrawMode, !this.pawn.health.hediffSet.HasHead,
                    this.pawn.IsInvisible(), curLevel, baseLevel);
        }

        public void RenderPawnAt(Vector3 drawLoc, RotDrawMode bodyDrawType, bool headStump, bool invisible
                , int curLevel, int baseLevel)
        {
            if (!this.graphics.AllResolved)
            {
                this.graphics.ResolveAllGraphics();
            }
            if (this.pawn.GetPosture() == PawnPosture.Standing)
            {
                this.RenderPawnInternal(drawLoc, 0f, true, bodyDrawType, headStump, invisible, curLevel, baseLevel);
                if (this.pawn.carryTracker != null)
                {
                    Thing carriedThing = this.pawn.carryTracker.CarriedThing;
                    if (carriedThing != null)
                    {
                        Vector3 vector = drawLoc;
                        bool flag = false;
                        bool flip = false;
                        if (this.pawn.CurJob == null || !this.pawn.jobs.curDriver.ModifyCarriedThingDrawPos(ref vector, ref flag, ref flip))
                        {
                            if (carriedThing is Pawn || carriedThing is Corpse)
                            {
                                vector += new Vector3(0.44f, 0f, 0f);
                            }
                            else
                            {
                                vector += new Vector3(0.18f, 0f, 0.05f);
                            }
                        }
                        if (flag)
                        {
                            vector.y -= 0.03787879f;
                        }
                        else
                        {
                            vector.y += 0.03787879f;
                        }
                        carriedThing.DrawAt(vector, flip);
                    }
                }
                if (!invisible)
                {
                    if (this.pawn.def.race.specialShadowData != null)
                    {
                        if (this.shadowGraphic == null)
                        {
                            this.shadowGraphic = new Graphic_Shadow(this.pawn.def.race.specialShadowData);
                        }
                        this.shadowGraphic.Draw(drawLoc, Rot4.North, this.pawn, 0f);
                    }
                    if (this.graphics.nakedGraphic != null && this.graphics.nakedGraphic.ShadowGraphic != null)
                    {
                        this.graphics.nakedGraphic.ShadowGraphic.Draw(drawLoc, Rot4.North, this.pawn, 0f);
                    }
                }
            }
            else
            {
                float angle = this.BodyAngle();
                Rot4 rot = this.LayingFacing();
                Building_Bed building_Bed = this.pawn.CurrentBed();
                bool renderBody;
                Vector3 rootLoc;
                if (building_Bed != null && this.pawn.RaceProps.Humanlike)
                {
                    renderBody = building_Bed.def.building.bed_showSleeperBody;
                    AltitudeLayer altLayer = (AltitudeLayer)Mathf.Max((int)building_Bed.def.altitudeLayer, 16);
                    Vector3 vector2;
                    Vector3 a = vector2 = this.pawn.Position.ToVector3ShiftedWithAltitude(altLayer);
                    vector2.y += 0.0265151523f;
                    Rot4 rotation = building_Bed.Rotation;
                    rotation.AsInt += 2;
                    float d = -this.BaseHeadOffsetAt(Rot4.South).z;
                    Vector3 a2 = rotation.FacingCell.ToVector3();
                    rootLoc = a + a2 * d;
                    rootLoc.y += 0.007575758f;
                }
                else
                {
                    renderBody = true;
                    rootLoc = drawLoc;
                    if (!this.pawn.Dead && this.pawn.CarriedBy == null)
                    {
                        rootLoc.y = AltitudeLayer.LayingPawn.AltitudeFor() + 0.007575758f;
                    }
                }
                this.RenderPawnInternal(rootLoc, angle, renderBody, rot, rot, bodyDrawType, false, headStump,
                        invisible, curLevel, baseLevel);
            }
            if (this.pawn.Spawned && !this.pawn.Dead)
            {
                this.pawn.stances.StanceTrackerDraw();
                this.pawn.pather.PatherDraw();
            }
        }

        private void RenderPawnInternal(Vector3 rootLoc, float angle, bool renderBody, RotDrawMode draw, bool headStump, bool invisible, int curLevel, int baseLevel)
        {
            this.RenderPawnInternal(rootLoc, angle, renderBody, this.pawn.Rotation, this.pawn.Rotation, draw, false, headStump, invisible, curLevel, baseLevel);
        }

        private void RenderPawnInternal(Vector3 rootLoc, float angle, bool renderBody, Rot4 bodyFacing, Rot4 headFacing, RotDrawMode bodyDrawType, bool portrait
                , bool headStump, bool invisible, int curLevel, int baseLevel)
        {
            if (!this.graphics.AllResolved)
            {
                this.graphics.ResolveAllGraphics();
            }
            float scaledSize = 1.5f * (1f - (((float)(curLevel) - (float)baseLevel) / 5f));

            Quaternion quaternion = Quaternion.AngleAxis(angle, Vector3.up);
            Mesh mesh = null;
            if (renderBody)
            {
                Vector3 loc = rootLoc;
                loc.y += 0.007575758f;
                if (bodyDrawType == RotDrawMode.Dessicated && !this.pawn.RaceProps.Humanlike && this.graphics.dessicatedGraphic != null && !portrait)
                {
                    this.graphics.dessicatedGraphic.Draw(loc, bodyFacing, this.pawn, angle);
                }
                else
                {
                    if (this.pawn.RaceProps.Humanlike)
                    {
                        mesh = new GraphicMeshSet(scaledSize).MeshAt(bodyFacing);
                    }
                    else
                    {
                        Vector2 drawSize = this.graphics.nakedGraphic.drawSize;
                        drawSize.x *= 1f - (((float)(curLevel) - (float)baseLevel) / 5f);
                        drawSize.y *= 1f - (((float)(curLevel) - (float)baseLevel) / 5f);

                        var newGraphic = this.graphics.nakedGraphic.GetCopy(drawSize);
                        mesh = newGraphic.MeshAt(bodyFacing);
                    }
                    List<Material> list = this.graphics.MatsBodyBaseAt(bodyFacing, bodyDrawType);
                    for (int i = 0; i < list.Count; i++)
                    {
                        Material mat = this.OverrideMaterialIfNeeded(list[i], this.pawn);
                        GenDraw.DrawMeshNowOrLater(mesh, loc, quaternion, mat, portrait);
                        loc.y += 0.003787879f;
                    }
                    if (bodyDrawType == RotDrawMode.Fresh)
                    {
                        Vector3 drawLoc = rootLoc;
                        drawLoc.y += 0.0189393945f;
                        this.woundOverlays.RenderOverBody(drawLoc, mesh, quaternion, portrait);
                    }
                }
            }
            Vector3 vector = rootLoc;
            Vector3 a = rootLoc;
            if (bodyFacing != Rot4.North)
            {
                a.y += 0.0265151523f;
                vector.y += 0.0227272734f;
            }
            else
            {
                a.y += 0.0227272734f;
                vector.y += 0.0265151523f;
            }
            if (this.graphics.headGraphic != null)
            {
                Vector3 b = (quaternion * this.BaseHeadOffsetAt(headFacing)) * (1f - (((float)(curLevel) - (float)baseLevel) / 5f));
                Material material = this.graphics.HeadMatAt(headFacing, bodyDrawType, headStump);
                if (material != null)
                {
                    GenDraw.DrawMeshNowOrLater(new GraphicMeshSet(scaledSize).MeshAt(headFacing), a + b, quaternion, material, portrait);
                }
                Vector3 loc2 = rootLoc + b;
                loc2.y += 0.0303030312f;
                bool flag = false;
                if (!portrait || !Prefs.HatsOnlyOnMap)
                {
                    Mesh mesh2 = new GraphicMeshSet(scaledSize).MeshAt(headFacing);
                    List<ApparelGraphicRecord> apparelGraphics = this.graphics.apparelGraphics;
                    for (int j = 0; j < apparelGraphics.Count; j++)
                    {
                        if (apparelGraphics[j].sourceApparel.def.apparel.LastLayer == ApparelLayerDefOf.Overhead)
                        {
                            if (!apparelGraphics[j].sourceApparel.def.apparel.hatRenderedFrontOfFace)
                            {
                                flag = true;
                                Material material2 = apparelGraphics[j].graphic.MatAt(bodyFacing, null);
                                material2 = this.OverrideMaterialIfNeeded(material2, this.pawn);
                                GenDraw.DrawMeshNowOrLater(mesh2, loc2, quaternion, material2, portrait);
                            }
                            else
                            {
                                Material material3 = apparelGraphics[j].graphic.MatAt(bodyFacing, null);
                                material3 = this.OverrideMaterialIfNeeded(material3, this.pawn);
                                Vector3 loc3 = rootLoc + b;
                                loc3.y += ((bodyFacing == Rot4.North) ? 0.003787879f : 0.03409091f);
                                GenDraw.DrawMeshNowOrLater(mesh2, loc3, quaternion, material3, portrait);
                            }
                        }
                    }
                }
                if (!flag && bodyDrawType != RotDrawMode.Dessicated && !headStump)
                {
                    Mesh mesh3 = new GraphicMeshSet(scaledSize).MeshAt(headFacing);
                    Material mat2 = this.graphics.HairMatAt(headFacing);
                    GenDraw.DrawMeshNowOrLater(mesh3, loc2, quaternion, mat2, portrait);
                }
            }
            if (renderBody)
            {
                for (int k = 0; k < this.graphics.apparelGraphics.Count; k++)
                {
                    ApparelGraphicRecord apparelGraphicRecord = this.graphics.apparelGraphics[k];
                    if (apparelGraphicRecord.sourceApparel.def.apparel.LastLayer == ApparelLayerDefOf.Shell)
                    {
                        Material material4 = apparelGraphicRecord.graphic.MatAt(bodyFacing, null);
                        material4 = this.OverrideMaterialIfNeeded(material4, this.pawn);
                        GenDraw.DrawMeshNowOrLater(mesh, vector, quaternion, material4, portrait);
                    }
                }
            }
            if (!portrait && this.pawn.RaceProps.Animal && this.pawn.inventory != null && this.pawn.inventory.innerContainer.Count > 0 && this.graphics.packGraphic != null)
            {
                Graphics.DrawMesh(mesh, vector, quaternion, this.graphics.packGraphic.MatAt(bodyFacing, null), 0);
            }
            if (!portrait)
            {
                this.DrawEquipment(rootLoc);
                if (this.pawn.apparel != null)
                {
                    List<Apparel> wornApparel = this.pawn.apparel.WornApparel;
                    for (int l = 0; l < wornApparel.Count; l++)
                    {
                        wornApparel[l].DrawWornExtras();
                    }
                }
                Vector3 bodyLoc = rootLoc;
                bodyLoc.y += 0.0416666679f;
                this.statusOverlays.RenderStatusOverlays(bodyLoc, quaternion, MeshPool.humanlikeHeadSet.MeshAt(headFacing));
            }
        }

        private void DrawEquipment(Vector3 rootLoc)
        {
            if (this.pawn.Dead || !this.pawn.Spawned)
            {
                return;
            }
            if (this.pawn.equipment == null || this.pawn.equipment.Primary == null)
            {
                return;
            }
            if (this.pawn.CurJob != null && this.pawn.CurJob.def.neverShowWeapon)
            {
                return;
            }
            Stance_Busy stance_Busy = this.pawn.stances.curStance as Stance_Busy;
            if (stance_Busy != null && !stance_Busy.neverAimWeapon && stance_Busy.focusTarg.IsValid)
            {
                Vector3 a;
                if (stance_Busy.focusTarg.HasThing)
                {
                    a = stance_Busy.focusTarg.Thing.DrawPos;
                }
                else
                {
                    a = stance_Busy.focusTarg.Cell.ToVector3Shifted();
                }
                float num = 0f;
                if ((a - this.pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
                {
                    num = (a - this.pawn.DrawPos).AngleFlat();
                }
                Vector3 drawLoc = rootLoc + new Vector3(0f, 0f, 0.4f).RotatedBy(num);
                drawLoc.y += 0.03787879f;
                this.DrawEquipmentAiming(this.pawn.equipment.Primary, drawLoc, num);
                return;
            }
            if (this.CarryWeaponOpenly())
            {
                if (this.pawn.Rotation == Rot4.South)
                {
                    Vector3 drawLoc2 = rootLoc + new Vector3(0f, 0f, -0.22f);
                    drawLoc2.y += 0.03787879f;
                    this.DrawEquipmentAiming(this.pawn.equipment.Primary, drawLoc2, 143f);
                    return;
                }
                if (this.pawn.Rotation == Rot4.North)
                {
                    Vector3 drawLoc3 = rootLoc + new Vector3(0f, 0f, -0.11f);
                    drawLoc3.y += 0f;
                    this.DrawEquipmentAiming(this.pawn.equipment.Primary, drawLoc3, 143f);
                    return;
                }
                if (this.pawn.Rotation == Rot4.East)
                {
                    Vector3 drawLoc4 = rootLoc + new Vector3(0.2f, 0f, -0.22f);
                    drawLoc4.y += 0.03787879f;
                    this.DrawEquipmentAiming(this.pawn.equipment.Primary, drawLoc4, 143f);
                    return;
                }
                if (this.pawn.Rotation == Rot4.West)
                {
                    Vector3 drawLoc5 = rootLoc + new Vector3(-0.2f, 0f, -0.22f);
                    drawLoc5.y += 0.03787879f;
                    this.DrawEquipmentAiming(this.pawn.equipment.Primary, drawLoc5, 217f);
                }
            }
        }

        public void DrawEquipmentAiming(Thing eq, Vector3 drawLoc, float aimAngle)
        {
            float num = aimAngle - 90f;
            Mesh mesh;
            if (aimAngle > 20f && aimAngle < 160f)
            {
                mesh = MeshPool.plane10;
                num += eq.def.equippedAngleOffset;
            }
            else if (aimAngle > 200f && aimAngle < 340f)
            {
                mesh = MeshPool.plane10Flip;
                num -= 180f;
                num -= eq.def.equippedAngleOffset;
            }
            else
            {
                mesh = MeshPool.plane10;
                num += eq.def.equippedAngleOffset;
            }
            num %= 360f;
            Graphic_StackCount graphic_StackCount = eq.Graphic as Graphic_StackCount;
            Material matSingle;
            if (graphic_StackCount != null)
            {
                matSingle = graphic_StackCount.SubGraphicForStackCount(1, eq.def).MatSingle;
            }
            else
            {
                matSingle = eq.Graphic.MatSingle;
            }
            Graphics.DrawMesh(mesh, drawLoc, Quaternion.AngleAxis(num, Vector3.up), matSingle, 0);
        }

        private Material OverrideMaterialIfNeeded(Material original, Pawn pawn)
        {
            Material baseMat = pawn.IsInvisible() ? InvisibilityMatPool.GetInvisibleMat(original) : original;
            return this.graphics.flasher.GetDamagedMat(baseMat);
        }

        private bool CarryWeaponOpenly()
        {
            return (this.pawn.carryTracker == null || this.pawn.carryTracker.CarriedThing == null) && (this.pawn.Drafted || (this.pawn.CurJob != null && this.pawn.CurJob.def.alwaysShowWeapon) || (this.pawn.mindState.duty != null && this.pawn.mindState.duty.def.alwaysShowWeapon));
        }

        public Rot4 LayingFacing()
        {
            if (this.pawn.GetPosture() == PawnPosture.LayingOnGroundFaceUp)
            {
                return Rot4.South;
            }
            if (this.pawn.RaceProps.Humanlike)
            {
                switch (this.pawn.thingIDNumber % 4)
                {
                    case 0:
                        return Rot4.South;
                    case 1:
                        return Rot4.South;
                    case 2:
                        return Rot4.East;
                    case 3:
                        return Rot4.West;
                }
            }
            else
            {
                switch (this.pawn.thingIDNumber % 4)
                {
                    case 0:
                        return Rot4.South;
                    case 1:
                        return Rot4.East;
                    case 2:
                        return Rot4.West;
                    case 3:
                        return Rot4.West;
                }
            }
            return Rot4.Random;
        }

        public float BodyAngle()
        {
            if (this.pawn.GetPosture() == PawnPosture.Standing)
            {
                return 0f;
            }
            Building_Bed building_Bed = this.pawn.CurrentBed();
            if (building_Bed != null && this.pawn.RaceProps.Humanlike)
            {
                Rot4 rotation = building_Bed.Rotation;
                rotation.AsInt += 2;
                return rotation.AsAngle;
            }
            if (this.pawn.Downed || this.pawn.Dead)
            {
                return this.wiggler.downedAngle;
            }
            if (this.pawn.RaceProps.Humanlike)
            {
                return this.LayingFacing().AsAngle;
            }
            Rot4 rot = Rot4.West;
            int num = this.pawn.thingIDNumber % 2;
            if (num != 0)
            {
                if (num == 1)
                {
                    rot = Rot4.East;
                }
            }
            else
            {
                rot = Rot4.West;
            }
            return rot.AsAngle;
        }

        public Vector3 BaseHeadOffsetAt(Rot4 rotation)
        {
            Vector2 headOffset = this.pawn.story.bodyType.headOffset;
            switch (rotation.AsInt)
            {
                case 0:
                    return new Vector3(0f, 0f, headOffset.y);
                case 1:
                    return new Vector3(headOffset.x, 0f, headOffset.y);
                case 2:
                    return new Vector3(0f, 0f, headOffset.y);
                case 3:
                    return new Vector3(-headOffset.x, 0f, headOffset.y);
                default:
                    Log.Error("BaseHeadOffsetAt error in " + this.pawn, false);
                    return Vector3.zero;
            }
        }

        public void Notify_DamageApplied(DamageInfo dam)
        {
            this.graphics.flasher.Notify_DamageApplied(dam);
            this.wiggler.Notify_DamageApplied(dam);
        }

        public void RendererTick()
        {
            this.wiggler.WigglerTick();
        }

        private Pawn pawn;

        public PawnGraphicSet graphics;

        public PawnDownedWiggler wiggler;

        private PawnHeadOverlays statusOverlays;

        private PawnWoundDrawer woundOverlays;

        private Graphic_Shadow shadowGraphic;

        private const float CarriedThingDrawAngle = 16f;

        private const float SubInterval = 0.003787879f;

        private const float YOffset_PrimaryEquipmentUnder = 0f;

        private const float YOffset_Behind = 0.003787879f;

        private const float YOffset_Body = 0.007575758f;

        private const float YOffsetInterval_Clothes = 0.003787879f;

        private const float YOffset_Wounds = 0.0189393945f;

        private const float YOffset_Shell = 0.0227272734f;

        private const float YOffset_Head = 0.0265151523f;

        private const float YOffset_OnHead = 0.0303030312f;

        private const float YOffset_PostHead = 0.03409091f;

        private const float YOffset_CarriedThing = 0.03787879f;

        private const float YOffset_PrimaryEquipmentOver = 0.03787879f;

        private const float YOffset_Status = 0.0416666679f;
    }
}