using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


namespace UniGLTF.UniUnlit
{
    public enum UniUnlitRenderMode
    {
        Opaque,
        Cutout,
        Transparent,
        TransparentWithZWrite
    }

    public enum UniUnlitVertexColorBlendOp
    {
        None,
        Multiply,
        Additive,
    }
        
    public class UniUnlitEditor : ShaderGUI
    {
        private const string PropNameMainTex = "_MainTex";
        private const string PropNameColor = "_Color";
        private const string PropNameCutoff = "_Cutoff";
        private const string PropNameBlendMode = "_BlendMode";
        private const string PropNameCullMode = "_CullMode";
        private const string PropeNameVColBlendMode = "_VColBlendMode";
        private const string PropNameSrcBlend = "_SrcBlend";
        private const string PropNameDstBlend = "_DstBlend";
        private const string PropNameZWrite = "_ZWrite";

        private const string PropNameStandardShadersRenderMode = "_Mode";

        private const string KeywordAlphaTestOn = "_ALPHATEST_ON";
        private const string KeywordAlphaBlendOn = "_ALPHABLEND_ON";
        private const string KeywordVertexColMul = "_VERTEXCOL_MUL";
        private const string KeywordVertexColAdd = "_VERTEXCOL_ADD";

        private const string TagRenderTypeKey = "RenderType";
        private const string TagRenderTypeValueOpaque = "Opaque";
        private const string TagRenderTypeValueTransparentCutout = "TransparentCutout";
        private const string TagRenderTypeValueTransparent = "Transparent";
        
        private MaterialProperty _mainTex;
        private MaterialProperty _color;
        private MaterialProperty _cutoff;
        private MaterialProperty _blendMode;
        private MaterialProperty _cullMode;
        private MaterialProperty _vColBlendMode;
        private MaterialProperty _srcBlend;
        private MaterialProperty _dstBlend;
        private MaterialProperty _zWrite;
        
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            _mainTex = FindProperty(PropNameMainTex, properties);
            _color = FindProperty(PropNameColor, properties);
            _cutoff = FindProperty(PropNameCutoff, properties);
            _blendMode = FindProperty(PropNameBlendMode, properties);
            _cullMode = FindProperty(PropNameCullMode, properties);
            _vColBlendMode = FindProperty(PropeNameVColBlendMode, properties);
            _srcBlend = FindProperty(PropNameSrcBlend, properties);
            _dstBlend = FindProperty(PropNameDstBlend, properties);
            _zWrite = FindProperty(PropNameZWrite, properties);

            var materials = materialEditor.targets.Select(x => x as Material).ToArray();
            
            EditorGUI.BeginChangeCheck();
            {
                DrawRenderingBox(materialEditor, materials);
                DrawColorBox(materialEditor, materials);
                DrawOptionsBox(materialEditor, materials);
            }
            EditorGUI.EndChangeCheck();
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            var blendMode = UniUnlitRenderMode.Opaque;
            if (material.HasProperty(PropNameStandardShadersRenderMode)) // from Standard shader
            {
                blendMode = (UniUnlitRenderMode) Math.Min(2f, material.GetFloat(PropNameStandardShadersRenderMode));
            }

            // assigns UniUnlit's properties...
            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            // take over old value
            material.SetFloat(PropNameBlendMode, (float) blendMode);
            
            ModeChanged(material, isRenderModeChangedByUser: true);
        }

