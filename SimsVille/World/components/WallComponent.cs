﻿/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.world.Model;
using Microsoft.Xna.Framework;
using TSO.Content.model;
using TSO.Content;
using tso.world.Utils;
using TSO.Files.formats.iff.chunks;
using Microsoft.Xna.Framework.Graphics;
using TSO.Common.utils;
using tso.common.utils;

namespace tso.world.Components
{
    //a mega fun component that draws all walls for you!!
    public class WallComponent : WorldComponent
    {

        public override float PreferredDrawOrder
        {
            get
            {
                return 800.0f;
            }
        }

        public Blueprint blueprint;
        private JunctionFlags[] UpJunctions;
        private JunctionFlags[] DownJunctions;
        private WallCuts[] Cuts;

        private static int NEAR_YOFF = 1;
        private static int MED_YOFF = 1;
        private static int FAR_YOFF = 1;

        //first is top left, second is top right, third is diag horiz, fourth is diag vert
        private static Rectangle[] DESTINATION_NEAR = new Rectangle[] {
            new Rectangle(4, 76+NEAR_YOFF, 64, 271),
            new Rectangle(68, 76+NEAR_YOFF, 64, 271),
            new Rectangle(4, 108+NEAR_YOFF, 128, 240),
            new Rectangle(60, 144+NEAR_YOFF, 16, 232)
        };
        private static Rectangle[] DESTINATION_MED = new Rectangle[] {
            new Rectangle(2, 38+MED_YOFF, 32, 135),
            new Rectangle(34, 38+MED_YOFF, 32, 135),
            new Rectangle(2, 54+MED_YOFF, 64, 120),
            new Rectangle(30, 72+MED_YOFF, 8, 116)
        };
        private static Rectangle[] DESTINATION_FAR = new Rectangle[] {
            new Rectangle(1, 19+FAR_YOFF, 16, 67),
            new Rectangle(17, 19+FAR_YOFF, 16, 67),
            new Rectangle(1, 27+FAR_YOFF, 32, 60),
            new Rectangle(15, 36+FAR_YOFF, 4, 58)
        };

        private static Rectangle JUNCDEST_NEAR = new Rectangle(4, 316 + NEAR_YOFF, 128, 64);
        private static Rectangle JUNCDEST_MED = new Rectangle(2, 158 + MED_YOFF, 64, 32);
        private static Rectangle JUNCDEST_FAR = new Rectangle(1, 79 + FAR_YOFF, 32, 16);

        private static Rectangle FLRDEST_NEAR = new Rectangle(5, 316, 127, 64);
        private static Rectangle FLRDEST_MED = new Rectangle(3, 158, 63, 32);
        private static Rectangle FLRDEST_FAR = new Rectangle(2, 79, 31, 16);

        private static Dictionary<JunctionFlags, int> JunctionMap = new Dictionary<JunctionFlags, int>()
        {
            {JunctionFlags.TopRight, 0},
            {JunctionFlags.TopLeft, 1},
            {JunctionFlags.TopLeft | JunctionFlags.TopRight, 2},
            {JunctionFlags.BottomLeft, 3},
            {JunctionFlags.BottomLeft | JunctionFlags.TopRight, 4},
            {JunctionFlags.BottomLeft | JunctionFlags.TopLeft, 5},
            {JunctionFlags.BottomLeft | JunctionFlags.TopLeft | JunctionFlags.TopRight, 6},
            {JunctionFlags.BottomRight, 7},
            {JunctionFlags.BottomRight | JunctionFlags.TopRight, 8},
            {JunctionFlags.BottomRight | JunctionFlags.TopLeft, 9},
            {JunctionFlags.BottomRight | JunctionFlags.TopRight | JunctionFlags.TopLeft, 10},
            {JunctionFlags.BottomRight | JunctionFlags.BottomLeft, 11},
            {JunctionFlags.BottomRight | JunctionFlags.BottomLeft | JunctionFlags.TopRight, 12},
            {JunctionFlags.BottomRight | JunctionFlags.BottomLeft | JunctionFlags.TopLeft, 13},
            {JunctionFlags.AllNonDiag, 14},
            {JunctionFlags.DiagTop, 15},
            {JunctionFlags.DiagTop | JunctionFlags.BottomLeft, 16},
            {JunctionFlags.DiagTop | JunctionFlags.BottomRight, 17},
            {JunctionFlags.DiagTop | JunctionFlags.BottomLeft | JunctionFlags.BottomRight, 18},
            {JunctionFlags.DiagLeft, 19},
            {JunctionFlags.DiagLeft | JunctionFlags.TopRight, 20},
            {JunctionFlags.DiagLeft | JunctionFlags.BottomRight, 21},
            {JunctionFlags.DiagLeft | JunctionFlags.BottomRight | JunctionFlags.TopRight, 22},
            {JunctionFlags.DiagLeft | JunctionFlags.DiagTop, 23},
            {JunctionFlags.DiagLeft | JunctionFlags.DiagTop | JunctionFlags.BottomRight, 24},
            {JunctionFlags.DiagBottom, 25},
            {JunctionFlags.DiagBottom | JunctionFlags.TopRight, 26},
            {JunctionFlags.DiagBottom | JunctionFlags.TopLeft, 27},
            {JunctionFlags.DiagBottom | JunctionFlags.TopLeft | JunctionFlags.TopRight, 28},
            {JunctionFlags.DiagBottom | JunctionFlags.DiagTop, 29},
            {JunctionFlags.DiagBottom | JunctionFlags.DiagLeft, 30},
            {JunctionFlags.DiagBottom | JunctionFlags.DiagLeft | JunctionFlags.TopRight, 31},
            {JunctionFlags.DiagBottom | JunctionFlags.DiagLeft | JunctionFlags.DiagTop, 32},
            {JunctionFlags.DiagRight, 33},
            {JunctionFlags.DiagRight | JunctionFlags.TopLeft, 34},
            {JunctionFlags.DiagRight | JunctionFlags.BottomLeft, 35},
            {JunctionFlags.DiagRight | JunctionFlags.TopLeft | JunctionFlags.BottomLeft, 36},
            {JunctionFlags.DiagRight | JunctionFlags.DiagTop, 37},
            {JunctionFlags.DiagRight | JunctionFlags.DiagTop | JunctionFlags.BottomLeft, 38},
            {JunctionFlags.DiagRight | JunctionFlags.DiagLeft, 39},
            {JunctionFlags.DiagRight | JunctionFlags.DiagLeft | JunctionFlags.DiagTop, 40},
            {JunctionFlags.DiagRight | JunctionFlags.DiagBottom, 41},
            {JunctionFlags.DiagRight | JunctionFlags.DiagBottom | JunctionFlags.TopLeft, 42},
            {JunctionFlags.DiagRight | JunctionFlags.DiagBottom | JunctionFlags.DiagTop, 43},
            {JunctionFlags.DiagRight | JunctionFlags.DiagBottom | JunctionFlags.DiagLeft, 44},
            {JunctionFlags.AllDiag, 45}
        };

