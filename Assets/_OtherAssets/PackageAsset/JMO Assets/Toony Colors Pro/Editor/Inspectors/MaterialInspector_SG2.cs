// Toony Colors Pro+Mobile 2
// (c) 2014-2021 Jean Moreno

//Enable this to display the default Inspector (in case the custom Inspector is broken)
//#define SHOW_DEFAULT_INSPECTOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ToonyColorsPro.Utilities;
using UnityEngine.Rendering;
using RenderingMode = ToonyColorsPro.ShaderGenerator.MaterialInspector_Hybrid.RenderingMode;

// Custom material inspector for generated shader

namespace ToonyColorsPro
{
	namespace ShaderGenerator
	{
		public class MaterialInspector_SG2 : ShaderGUI
		{
			//Properties
			private Material targetMaterial { get { return (_materialEditor == null) ? null : _materialEditor.target as Material; } }
			private MaterialEditor _materialEditor;
			private MaterialProperty[] _properties;
			private Stack<bool> toggledGroups = new Stack<bool>();
			private bool hasAutoTransparency;

			//--------------------------------------------------------------------------------------------------

			public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
			{
				_materialEditor = materialEditor;
				_properties = properties;

				hasAutoTransparency = System.Array.Exists(_properties, prop => prop.name == PROP_RENDERING_MODE);

#if SHOW_DEFAULT_INSPECTOR
		base.OnGUI();
		return;
#else

				//Header
				EditorGUILayout.BeginHorizontal();
				var label = (Screen.width > 450f) ? "TOONY COLORS PRO 2 - INSPECTOR (Generated Shader)" : (Screen.width > 300f ? "TOONY COLORS PRO 2 - INSPECTOR" : "TOONY COLORS PRO 2");
				TCP2_GUI.HeaderBig(label);
				if(TCP2_GUI.Button(TCP2_GUI.CogIcon2, "SG2", "Open in Shader Generator"))
				{
					if(targetMaterial.shader != null)
					{
						ShaderGenerator2.OpenWithShader(targetMaterial.shader);
					}
				}
				EditorGUILayout.EndHorizontal();
				TCP2_GUI.Separator();

				//Iterate Shader properties
				materialEditor.serializedObject.Update();
				var mShader = materialEditor.serializedObject.FindProperty("m_Shader");
				toggledGroups.Clear();

				// Auto-transparency
				if (hasAutoTransparency)
				{
					int indent = EditorGUI.indentLevel;
					EditorGUI.indentLevel++;
					{
						EditorGUILayout.BeginHorizontal();
						{
							GUILayout.Space(15);
							GUILayout.Label(TCP2_GUI.TempContent("Transparency"), EditorStyles.boldLabel);
						}
						EditorGUILayout.EndHorizontal();
						HandleRenderingMode();
					}
					EditorGUI.indentLevel = indent;
				}

				if (materialEditor.isVisible && !mShader.hasMultipleDifferentValues && mShader.objectReferenceValue != null)
				{
					//Retina display fix
					EditorGUIUtility.labelWidth = Utils.ScreenWidthRetina - 120f;
					EditorGUIUtility.fieldWidth = 64f;

					EditorGUI.BeginChangeCheck();

					EditorGUI.indentLevel++;
					foreach(var p in properties)
					{
						var visible = (toggledGroups.Count == 0 || toggledGroups.Peek());

						//Hacky way to separate material inspector properties into foldout groups
						if(p.name.StartsWith("__BeginGroup"))
						{
							//Foldout
							if(visible)
							{
								GUILayout.Space(2f);
								Rect propertyRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, EditorStyles.layerMaskField);
								propertyRect.x += 12;
								propertyRect.width -= 12;
								p.floatValue = EditorGUI.Foldout(propertyRect, p.floatValue > 0, p.displayName, true) ? 1 : 0;
							}

							EditorGUI.indentLevel++;
							toggledGroups.Push((p.floatValue > 0) && visible);
						}
						else if(p.name.StartsWith("__EndGroup"))
						{
							EditorGUI.indentLevel--;
							toggledGroups.Pop();
							GUILayout.Space(2f);
						}
						else
						{
							//Draw regular property
							if (visible && (p.flags & (MaterialProperty.PropFlags.PerRendererData | MaterialProperty.PropFlags.HideInInspector)) == MaterialProperty.PropFlags.None)
							{
								_materialEditor.ShaderProperty(p, p.displayName);
							}
						}
					}
					EditorGUI.indentLevel--;

					if(EditorGUI.EndChangeCheck())
					{
						materialEditor.PropertiesChanged();
					}
				}

#endif     // !SHOW_DEFAULT_INSPECTOR

#if UNITY_5_5_OR_NEWER
				TCP2_GUI.Separator();
				materialEditor.RenderQueueField();
#endif
#if UNITY_5_6_OR_NEWER
				materialEditor.EnableInstancingField();
#endif
			}

