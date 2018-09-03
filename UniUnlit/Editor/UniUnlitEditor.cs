using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


namespace UniGLTF
{
    public class UniUnlitEditor : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);
        }
    }
}