        private Texture2D[] WallZBuffers;
        public Dictionary<ushort, Wall> WallCache = new Dictionary<ushort,Wall>();
        public Dictionary<ushort, WallStyle> WallStyleCache = new Dictionary<ushort,WallStyle>();

        private uint TileRoom;

        public override void Draw(GraphicsDevice device, WorldState world)
        {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            if (WallZBuffers == null) WallZBuffers = TextureGenerator.GetWallZBuffer(device);

            var pxOffset = world.WorldSpace.GetScreenOffset();
            var wallContent = Content.Get().WorldWalls;
            var floorContent = Content.Get().WorldFloors;

            //draw walls
            for (sbyte level = 1; level <= world.Level; level++)
            {
                int off = 0;
                bool canCut = !(level < world.Level);
                GenerateWallData(blueprint.Walls[level-1], blueprint.WallsAt[level-1], canCut);
                var rMap = blueprint.RoomMap[level - 1];
                for (short y = 0; y < blueprint.Height; y++)
                { //ill decide on a reasonable system for components when it's finished ok pls :(
                    for (short x = 0; x < blueprint.Height; x++)
                    {
                        TileRoom = (rMap == null)?1:rMap[x + y * blueprint.Width];
                        var comp = blueprint.GetWall(x, y, level);
                        if (comp.Segments != 0)
                        {
                            comp = RotateWall(world.Rotation, comp, x, y, level);
                            var tilePosition = new Vector3(x, y, (level-1) * 2.95f);
                            world._2D.OffsetPixel(world.WorldSpace.GetScreenFromTile(tilePosition));
                            world._2D.OffsetTile(tilePosition);
                            var myCuts = Cuts[off];
                            var cDown = canCut && WallsDownAt(x, y);

                            if ((byte)comp.Segments < 16) myCuts = RotateCuts(world.Rotation, myCuts, x, y);
                            else if ((int)world.Rotation > 1) //for diagonals, just flip cuts on two furthest rotations
                            {
                                if (myCuts.TLCut == WallCut.DownRightUpLeft) myCuts.TLCut = WallCut.DownLeftUpRight;
                                else if (myCuts.TLCut == WallCut.DownLeftUpRight) myCuts.TLCut = WallCut.DownRightUpLeft;
                                if (myCuts.TRCut == WallCut.DownRightUpLeft) myCuts.TRCut = WallCut.DownLeftUpRight;
                                else if (myCuts.TRCut == WallCut.DownLeftUpRight) myCuts.TRCut = WallCut.DownRightUpLeft;
                            }

                            if ((comp.Segments & WallSegments.TopLeft) == WallSegments.TopLeft && !(myCuts.TLCut > 0 && comp.TopLeftDoor))
                            {
                                //draw top left and top right relative to this rotation
                                var tlPattern = GetPattern(comp.TopLeftPattern);

                                ushort styleID = 1;
                                ushort overlayID = 0;
                                bool down = false;

                                switch (myCuts.TLCut)
                                {
                                    case WallCut.Down:
                                        styleID = comp.TopLeftStyle;
                                        down = true; break;
                                    case WallCut.DownLeftUpRight:
                                        styleID = 7;
                                        overlayID = 252; break;
                                    case WallCut.DownRightUpLeft:
                                        styleID = 8;
                                        overlayID = 253; break;
                                    default:
                                        if (comp.TopLeftStyle != 1) styleID = comp.TopLeftStyle;
                                        else if (comp.ObjSetTLStyle != 0) styleID = comp.ObjSetTLStyle; //use custom style set by object if no cut
                                        else styleID = 1;
                                        break;
                                }

                                var tlStyle = GetStyle(styleID);

                                var _Sprite = GetWallSprite(tlPattern, tlStyle, 0, down, world);
                                if (_Sprite.Pixel != null)
                                {
                                    world._2D.Draw(_Sprite);

                                    //draw overlay if exists

                                    if (overlayID != 0) world._2D.Draw(GetWallSprite(GetPattern(overlayID), null, 0, down, world));

                                    var contOff = tilePosition + RotateOffset(world.Rotation, new Vector3(0, -1, 0));

                                    if (comp.TopLeftThick)
                                    {
                                        //check far side of wall for continuation. if there is none, round this part off
                                        if (!comp.TopRightThick && OffsetValid(contOff, blueprint))
                                        {
                                            var comp2 = RotateWall(world.Rotation, blueprint.GetWall((short)(contOff.X), (short)(contOff.Y), level), (short)(contOff.X), (short)(contOff.Y), level);
                                            if (!comp2.TopLeftThick)
                                            {
                                                _Sprite = CopySprite(_Sprite);
                                                tlStyle = GetStyle(comp.TopLeftStyle); //return to normal if cutaway
                                                var tilePosition2 = contOff;

                                                world._2D.OffsetPixel(world.WorldSpace.GetScreenFromTile(tilePosition2));
                                                world._2D.OffsetTile(tilePosition2);
                                                int newWidth = 0;
                                                bool downAtCont = canCut && WallsDownAt((short)(contOff.X), (short)(contOff.Y));

                                                SPR mask = null;
                                                switch (world.Zoom)
                                                {
                                                    case WorldZoom.Far:
                                                        newWidth = 3;
                                                        mask = downAtCont ? tlStyle.WallsDownFar : tlStyle.WallsUpFar;
                                                        break;
                                                    case WorldZoom.Medium:
                                                        newWidth = 6;
                                                        mask = downAtCont ? tlStyle.WallsDownMedium : tlStyle.WallsUpMedium;
                                                        break;
                                                    case WorldZoom.Near:
                                                        newWidth = 12;
                                                        mask = downAtCont ? tlStyle.WallsDownNear : tlStyle.WallsUpNear;
                                                        break;
                                                }
                                                if (mask != null) _Sprite.Mask = world._2D.GetTexture(mask.Frames[0]);
                                                if (x > 0 && (blueprint.GetWall((short)(x - 1), (short)(y - 1), level).Segments & WallSegments.VerticalDiag) == WallSegments.VerticalDiag) newWidth = (newWidth * 2) / 3;
                                                //if there is a diagonal behind the extension, make it a bit shorter.
                                                _Sprite.SrcRect.Width = newWidth;
                                                _Sprite.DestRect.Width = newWidth;
                                                world._2D.Draw(_Sprite);
                                            }
                                        }

                                        //check close side of wall for continuation
                                        contOff = tilePosition + RotateOffset(world.Rotation, new Vector3(0, 1, 0)); //so we can check the most things with the least impact
                                        var contOff2 = tilePosition + RotateOffset(world.Rotation, new Vector3(-1, 1, 0)); //last check, unfortunately required
                                        if (OffsetValid(contOff, blueprint) && OffsetValid(contOff2, blueprint))
                                        {
                                            var comp2 = RotateWall(world.Rotation, blueprint.GetWall((short)(contOff.X), (short)(contOff.Y), level), (short)(contOff.X), (short)(contOff.Y), level);
                                            if (!comp2.TopLeftThick && !comp2.TopRightThick)
                                            {
                                                var comp3 = RotateWall(world.Rotation, blueprint.GetWall((short)(contOff2.X), (short)(contOff2.Y), level), (short)(contOff2.X), (short)(contOff2.Y), level);
                                                if (!comp3.TopRightThick)
                                                {
                                                    _Sprite = GetWallSprite(tlPattern, tlStyle, 1, down, world);
                                                    var tilePosition2 = tilePosition + RotateOffset(world.Rotation, new Vector3(-1, 1, 0));
                                                    bool downAtCont = canCut && WallsDownAt((int)tilePosition.X, (int)tilePosition.Y) && styleID != 8;
                                                    tlStyle = GetStyle(comp.TopLeftStyle);


                                                    world._2D.OffsetPixel(world.WorldSpace.GetScreenFromTile(tilePosition2));
                                                    world._2D.OffsetTile(tilePosition2);
                                                    int newWidth = 0;


                                                    SPR mask = null;
                                                    switch (world.Zoom)
                                                    {
                                                        case WorldZoom.Far:
                                                            newWidth = 3;
                                                            mask = downAtCont ? tlStyle.WallsDownFar : tlStyle.WallsUpFar;
                                                            break;
                                                        case WorldZoom.Medium:
                                                            newWidth = 6;
                                                            mask = downAtCont ? tlStyle.WallsDownMedium : tlStyle.WallsUpMedium;
                                                            break;
                                                        case WorldZoom.Near:
                                                            newWidth = 12;
                                                            mask = downAtCont ? tlStyle.WallsDownNear : tlStyle.WallsUpNear;
                                                            break;
                                                    }
                                                    if (mask != null) _Sprite.Mask = world._2D.GetTexture(mask.Frames[1]);
                                                    _Sprite.SrcRect.X += _Sprite.SrcRect.Width - newWidth;
                                                    _Sprite.DestRect.X += _Sprite.DestRect.Width - newWidth;
                                                    _Sprite.SrcRect.Width = newWidth;
                                                    _Sprite.DestRect.Width = newWidth;
                                                    world._2D.Draw(_Sprite);
                                                }
                                            }
                                        }
                                        world._2D.OffsetPixel(world.WorldSpace.GetScreenFromTile(tilePosition));
                                        world._2D.OffsetTile(tilePosition);
                                    }
                                }
                            }
                            //top right

                            if ((comp.Segments & WallSegments.TopRight) == WallSegments.TopRight && !(myCuts.TRCut > 0 && comp.TopRightDoor))
                            {

                                var trPattern = GetPattern(comp.TopRightPattern);

                                ushort styleID = 1;
                                ushort overlayID = 0;
                                bool down = false;

                                switch (myCuts.TRCut)
                                {
                                    case WallCut.Down:
                                        styleID = comp.TopRightStyle;
                                        down = true; break;
                                    case WallCut.DownLeftUpRight:
                                        styleID = 7;
                                        overlayID = 252; break;
                                    case WallCut.DownRightUpLeft:
                                        styleID = 8;
                                        overlayID = 253; break;
                                    default:
                                        if (comp.TopRightStyle != 1) styleID = comp.TopRightStyle;
                                        else if (comp.ObjSetTRStyle != 0) styleID = comp.ObjSetTRStyle; //use custom style set by object if no cut
                                        else styleID = 1;
                                        break;
                                }

                                var trStyle = GetStyle(styleID);

                                var _Sprite = GetWallSprite(trPattern, trStyle, 1, down, world);
                                if (_Sprite.Pixel != null)
                                {
                                    world._2D.Draw(_Sprite);

                                    if (overlayID != 0) world._2D.Draw(GetWallSprite(GetPattern(overlayID), null, 1, down, world));
                                    var contOff = tilePosition + RotateOffset(world.Rotation, new Vector3(-1, 0, 0));
                                    if (comp.TopRightThick)
                                    {
                                        //check far side of wall for continuation. if there is none, round this part off
                                        if (!comp.TopLeftThick && OffsetValid(contOff, blueprint))
                                        { 
                                            var comp2 = RotateWall(world.Rotation, blueprint.GetWall((short)(contOff.X), (short)(contOff.Y), level), (short)(contOff.X), (short)(contOff.Y), level);
                                            if (!comp2.TopRightThick)
                                            {
                                                _Sprite = CopySprite(_Sprite);
                                                trStyle = GetStyle(comp.TopRightStyle); //return to normal if cutaway

                                                var tilePosition2 = contOff;
                                                world._2D.OffsetPixel(world.WorldSpace.GetScreenFromTile(tilePosition2));
                                                world._2D.OffsetTile(tilePosition2);
                                                int newWidth = 0;
                                                bool downAtCont = canCut && WallsDownAt((short)(contOff.X), (short)(contOff.Y));

                                                SPR mask = null;
                                                switch (world.Zoom)
                                                {
                                                    case WorldZoom.Far:
                                                        newWidth = 3;
                                                        mask = downAtCont ? trStyle.WallsDownFar : trStyle.WallsUpFar;
                                                        break;
                                                    case WorldZoom.Medium:
                                                        newWidth = 6;
                                                        mask = downAtCont ? trStyle.WallsDownMedium : trStyle.WallsUpMedium;
                                                        break;
                                                    case WorldZoom.Near:
                                                        newWidth = 12;
                                                        mask = downAtCont ? trStyle.WallsDownNear : trStyle.WallsUpNear;
                                                        break;
                                                }
                                                if (mask != null) _Sprite.Mask = world._2D.GetTexture(mask.Frames[1]);
                                                if (y > 0 && (blueprint.GetWall((short)(x - 1), (short)(y - 1), level).Segments & WallSegments.VerticalDiag) == WallSegments.VerticalDiag) newWidth = (newWidth * 2) / 3;
                                                //if there is a diagonal behind the extension, make it a bit shorter.
                                                _Sprite.SrcRect.X += _Sprite.SrcRect.Width - newWidth;
                                                _Sprite.DestRect.X += _Sprite.DestRect.Width - newWidth;
                                                _Sprite.SrcRect.Width = newWidth;
                                                _Sprite.DestRect.Width = newWidth;
                                                world._2D.Draw(_Sprite);
                                            }
                                        }

                                        //check close side of wall for continuation
                                        contOff = tilePosition + RotateOffset(world.Rotation, new Vector3(1, 0, 0)); //so we can check the most things with the least impact
                                        var contOff2 = tilePosition + RotateOffset(world.Rotation, new Vector3(1, -1, 0)); //last check, unfortunately required
                                        if (OffsetValid(contOff, blueprint) && OffsetValid(contOff2, blueprint))
                                        {
                                            var comp2 = RotateWall(world.Rotation, blueprint.GetWall((short)(contOff.X), (short)(contOff.Y), level), (short)(contOff.X), (short)(contOff.Y), level);
                                            if (!comp2.TopLeftThick && !comp2.TopRightThick)
                                            {
                                                var comp3 = RotateWall(world.Rotation, blueprint.GetWall((short)(contOff2.X), (short)(contOff2.Y), level), (short)(contOff2.X), (short)(contOff2.Y), level);
                                                if (!comp3.TopLeftThick)
                                                {
                                                    _Sprite = GetWallSprite(trPattern, trStyle, 0, down, world);
                                                    var tilePosition2 = tilePosition + RotateOffset(world.Rotation, new Vector3(1, -1, 0));
                                                    bool downAtCont = canCut && WallsDownAt((int)tilePosition.X, (int)tilePosition.Y) && styleID != 8;
                                                    trStyle = GetStyle(comp.TopRightStyle);


                                                    world._2D.OffsetPixel(world.WorldSpace.GetScreenFromTile(tilePosition2));
                                                    world._2D.OffsetTile(tilePosition2);
                                                    int newWidth = 0;


                                                    SPR mask = null;
                                                    switch (world.Zoom)
                                                    {
                                                        case WorldZoom.Far:
                                                            newWidth = 3;
                                                            mask = downAtCont ? trStyle.WallsDownFar : trStyle.WallsUpFar;
                                                            break;
                                                        case WorldZoom.Medium:
                                                            newWidth = 6;
                                                            mask = downAtCont ? trStyle.WallsDownMedium : trStyle.WallsUpMedium;
                                                            break;
                                                        case WorldZoom.Near:
                                                            newWidth = 12;
                                                            mask = downAtCont ? trStyle.WallsDownNear : trStyle.WallsUpNear;
                                                            break;
                                                    }
                                                    if (mask != null) _Sprite.Mask = world._2D.GetTexture(mask.Frames[0]);
                                                    _Sprite.SrcRect.Width = newWidth;
                                                    _Sprite.DestRect.Width = newWidth;
                                                    world._2D.Draw(_Sprite);
                                                }
                                            }
                                        }
                                        world._2D.OffsetPixel(world.WorldSpace.GetScreenFromTile(tilePosition));
                                        world._2D.OffsetTile(tilePosition);
                                    }
                                }
                            }

                            //horizontal diag

                            if (comp.Segments == WallSegments.HorizontalDiag)
                            {

                                var trPattern = GetPattern(comp.BottomRightPattern); //bottom left is facing other way.

                                ushort styleID = 1;
                                ushort overlayID = 0;
                                bool down = false;

                                switch (myCuts.TRCut)
                                {
                                    case WallCut.Down:
                                        styleID = comp.TopRightStyle;
                                        down = true; break;
                                    case WallCut.DownLeftUpRight:
                                        styleID = 7;
                                        overlayID = 252; break;
                                    case WallCut.DownRightUpLeft:
                                        styleID = 8;
                                        overlayID = 253; break;
                                    default:
                                        if (comp.TopRightStyle != 1) styleID = comp.TopRightStyle;
                                        else if (comp.ObjSetTRStyle != 0) styleID = comp.ObjSetTRStyle; //use custom style set by object if no cut
                                        else styleID = 1;
                                        break;
                                }

                                var trStyle = GetStyle(styleID);
                                
                                int roomSide = 16-((((int)world.Rotation+1)/2)%2) * 16;
                                var _Sprite = GetWallSprite(trPattern, trStyle, 2, down, world);

                                _Sprite.Room = (ushort)(TileRoom>>roomSide);
                                if (_Sprite.Pixel != null)
                                {
                                    world._2D.Draw(_Sprite);

                                    if (overlayID != 0) world._2D.Draw(GetWallSprite(GetPattern(overlayID), null, 2, down, world));

                                    //draw diagonally cut floors
                                    if (comp.TopLeftPattern != 0)
                                    {
                                        var floor = GetFloorSprite(floorContent.Get(comp.TopLeftPattern), 0, world, 3);
                                        floor.Room = (ushort)(TileRoom >> roomSide);
                                        if (floor.Pixel != null) world._2D.Draw(floor);
                                    }
                                    if (comp.TopLeftStyle != 0)
                                    {
                                        var floor = GetFloorSprite(floorContent.Get(comp.TopLeftStyle), 0, world, 2);
                                        floor.Room = (ushort)(TileRoom >> (16-roomSide));
                                        if (floor.Pixel != null) world._2D.Draw(floor);
                                    }
                                }
                            }

                            if (comp.Segments == WallSegments.VerticalDiag)
                            {

                                var trPattern = GetPattern(comp.BottomRightPattern); //choose right one here, not sure which is chosen in real game

                                ushort styleID = 1;
                                ushort overlayID = 0;
                                bool down = false;

                                switch (myCuts.TRCut)
                                {
                                    case WallCut.Down:
                                        styleID = comp.TopRightStyle;
                                        down = true; break;
                                    case WallCut.DownLeftUpRight:
                                        styleID = 7;
                                        overlayID = 252; break;
                                    case WallCut.DownRightUpLeft:
                                        styleID = 8;
                                        overlayID = 253; break;
                                    default:
                                        if (comp.TopRightStyle != 1) styleID = comp.TopRightStyle;
                                        else if (comp.ObjSetTRStyle != 0) styleID = comp.ObjSetTRStyle; //use custom style set by object if no cut
                                        else styleID = 1;
                                        break;
                                }

                                var trStyle = GetStyle(styleID);

                                int roomSide = ((int)world.Rotation / 2) * 16;
                                var _Sprite = GetWallSprite(trPattern, trStyle, 3, down, world);
                                _Sprite.Room = (ushort)(TileRoom>>roomSide);
                                if (_Sprite.Pixel != null)
                                {
                                    world._2D.Draw(_Sprite);

                                    if (overlayID != 0) world._2D.Draw(GetWallSprite(GetPattern(overlayID), null, 3, down, world));

                                    //draw diagonally cut floors
                                    if (comp.TopLeftPattern != 0)
                                    {
                                        var floor = GetFloorSprite(floorContent.Get(comp.TopLeftPattern), 0, world, 1);
                                        floor.Room = (ushort)(TileRoom >> roomSide);
                                        if (floor.Pixel != null) world._2D.Draw(floor);
                                    }
                                    if (comp.TopLeftStyle != 0)
                                    {
                                        var floor = GetFloorSprite(floorContent.Get(comp.TopLeftStyle), 0, world, 0);
                                        floor.Room = (ushort)(TileRoom >> (16-roomSide));
                                        if (floor.Pixel != null) world._2D.Draw(floor);
                                    }
                                }
                            }
                        }

                        //draw junctions (part of this iteration to simplify things)

                        JunctionFlags flags;

                        float yOff;
                        if (UpJunctions[off] == 0)
                        {
                            flags = DownJunctions[off];
                            yOff = 0.3f + (level - 1) * 2.95f; ;
                        }
                        else
                        {
                            flags = UpJunctions[off];
                            yOff = level * 2.95f;
                        }

                        if (flags > 0 && JunctionMap.ContainsKey(flags)) //there is a junction here! if the junction map contains the unrotated junction, it will contain the rotated junction.
                        {
                            flags = RotateJunction(world.Rotation, flags);
                            var tilePosition = new Vector3(x - 0.5f, y - 0.5f, yOff); //2.95 for walls up, 0.3 for walls down
                            world._2D.OffsetPixel(world.WorldSpace.GetScreenFromTile(tilePosition));
                            world._2D.OffsetTile(tilePosition);

                            var _Sprite = new _2DSprite()
                            {
                                RenderMode = _2DBatchRenderMode.Z_BUFFER
                            };

                            var Junctions = wallContent.Junctions;

                            SPR sprite = null;
                            switch (world.Zoom)
                            {
                                case WorldZoom.Far:
                                    sprite = Junctions.Far;
                                    _Sprite.DestRect = JUNCDEST_FAR;
                                    _Sprite.Depth = WallZBuffers[20];
                                    break;
                                case WorldZoom.Medium:
                                    sprite = Junctions.Medium;
                                    _Sprite.DestRect = JUNCDEST_MED;
                                    _Sprite.Depth = WallZBuffers[19];
                                    break;
                                case WorldZoom.Near:
                                    sprite = Junctions.Near;
                                    _Sprite.DestRect = JUNCDEST_NEAR;
                                    _Sprite.Depth = WallZBuffers[18];
                                    break;
                            }
                            _Sprite.Pixel = world._2D.GetTexture(sprite.Frames[JunctionMap[flags]]);
                            _Sprite.SrcRect = new Microsoft.Xna.Framework.Rectangle(0, 0, _Sprite.Pixel.Width, _Sprite.Pixel.Height);
                            _Sprite.Room = 1;
                            world._2D.Draw(_Sprite);
                        }

                        off++;
                    }
                }
            }
            timer.Stop();
            //System.Diagnostics.Debug.WriteLine("Drawing walls took " + timer.ElapsedMilliseconds.ToString() + " ms");
        }

