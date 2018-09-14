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
        
        private Dictionary<string, MaterialProperty> _properties = new Dictionary<string, MaterialProperty>();

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
            if (material.HasProperty(PropNameBlendMode)) // from MToon shader
            {
                blendMode = (UniUnlitRenderMode) material.GetFloat(PropNameBlendMode);
            }
            else if (material.HasProperty("_Mode")) // from Standard shader
            {
                blendMode = (UniUnlitRenderMode) Math.Min(2f, material.GetFloat("_Mode"));
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
                    material.SetOverrideTag("RenderType", "Opaque");
                    material.SetInt("_SrcBlend", (int) BlendMode.One);
                    material.SetInt("_DstBlend", (int) BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    SetKeyword(material, "_ALPHATEST_ON", false);
                    SetKeyword(material, "_ALPHABLEND_ON", false);
                    if (isRenderModeChangedByUser) material.renderQueue = -1;
                    break;
                case UniUnlitRenderMode.Cutout:
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetInt("_SrcBlend", (int) BlendMode.One);
                    material.SetInt("_DstBlend", (int) BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    SetKeyword(material, "_ALPHATEST_ON", true);
                    SetKeyword(material, "_ALPHABLEND_ON", false);
                    if (isRenderModeChangedByUser) material.renderQueue = (int) RenderQueue.AlphaTest;
                    break;
                case UniUnlitRenderMode.Transparent:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int) BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int) BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    SetKeyword(material, "_ALPHATEST_ON", false);
                    SetKeyword(material, "_ALPHABLEND_ON", true);
                    if (isRenderModeChangedByUser) material.renderQueue = (int) RenderQueue.Transparent;
                    break;
                case UniUnlitRenderMode.TransparentWithZWrite:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int) BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int) BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 1);
                    SetKeyword(material, "_ALPHATEST_ON", false);
                    SetKeyword(material, "_ALPHABLEND_ON", true);
                    if (isRenderModeChangedByUser) material.renderQueue = (int) RenderQueue.Transparent;
                    break;
            }
        }
        
        private static void SetupVertexColorBlendOp(Material material, UniUnlitVertexColorBlendOp vColBlendOp)
        {
            switch (vColBlendOp)
            {
                case UniUnlitVertexColorBlendOp.None:
                    SetKeyword(material, "_VERTEXCOL_MUL", false);
                    SetKeyword(material, "_VERTEXCOL_ADD", false);
                    break;
                case UniUnlitVertexColorBlendOp.Multiply:
                    SetKeyword(material, "_VERTEXCOL_MUL", true);
                    SetKeyword(material, "_VERTEXCOL_ADD", false);
                    break;
                case UniUnlitVertexColorBlendOp.Additive:
                    SetKeyword(material, "_VERTEXCOL_MUL", false);
                    SetKeyword(material, "_VERTEXCOL_ADD", true);
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