        private void DrawRenderingBox(MaterialEditor materialEditor, Material[] materials)
        {
            EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                if (PopupEnum<UniUnlitRenderMode>("Rendering Type", _blendMode, materialEditor))
                {
                    ModeChanged(materials, isRenderModeChangedByUser: true);
                }
                if (PopupEnum<CullMode>("Cull Mode", _cullMode, materialEditor))
                {
                    ModeChanged(materials, isRenderModeChangedByUser: true);
                }
                EditorGUILayout.Space();

                switch ((UniUnlitRenderMode) _blendMode.floatValue)
                {
                    case UniUnlitRenderMode.Cutout:
                        materialEditor.ShaderProperty(_cutoff, "Cutoff");
                        break;
                    case UniUnlitRenderMode.Opaque:
                    case UniUnlitRenderMode.Transparent:
                        break;
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        private void DrawColorBox(MaterialEditor materialEditor, Material[] materials)
        {
            EditorGUILayout.LabelField("Color", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("Main Tex", "(RGBA)"), _mainTex, _color);
                materialEditor.TextureScaleOffsetProperty(_mainTex);
                EditorGUILayout.Space();
                
                if (PopupEnum<UniUnlitVertexColorBlendOp>("Vertex Color Blend Mode", _vColBlendMode, materialEditor))
                {
                    ModeChanged(materials, isRenderModeChangedByUser: true);
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawOptionsBox(MaterialEditor materialEditor, Material[] materials)
        {
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                #if UNITY_5_6_OR_NEWER
//                    materialEditor.EnableInstancingField();
                    materialEditor.DoubleSidedGIField();
                #endif
                    materialEditor.RenderQueueField();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        private static bool PopupEnum<T>(string name, MaterialProperty property, MaterialEditor editor) where T : struct
        {
            EditorGUI.showMixedValue = property.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            var ret = EditorGUILayout.Popup(name, (int) property.floatValue, Enum.GetNames(typeof(T)));
            var changed = EditorGUI.EndChangeCheck();
            if (changed)
            {
                editor.RegisterPropertyChangeUndo("EnumPopUp");
                property.floatValue = ret;
            }
            EditorGUI.showMixedValue = false;
            return changed;
        }


        private static void ModeChanged(Material[] materials, bool isRenderModeChangedByUser = false)
        {
            foreach (var material in materials)
            {
                ModeChanged(material, isRenderModeChangedByUser);
            }
        }
        private static void ModeChanged(Material material, bool isRenderModeChangedByUser = false)
        {
            SetupBlendMode(material, (UniUnlitRenderMode) material.GetFloat(PropNameBlendMode),
                isRenderModeChangedByUser);
            SetupVertexColorBlendOp(material, (UniUnlitVertexColorBlendOp) material.GetFloat(PropeNameVColBlendMode));
        }
        
        private static void SetupBlendMode(Material material, UniUnlitRenderMode renderMode,
            bool isRenderModeChangedByUser = false)
        {
            switch (renderMode)
            {
                case UniUnlitRenderMode.Opaque:
                    material.SetOverrideTag(TagRenderTypeKey, TagRenderTypeValueOpaque);
                    material.SetInt(PropNameSrcBlend, (int) BlendMode.One);
                    material.SetInt(PropNameDstBlend, (int) BlendMode.Zero);
                    material.SetInt(PropNameZWrite, 1);
                    SetKeyword(material, KeywordAlphaTestOn, false);
                    SetKeyword(material, KeywordAlphaBlendOn, false);
                    if (isRenderModeChangedByUser) material.renderQueue = -1;
                    break;
                case UniUnlitRenderMode.Cutout:
                    material.SetOverrideTag(TagRenderTypeKey, TagRenderTypeValueTransparentCutout);
                    material.SetInt(PropNameSrcBlend, (int) BlendMode.One);
                    material.SetInt(PropNameDstBlend, (int) BlendMode.Zero);
                    material.SetInt(PropNameZWrite, 1);
                    SetKeyword(material, KeywordAlphaTestOn, true);
                    SetKeyword(material, KeywordAlphaBlendOn, false);
                    if (isRenderModeChangedByUser) material.renderQueue = (int) RenderQueue.AlphaTest;
                    break;
                case UniUnlitRenderMode.Transparent:
                    material.SetOverrideTag(TagRenderTypeKey, TagRenderTypeValueTransparent);
                    material.SetInt(PropNameSrcBlend, (int) BlendMode.SrcAlpha);
                    material.SetInt(PropNameDstBlend, (int) BlendMode.OneMinusSrcAlpha);
                    material.SetInt(PropNameZWrite, 0);
                    SetKeyword(material, KeywordAlphaTestOn, false);
                    SetKeyword(material, KeywordAlphaBlendOn, true);
                    if (isRenderModeChangedByUser) material.renderQueue = (int) RenderQueue.Transparent;
                    break;
                case UniUnlitRenderMode.TransparentWithZWrite:
                    material.SetOverrideTag(TagRenderTypeKey, TagRenderTypeValueTransparent);
                    material.SetInt(PropNameSrcBlend, (int) BlendMode.SrcAlpha);
                    material.SetInt(PropNameDstBlend, (int) BlendMode.OneMinusSrcAlpha);
                    material.SetInt(PropNameZWrite, 1);
                    SetKeyword(material, KeywordAlphaTestOn, false);
                    SetKeyword(material, KeywordAlphaBlendOn, true);
                    if (isRenderModeChangedByUser) material.renderQueue = (int) RenderQueue.Transparent;
                    break;
            }
        }
        
        private static void SetupVertexColorBlendOp(Material material, UniUnlitVertexColorBlendOp vColBlendOp)
        {
            switch (vColBlendOp)
            {
                case UniUnlitVertexColorBlendOp.None:
                    SetKeyword(material, KeywordVertexColMul, false);
                    SetKeyword(material, KeywordVertexColAdd, false);
                    break;
                case UniUnlitVertexColorBlendOp.Multiply:
                    SetKeyword(material, KeywordVertexColMul, true);
                    SetKeyword(material, KeywordVertexColAdd, false);
                    break;
                case UniUnlitVertexColorBlendOp.Additive:
                    SetKeyword(material, KeywordVertexColMul, false);
                    SetKeyword(material, KeywordVertexColAdd, true);
                    break;
            }
        }
        
        private static void SetKeyword(Material mat, string keyword, bool required)
        {
            if (required)
                mat.EnableKeyword(keyword);
            else
                mat.DisableKeyword(keyword);
        }
    }
}