        private bool OffsetValid(Vector3 off, Blueprint blueprint)
        {
            return off.X >= 0 && off.Y >= 0 && off.X < blueprint.Width && off.Y < blueprint.Height;
        }

        private Vector3 RotateOffset(WorldRotation rot, Vector3 off)
        {
            switch (rot) {
                case WorldRotation.TopLeft:
                    return off;
                case WorldRotation.TopRight:
                    return new Vector3(off.Y, -off.X, off.Z);
                case WorldRotation.BottomRight:
                    return new Vector3(-off.X, -off.Y, off.Z);
                case WorldRotation.BottomLeft:
                    return new Vector3(-off.Y, off.X, off.Z);
            }
            return off;
        }

        private void GenerateWallData(WallTile[] walls, List<int> wallsAt, bool notTop) {

            var width = blueprint.Width;
            var height = blueprint.Height;
            Cuts = new WallCuts[width * height];
            DownJunctions = new JunctionFlags[width * height];
            UpJunctions = new JunctionFlags[width * height];

            foreach (var off in wallsAt)
            {
                var wall = walls[off];
                var x = off % width;
                var y = off / width;
                var result = new WallCuts();
                if (notTop && WallsDownAt(x, y))
                {
                    var cuts = GetCutEdges(off % width, off / width);

                    if (wall.TopLeftThick && wall.TopLeftStyle != 255)
                    {
                        if (cuts != 0)
                        {
                            if ((cuts & CutawayEdges.NegativeX) == CutawayEdges.NegativeX) result.TLCut = WallCut.Up; //if we are on the very edge of the cut we're up
                            else if ((cuts & CutawayEdges.PositiveY) == CutawayEdges.PositiveY) {
                                if ((cuts & CutawayEdges.NegativeY) == CutawayEdges.NegativeY)
                                {
                                    result.TLCut = WallCut.Down; //special case, cuts at both sides... just put wall down
                                }
                                else
                                {
                                    result.TLCut = WallCut.DownRightUpLeft;
                                }
                            }
                            else if ((cuts & CutawayEdges.NegativeY) == CutawayEdges.NegativeY)
                            {
                                result.TLCut = WallCut.DownLeftUpRight;
                            }
                            else result.TLCut = WallCut.Down;
                        }
                        else
                        {
                            result.TLCut = WallCut.Down;
                        }

                    }

                    if (wall.TopRightStyle == 1) //NOTE: top right style also includes diagonals!
                    {
                        if (wall.Segments == WallSegments.HorizontalDiag) {
                            if (cuts != 0)
                            {
                                var cutOnLeft = (cuts & (CutawayEdges.PositiveY | CutawayEdges.NegativeX)) > 0;
                                var cutOnRight = (cuts & (CutawayEdges.NegativeY | CutawayEdges.PositiveX)) > 0;
                                if (cutOnLeft && cutOnRight) result.TRCut = WallCut.Down;
                                else if (cutOnLeft) result.TRCut = WallCut.DownRightUpLeft;
                                else if (cutOnRight) result.TRCut = WallCut.DownLeftUpRight;
                                else result.TRCut = WallCut.Down;
                            }
                            else
                            {
                                result.TRCut = WallCut.Down;
                            }
                        }
                        else if (wall.Segments == WallSegments.VerticalDiag)
                        {
                            if (cuts != 0) //this info is not useful for front rotation, but is useful for sides.
                            {
                                var cutOnLeft = (cuts & (CutawayEdges.PositiveY | CutawayEdges.PositiveX)) > 0;
                                var cutOnRight = (cuts & (CutawayEdges.NegativeY | CutawayEdges.NegativeX)) > 0;
                                if (cutOnLeft && cutOnRight) result.TRCut = WallCut.Down;
                                else if (cutOnLeft) result.TRCut = WallCut.DownRightUpLeft;
                                else if (cutOnRight) result.TRCut = WallCut.DownLeftUpRight;
                                else result.TRCut = WallCut.Down;
                            }
                            else
                            {
                                result.TRCut = WallCut.Down;
                            }
                        }
                        else
                        {
                            if (cuts != 0)
                            {
                                if ((cuts & CutawayEdges.NegativeY) == CutawayEdges.NegativeY) result.TRCut = WallCut.Up; //if we are on the very edge of the cut we're up
                                else if ((cuts & CutawayEdges.PositiveX) == CutawayEdges.PositiveX)
                                {
                                    if ((cuts & CutawayEdges.NegativeX) == CutawayEdges.NegativeX)
                                    { //special case, cuts at both sides... just put wall down
                                        result.TRCut = WallCut.Down;
                                    }
                                    else
                                    {
                                        result.TRCut = WallCut.DownLeftUpRight;
                                    }
                                }
                                else if ((cuts & CutawayEdges.NegativeX) == CutawayEdges.NegativeX)
                                {
                                    result.TRCut = WallCut.DownRightUpLeft;
                                }
                                else result.TRCut = WallCut.Down;
                            }
                            else
                            {
                                result.TRCut = WallCut.Down;
                            }
                        }
                    }
                }
                //add to relevant junctions
                if ((wall.Segments & WallSegments.TopLeft) > 0 && !(wall.TopLeftDoor && result.TLCut > 0) && wall.TopLeftThick)
                {
                    if (result.TLCut > 0)
                    {
                        DownJunctions[off] |= JunctionFlags.BottomLeft;
                        if (y < height) DownJunctions[off + width] |= JunctionFlags.TopRight;
                    }
                    else
                    {
                        UpJunctions[off] |= JunctionFlags.BottomLeft;
                        if (y < height) UpJunctions[off + width] |= JunctionFlags.TopRight;
                    }
                }

                if ((wall.Segments & WallSegments.TopRight) > 0 && !(wall.TopRightDoor && result.TRCut > 0) && wall.TopRightThick)
                {
                    if (result.TRCut > 0)
                    {
                        DownJunctions[off] |= JunctionFlags.BottomRight;
                        if (x < width) DownJunctions[off + 1] |= JunctionFlags.TopLeft;
                    }
                    else
                    {
                        UpJunctions[off] |= JunctionFlags.BottomRight;
                        if (x < width) UpJunctions[off + 1] |= JunctionFlags.TopLeft;
                    }
                }

                if (wall.Segments == WallSegments.VerticalDiag && (wall.TopRightStyle == 1 || wall.TopRightStyle == 255))
                {
                    if (result.TRCut > 0)
                    {
                        DownJunctions[off] |= JunctionFlags.DiagBottom;
                        if (x < width && y < height) DownJunctions[off + 1 + width] |= JunctionFlags.DiagTop;
                    }
                    else
                    {
                        UpJunctions[off] |= JunctionFlags.DiagBottom;
                        if (x < width && y < height) UpJunctions[off + 1 + width] |= JunctionFlags.DiagTop;
                    }
                }
                else if (wall.Segments == WallSegments.HorizontalDiag && (wall.TopRightStyle == 1 || wall.TopRightStyle == 255))
                {
                    if (result.TRCut > 0)
                    {
                        if (x < width) DownJunctions[off + 1] |= JunctionFlags.DiagLeft;
                        if (y < height) DownJunctions[off + width] |= JunctionFlags.DiagRight;
                    }
                    else
                    {
                        if (x < width) UpJunctions[off + 1] |= JunctionFlags.DiagLeft;
                        if (y < height) UpJunctions[off + width] |= JunctionFlags.DiagRight;
                    }
                }
                Cuts[off] = result;
            }
        }

