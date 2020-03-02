﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Editor;
using Assets.Scripts;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

public class LayerCurveSet
{
    public string Path;
    public EditorCurveBinding SpriteBinding;
    public EditorCurveBinding PositionXBinding;
    public EditorCurveBinding PositionYBinding;
    public EditorCurveBinding PositionZBinding;
}

public static class ImporterExtensions
{
    public static Keyframe SetConstant(this Keyframe frame)
    {
        frame.inTangent = Mathf.Infinity;
        frame.outTangent = Mathf.Infinity;
        return frame;
    }
}


[ScriptedImporter(1, "spr")]
public class SprImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var path = ctx.assetPath;
        var name = Path.GetFileNameWithoutExtension(ctx.assetPath);

        var spr = new RagnarokSpriteLoader();
        spr.Load(ctx);

        var basePath = Path.GetDirectoryName(path);
        var baseName = Path.GetFileNameWithoutExtension(path);
        var actName = Path.Combine(basePath, baseName + ".act");

        if (File.Exists(actName))
        {
            var actLoader = new RagnarokActLoader();
            var actions = actLoader.Load(ctx, spr, actName);

            var asset = ScriptableObject.CreateInstance(typeof(RoSpriteData)) as RoSpriteData;
            asset.Actions = actions.ToArray();
            asset.Sprites = spr.Sprites.ToArray();
            asset.SpriteSizes = spr.SpriteSizes.ToArray();
            asset.Name = baseName;
            asset.Atlas = spr.Atlas;
            asset.Sounds = new AudioClip[actLoader.Sounds.Length];


            for(var i = 0; i < asset.Sounds.Length; i++)
            {
                var s = actLoader.Sounds[i];
                var sPath = $"Assets/sounds/{s}";
                var sound = AssetDatabase.LoadAssetAtPath<AudioClip>(sPath);
                if(sound == null)
                    Debug.Log("Could not find sound " + sPath);
                asset.Sounds[i] = sound;
            }
            //asset.Sounds = asset.Sounds.ToArray();
            

            Debug.Log(asset.Sprites.Length);

            switch (asset.Actions.Length)
            {

                case 8:
                    asset.Type = SpriteType.Npc;
                    break;
                case 32:
                    asset.Type = SpriteType.ActionNpc;
                    break;
                case 40:
                    asset.Type = SpriteType.Monster;
                    break;
                case 48:
                    asset.Type = SpriteType.Monster2;
                    break;
                case 56:
	                asset.Type = SpriteType.Monster; //???
	                break;
                case 64:
                    asset.Type = SpriteType.Monster;
                    break;
                case 72:
                    asset.Type = SpriteType.Pet;
                    break;
            }

            var maxExtent = 0f;

            foreach (var a in asset.Actions)
            {
                foreach (var f in a.Frames)
                {
                    foreach (var l in f.Layers)
                    {
                        if (l.Index == -1)
                            continue;
                        var sprite = asset.SpriteSizes[l.Index];
                        var y = l.Position.y + sprite.y / 2f;
                        if (l.Position.x < 0)
                            y = Mathf.Abs(l.Position.y - sprite.y / 2f);
                        if (y > maxExtent)
                            maxExtent = y;

                    }
                }
            }

            asset.Size = Mathf.CeilToInt(maxExtent);

            //Debug.Log(asset.Actions.Length);

            ctx.AddObjectToAsset(name + " data", asset);
            ctx.SetMainObject(asset);
            
            //CreateObjectWithAnimations(obj, ctx, spr.Sprites, actions);
        }
    }
}