			// Auto-transparency handling

			const string PROP_RENDERING_MODE = "_RenderingMode";
			const string PROP_ZWRITE = "_ZWrite";
			const string PROP_BLEND_SRC = "_SrcBlend";
			const string PROP_BLEND_DST = "_DstBlend";
			const string PROP_CULLING = "_Cull";

			void HandleRenderingMode()
			{
				bool showMixed = EditorGUI.showMixedValue;
				var renderingModeProp = FindProperty(PROP_RENDERING_MODE, _properties);
				EditorGUI.showMixedValue = renderingModeProp.hasMixedValue;
				{
					EditorGUILayout.BeginHorizontal();
					{
						EditorGUILayout.PrefixLabel(TCP2_GUI.TempContent("Rendering Mode"));
						GUILayout.FlexibleSpace();
						var newRenderingMode = (RenderingMode)EditorGUILayout.EnumPopup(GUIContent.none, (RenderingMode)renderingModeProp.floatValue, GUILayout.Width(118));
						if ((float)newRenderingMode != renderingModeProp.floatValue)
						{
							Undo.RecordObjects(this._materialEditor.targets, "Change Material Rendering Mode");
							SetRenderingMode(newRenderingMode);
						}
					}
					EditorGUILayout.EndHorizontal();
				}
				EditorGUI.showMixedValue = showMixed;
			}

			void SetRenderingMode(RenderingMode mode)
			{
				switch (mode)
				{
					case RenderingMode.Opaque:
						SetRenderQueue(RenderQueue.Geometry);
						//SetCulling(Culling.Back);
						SetZWrite(true);
						SetBlending(BlendFactor.One, BlendFactor.Zero);
						IterateMaterials(mat => mat.DisableKeyword("_ALPHAPREMULTIPLY_ON"));
						IterateMaterials(mat => mat.DisableKeyword("_ALPHABLEND_ON"));
						break;

					case RenderingMode.Fade:
						SetRenderQueue(RenderQueue.Transparent);
						//SetCulling(Culling.Off);
						SetZWrite(false);
						SetBlending(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha);
						IterateMaterials(mat => mat.DisableKeyword("_ALPHAPREMULTIPLY_ON"));
						IterateMaterials(mat => mat.EnableKeyword("_ALPHABLEND_ON"));
						break;

					case RenderingMode.Transparent:
						SetRenderQueue(RenderQueue.Transparent);
						//SetCulling(Culling.Off);
						SetZWrite(false);
						SetBlending(BlendFactor.One, BlendFactor.OneMinusSrcAlpha);
						IterateMaterials(mat => mat.EnableKeyword("_ALPHAPREMULTIPLY_ON"));
						IterateMaterials(mat => mat.DisableKeyword("_ALPHABLEND_ON"));
						break;
				}
				IterateMaterials(mat => mat.SetFloat(PROP_RENDERING_MODE, (float)mode));
			}

			void SetZWrite(bool enable)
			{
				IterateMaterials(mat => mat.SetFloat(PROP_ZWRITE, enable ? 1.0f : 0.0f));
			}

			void SetRenderQueue(RenderQueue queue)
			{
				IterateMaterials(mat => mat.renderQueue = (int)queue);
			}

			void SetCulling(Culling culling)
			{
				IterateMaterials(mat => mat.SetFloat(PROP_CULLING, (float)culling));
			}

			void SetBlending(BlendFactor src, BlendFactor dst)
			{
				IterateMaterials(mat => mat.SetFloat(PROP_BLEND_SRC, (float)src));
				IterateMaterials(mat => mat.SetFloat(PROP_BLEND_DST, (float)dst));
			}

			void IterateMaterials(System.Action<Material> action)
			{
				foreach (var target in this._materialEditor.targets)
				{
					action(target as Material);
				}
			}
		}
	}
}