        /// <summary>
        /// Walls Cutaway helper methods.
        /// </summary>
        private bool WallsDownAt(int x, int y)
        {
            var cuts = blueprint.Cutaway;
            foreach (var cut in cuts)
            {
                if (cut.Contains(x, y)) return true;
            }
            return false;
        }

        private CutawayEdges GetCutEdges(int x, int y) //todo, rotate result for rotations
        {
            var result = new CutawayEdges();
            if (!WallsDownAt(x + 1, y)) result |= CutawayEdges.PositiveX;
            if (!WallsDownAt(x - 1, y)) result |= CutawayEdges.NegativeX;
            if (!WallsDownAt(x, y + 1)) result |= CutawayEdges.PositiveY;
            if (!WallsDownAt(x, y - 1)) result |= CutawayEdges.NegativeY;
            return result;
        }

        private Wall GetPattern(ushort id)
        {
            if (!WallCache.ContainsKey(id)) WallCache.Add(id, Content.Get().WorldWalls.Get(id));
            return WallCache[id];
        }

        private WallStyle GetStyle(ushort id)
        {
            if (!WallStyleCache.ContainsKey(id)) WallStyleCache.Add(id, Content.Get().WorldWalls.GetWallStyle(id));
            return WallStyleCache[id];
        }

        private _2DSprite GetWallSprite(Wall pattern, WallStyle style, int rotation, bool down, WorldState world)
        {
            var _Sprite = new _2DSprite()
            {
                RenderMode = _2DBatchRenderMode.WALL
            };
            SPR sprite = null;
            SPR mask = null;
            switch (world.Zoom)
            {
                case WorldZoom.Far:
                    sprite = pattern.Far;
                    if (style != null) mask = (down) ? style.WallsDownFar : style.WallsUpFar;
                    _Sprite.DestRect = DESTINATION_FAR[rotation];
                    _Sprite.Depth = WallZBuffers[rotation+8];
                    break;
                case WorldZoom.Medium:
                    sprite = pattern.Medium;
                    if (style != null) mask = (down) ? style.WallsDownMedium : style.WallsUpMedium;
                    _Sprite.DestRect = DESTINATION_MED[rotation];
                    _Sprite.Depth = WallZBuffers[rotation+4];
                    break;
                case WorldZoom.Near:
                    sprite = pattern.Near;
                    if (style != null) mask = (down) ? style.WallsDownNear : style.WallsUpNear;
                    _Sprite.DestRect = DESTINATION_NEAR[rotation];
                    _Sprite.Depth = WallZBuffers[rotation];
                    break;
                }
            if (sprite != null)
            {
                if (mask == null) mask = sprite;
                _Sprite.Pixel = world._2D.GetTexture(sprite.Frames[rotation]);
                _Sprite.Mask = world._2D.GetTexture(mask.Frames[rotation]);
                _Sprite.SrcRect = new Microsoft.Xna.Framework.Rectangle(0, 0, _Sprite.Pixel.Width, _Sprite.Pixel.Height);
            }

            _Sprite.Room = (ushort)TileRoom;

            return _Sprite;
        }

        //Gets a floor sprite. Used to draw floors cut in half by walls.

        private _2DSprite GetFloorSprite(Floor pattern, int rotation, WorldState world, byte cut)
        {
            var _Sprite = new _2DSprite()
            {
                RenderMode = _2DBatchRenderMode.Z_BUFFER
            };
            if (pattern == null) return _Sprite;
            SPR2 sprite = null;
            switch (world.Zoom)
            {
                case WorldZoom.Far:
                    sprite = pattern.Far;
                    _Sprite.DestRect = FLRDEST_FAR;
                    _Sprite.Depth = WallZBuffers[14];
                    break;
                case WorldZoom.Medium:
                    sprite = pattern.Medium;
                    _Sprite.DestRect = FLRDEST_MED;
                    _Sprite.Depth = WallZBuffers[13];
                    break;
                case WorldZoom.Near:
                    sprite = pattern.Near;
                    _Sprite.DestRect = FLRDEST_NEAR;
                    _Sprite.Depth = WallZBuffers[12];
                    break;
            }
            if (sprite != null)
            {
                _Sprite.Pixel = world._2D.GetTexture(sprite.Frames[rotation]);
                _Sprite.SrcRect = new Microsoft.Xna.Framework.Rectangle(0, 0, _Sprite.Pixel.Width, _Sprite.Pixel.Height);
            }

            switch (cut)
            {
                case 0: //vertical cut, left side
                    _Sprite.DestRect.Width /= 2;
                    _Sprite.SrcRect.Width /= 2;
                    break;
                case 1: //vertical cut, right side
                    _Sprite.DestRect.X += _Sprite.DestRect.Width / 2;
                    _Sprite.DestRect.Width /= 2;
                    _Sprite.SrcRect.X += _Sprite.SrcRect.Width / 2;
                    _Sprite.SrcRect.Width /= 2;
                    break;
                case 2: //horizontal cut, top side
                    _Sprite.DestRect.Height /= 2;
                    _Sprite.SrcRect.Height /= 2;
                    break;
                case 3:
                    _Sprite.DestRect.Y += _Sprite.DestRect.Height / 2;
                    _Sprite.DestRect.Height /= 2;
                    _Sprite.SrcRect.Y += _Sprite.SrcRect.Height / 2;
                    _Sprite.SrcRect.Height /= 2;
                    break;
            }

            _Sprite.Room = (ushort)TileRoom;

            return _Sprite;
        }

        /// <summary>
        /// Gets rotated junctions and segements
        /// </summary>

        private JunctionFlags RotateJunction(WorldRotation rot, JunctionFlags input)
        {
            var rotN = (int)rot;
            int rotLower = (((int)input & 15) << rotN);
            int rotHigher = (((int)input & 240) << rotN);
            return (JunctionFlags)((rotLower & 15) | (rotLower >> 4) | ((rotHigher | rotHigher>>4) & 240));
        }

        private WallCuts RotateCuts(WorldRotation rot, WallCuts input, short x, short y)
        {
            int rotN = (int)rot;
            var output = new WallCuts();
            switch (rotN)
            {
                case 0:
                    return input;
                case 1:
                    output.TRCut = input.TLCut;
                    if (y + 1 < blueprint.Height) output.TLCut = Cuts[(y + 1) * blueprint.Width + x].TRCut;

                    if (output.TLCut == WallCut.DownLeftUpRight) output.TLCut = WallCut.DownRightUpLeft; //flip cut
                    else if (output.TLCut == WallCut.DownRightUpLeft) output.TLCut = WallCut.DownLeftUpRight;

                    return output;
                case 2:
                    output.TRCut = input.TLCut;
                    if (y + 1 < blueprint.Height) output.TRCut = Cuts[(y + 1) * blueprint.Width + x].TRCut;
                    if (x + 1 < blueprint.Width) output.TLCut = Cuts[y * blueprint.Width + x + 1].TLCut;

                    if (output.TLCut == WallCut.DownLeftUpRight) output.TLCut = WallCut.DownRightUpLeft; //flip cuts
                    else if (output.TLCut == WallCut.DownRightUpLeft) output.TLCut = WallCut.DownLeftUpRight;
                    if (output.TRCut == WallCut.DownLeftUpRight) output.TRCut = WallCut.DownRightUpLeft;
                    else if (output.TRCut == WallCut.DownRightUpLeft) output.TRCut = WallCut.DownLeftUpRight;

                    return output;
                case 3:
                    output.TLCut = input.TRCut;
                    if (x + 1 < blueprint.Width) output.TRCut = Cuts[y * blueprint.Width + x+1].TLCut;

                    if (output.TRCut == WallCut.DownLeftUpRight) output.TRCut = WallCut.DownRightUpLeft; //flip cut
                    else if (output.TRCut == WallCut.DownRightUpLeft) output.TRCut = WallCut.DownLeftUpRight;

                    return output;
            }
            return output;
        }

        private WallTile RotateWall(WorldRotation rot, WallTile input, short x, short y, sbyte level)
        {
            int rotN = (int)rot;
            var output = new WallTile();
            if (rot != 0)
            {
                if (input.Segments == WallSegments.HorizontalDiag)
                {
                    output.Segments = ((rotN % 2) == 0) ? WallSegments.HorizontalDiag : WallSegments.VerticalDiag;
                    output.TopRightStyle = input.TopRightStyle;
                    switch (rotN)
                    {
                        case 1:
                            output.BottomRightPattern = input.BottomLeftPattern;
                            output.BottomLeftPattern = input.BottomRightPattern;
                            output.TopLeftStyle = input.TopLeftPattern;
                            output.TopLeftPattern = input.TopLeftStyle;
                            break;
                        case 2:
                            output.BottomRightPattern = input.BottomLeftPattern; //flip sides
                            output.BottomLeftPattern = input.BottomRightPattern;
                            output.TopLeftStyle = input.TopLeftPattern;
                            output.TopLeftPattern = input.TopLeftStyle;
                            break;
                        case 3:
                            output.BottomRightPattern = input.BottomRightPattern;
                            output.BottomLeftPattern = input.BottomLeftPattern;
                            output.TopLeftStyle = input.TopLeftStyle;
                            output.TopLeftPattern = input.TopLeftPattern;
                            break;
                    }
                        
                }
                else if (input.Segments == WallSegments.VerticalDiag)
                {
                    output.Segments = ((rotN % 2) == 0) ? WallSegments.VerticalDiag : WallSegments.HorizontalDiag;
                    output.TopRightStyle = input.TopRightStyle;
                    switch (rotN)
                    {
                        case 1:
                            output.BottomRightPattern = input.BottomRightPattern;
                            output.BottomLeftPattern = input.BottomLeftPattern;
                            output.TopLeftStyle = input.TopLeftStyle;
                            output.TopLeftPattern = input.TopLeftPattern;
                            break;
                        case 2:
                            output.BottomRightPattern = input.BottomLeftPattern; //flip sides
                            output.BottomLeftPattern = input.BottomRightPattern;
                            output.TopLeftStyle = input.TopLeftPattern;
                            output.TopLeftPattern = input.TopLeftStyle;
                            break;
                        case 3:
                            output.BottomRightPattern = input.BottomLeftPattern;
                            output.BottomLeftPattern = input.BottomRightPattern;
                            output.TopLeftStyle = input.TopLeftPattern;
                            output.TopLeftPattern = input.TopLeftStyle;
                            break;
                    }
                }
                else
                {
                    switch (rotN)
                    {
                        case 1:
                            if ((input.Segments & WallSegments.TopLeft) > 0) output.Segments |= WallSegments.TopRight;
                            if ((input.Segments & WallSegments.TopRight) > 0) output.Segments |= WallSegments.BottomRight;
                            if ((input.Segments & WallSegments.BottomRight) > 0) output.Segments |= WallSegments.BottomLeft;
                            if ((input.Segments & WallSegments.BottomLeft) > 0) output.Segments |= WallSegments.TopLeft;
                            output.TopLeftPattern = input.BottomLeftPattern;
                            output.TopRightPattern = input.TopLeftPattern;
                            output.BottomRightPattern = input.TopRightPattern;
                            output.BottomLeftPattern = input.BottomRightPattern;

                            if (y + 1 < blueprint.Height)
                            {
                                var newLeft = blueprint.GetWall(x, (short)(y + 1), level);
                                output.TopLeftStyle = newLeft.TopRightStyle;
                                output.ObjSetTLStyle = newLeft.ObjSetTRStyle;
                                output.TopLeftDoor = newLeft.TopRightDoor;
                            }

                            output.TopRightStyle = input.TopLeftStyle;
                            output.ObjSetTRStyle = input.ObjSetTLStyle;
                            output.TopRightDoor = input.TopLeftDoor;
                            break;
                        case 2:
                            if ((input.Segments & WallSegments.TopLeft) > 0) output.Segments |= WallSegments.BottomRight;
                            if ((input.Segments & WallSegments.TopRight) > 0) output.Segments |= WallSegments.BottomLeft;
                            if ((input.Segments & WallSegments.BottomRight) > 0) output.Segments |= WallSegments.TopLeft;
                            if ((input.Segments & WallSegments.BottomLeft) > 0) output.Segments |= WallSegments.TopRight;
                            output.TopLeftPattern = input.BottomRightPattern;
                            output.TopRightPattern = input.BottomLeftPattern;
                            output.BottomRightPattern = input.TopLeftPattern;
                            output.BottomLeftPattern = input.TopRightPattern;

                            if (y + 1 < blueprint.Height)
                            {
                                var newRight = blueprint.GetWall(x, (short)(y + 1), level);
                                output.TopRightStyle = newRight.TopRightStyle;
                                output.ObjSetTRStyle = newRight.ObjSetTRStyle;
                                output.TopRightDoor = newRight.TopRightDoor;
                            }

                            if (x + 1 < blueprint.Width)
                            {
                                var newLeft = blueprint.GetWall((short)(x + 1), y, level);
                                output.TopLeftStyle = newLeft.TopLeftStyle;
                                output.ObjSetTLStyle = newLeft.ObjSetTLStyle;
                                output.TopLeftDoor = newLeft.TopLeftDoor;
                            }

                            break;
                        case 3:
                            if ((input.Segments & WallSegments.TopLeft) > 0) output.Segments |= WallSegments.BottomLeft;
                            if ((input.Segments & WallSegments.TopRight) > 0) output.Segments |= WallSegments.TopLeft;
                            if ((input.Segments & WallSegments.BottomRight) > 0) output.Segments |= WallSegments.TopRight;
                            if ((input.Segments & WallSegments.BottomLeft) > 0) output.Segments |= WallSegments.BottomRight;
                            output.TopLeftPattern = input.TopRightPattern;
                            output.TopRightPattern = input.BottomRightPattern;
                            output.BottomRightPattern = input.BottomLeftPattern;
                            output.BottomLeftPattern = input.TopLeftPattern;

                            output.TopLeftStyle = input.TopRightStyle;
                            output.TopLeftDoor = input.TopRightDoor;
                            output.ObjSetTLStyle = input.ObjSetTRStyle;

                            if (x + 1 < blueprint.Width)
                            {
                                var newRight = blueprint.GetWall((short)(x + 1), y, level);
                                output.TopRightStyle = newRight.TopLeftStyle;
                                output.ObjSetTRStyle = newRight.ObjSetTLStyle;
                                output.TopRightDoor = newRight.TopLeftDoor;
                            }
                            break;
                    }
                }
            }
            else
            {
                output = input;
            }

            return output;
        }

        private _2DSprite CopySprite(_2DSprite _Sprite)
        {
            return new _2DSprite()
            {
                DestRect = _Sprite.DestRect,
                SrcRect = _Sprite.SrcRect,
                RenderMode = _2DBatchRenderMode.WALL,
                Pixel = _Sprite.Pixel,
                Depth = _Sprite.Depth,
                Room = _Sprite.Room
            };
        }
    }

    public struct WallCuts
    {
        public WallCut TLCut;
        public WallCut TRCut;
    }

    public enum WallCut : byte
    {
        Up = 0,
        DownLeftUpRight = 1,
        DownRightUpLeft = 2,
        Down = 3
    }

    [Flags]
    public enum CutawayEdges : byte
    {
        PositiveY = 1,
        PositiveX = 2,
        NegativeY = 4,
        NegativeX = 8,
    }

    [Flags]
    public enum JunctionFlags : byte
    {
        TopLeft = 1,
        TopRight = 2,
        BottomRight = 4,
        BottomLeft = 8,
        DiagLeft = 16,
        DiagTop = 32,
        DiagRight = 64,
        DiagBottom = 128,

        AllNonDiag = 15,
        AllDiag = 240
    }
}