using UnityEngine;
using UnityEditor;

namespace UTJ.HumbleNormalEditor
{
    public class NormalEditorWindow : EditorWindow
    {
        public static bool isOpen;
        Vector2 m_scrollPos;
        NormalEditor m_target;
        MeshRenderer m_mr;

        bool foldEdit = true;
        bool foldMisc = true;
        bool foldInExport = false;
        bool foldDisplay = true;
        int displayIndex;
        int inexportIndex;

        Vector3 setValue = Vector3.up;
        Vector3 moveAmount;
        Vector3 rotateAmount;
        Vector3 scaleAmount;
        float equalizeRadius = 0.5f;
        float equalizeAmount = 1.0f;
        GameObject projector;



        [MenuItem("Window/Normal Editor")]
        public static void Open()
        {
            var window = EditorWindow.GetWindow<NormalEditorWindow>();
            window.titleContent = new GUIContent("Normal Editor");
            window.Show();
            window.OnSelectionChange();
        }



        private void OnEnable()
        {
            isOpen = true;
            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
            isOpen = false;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if(m_target != null)
            {
                int ret = m_target.OnSceneGUI();
                if ((ret & (int)SceneGUIState.Repaint) != 0)
                    RepaintAllViews();
            }
        }

        private void OnGUI()
        {
            if (m_target != null)
            {
                if(!m_target.isActiveAndEnabled)
                {
                    EditorGUILayout.LabelField("(Enable " + m_target.name + " to show Normal Editor)");
                }
                else
                {
                    m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
                    DrawNormalEditor();
                    EditorGUILayout.EndScrollView();
                }
            }
            else if(m_mr != null)
            {
                if (GUILayout.Button("Add Normal Editor to " + m_mr.name))
                {
                    m_mr.gameObject.AddComponent<NormalEditor>();
                    OnSelectionChange();
                }
            }
        }

        private void OnSelectionChange()
        {
            m_target = null;
            m_mr = null;
            if (Selection.activeGameObject != null)
            {
                m_target = Selection.activeGameObject.GetComponent<NormalEditor>();
                m_mr = Selection.activeGameObject.GetComponent<MeshRenderer>();
            }
            Repaint();
        }






        void RepaintAllViews()
        {
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        static readonly int indentSize = 18;
        static readonly int spaceSize = 5;
        static readonly int c1Width = 100;

        static readonly string[] strBrushTypes = new string[] {
            "Paint",
            "Scale",
            "Equalize",
            "Reset",
        };
        static readonly string[] strSelectMode = new string[] {
            "Single",
            "Rect",
            "Brush",
        };

        static readonly string[] strCommands = new string[] {
            "Selection",
            "Brush",
            "Assign",
            "Move",
            "Rotate",
            "Scale",
            "Equalize",
            "Projection",
            "Reset",
        };

        void DrawEditPanel()
        {
            var settings = m_target.settings;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(indentSize));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(c1Width));
            settings.editMode = (EditMode)GUILayout.SelectionGrid((int)settings.editMode, strCommands, 1);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(spaceSize));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();

            if (settings.editMode == EditMode.Select)
            {
                settings.selectMode = (SelectMode)GUILayout.SelectionGrid((int)settings.selectMode, strSelectMode, 3);
                EditorGUILayout.Space();
                if (settings.selectMode == SelectMode.Brush)
                {
                    settings.brushRadius = EditorGUILayout.Slider("Brush Radius", settings.brushRadius, 0.01f, 1.0f);
                    settings.brushStrength = EditorGUILayout.Slider("Brush Strength", settings.brushStrength, 0.01f, 1.0f);
                    settings.brushFalloff = EditorGUILayout.Slider("Brush Falloff", settings.brushFalloff, 0.01f, 1.0f);
                }
                else
                {
                    settings.selectFrontSideOnly = EditorGUILayout.Toggle("Front Side Only", settings.selectFrontSideOnly);
                }
                EditorGUILayout.Space();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Save", GUILayout.Width(50));
                for (int i = 0; i < 5; ++i)
                {
                    if (GUILayout.Button((i + 1).ToString()))
                        settings.selectionSets[i].selection = m_target.selection;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Load", GUILayout.Width(50));
                for (int i = 0; i < 5; ++i)
                {
                    if (GUILayout.Button((i + 1).ToString()))
                        m_target.selection = settings.selectionSets[i].selection;
                }
                GUILayout.EndHorizontal();

                EditorGUILayout.Space();

                GUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUI.indentLevel * indentSize);
                if (GUILayout.Button("Select All"))
                {
                    if (m_target.SelectAll())
                        m_target.UpdateSelection();
                }
                if (GUILayout.Button("Clear Selection"))
                {
                    if (m_target.ClearSelection())
                        m_target.UpdateSelection();
                }
                GUILayout.EndHorizontal();
            }
            else if (settings.editMode == EditMode.Brush)
            {
                settings.brushMode = (BrushMode)GUILayout.SelectionGrid((int)settings.brushMode, strBrushTypes, 4);
                EditorGUILayout.Space();
                settings.brushRadius = EditorGUILayout.Slider("Brush Radius", settings.brushRadius, 0.01f, 1.0f);
                settings.brushStrength = EditorGUILayout.Slider("Brush Strength", settings.brushStrength, -1.0f, 1.0f);
                settings.brushFalloff = EditorGUILayout.Slider("Brush Falloff", settings.brushFalloff, 0.01f, 1.0f);
                EditorGUILayout.Space();

                if (settings.brushMode == BrushMode.Paint)
                {
                    GUILayout.BeginHorizontal();
                    settings.primary = EditorGUILayout.ColorField(settings.primary, GUILayout.Width(35));
                    settings.primary = NormalEditor.ToColor(EditorGUILayout.Vector3Field("", NormalEditor.ToVector(settings.primary)));
                    settings.pickNormal = GUILayout.Toggle(settings.pickNormal, "Pick", "Button", GUILayout.Width(90));
                    GUILayout.EndHorizontal();
                }
            }
            else if (settings.editMode == EditMode.Assign)
            {
                setValue = EditorGUILayout.Vector3Field("Value", setValue);
                if (GUILayout.Button("Assign"))
                {
                    m_target.ApplySet(setValue);
                    m_target.PushUndo();
                }
            }
            else if (settings.editMode == EditMode.Move)
            {
                moveAmount = EditorGUILayout.Vector3Field("Move Amount", moveAmount);
                if (GUILayout.Button("Move"))
                {
                    m_target.ApplyMove(moveAmount);
                    m_target.PushUndo();
                }
            }
            else if (settings.editMode == EditMode.Rotate)
            {
                rotateAmount = EditorGUILayout.Vector3Field("Rotate Amount", rotateAmount);
                settings.rotatePivot = EditorGUILayout.Toggle("Pivot", settings.rotatePivot);
                if (GUILayout.Button("Rotate"))
                {
                    if (settings.rotatePivot)
                        m_target.ApplyRotatePivot(
                            Quaternion.Euler(rotateAmount.x, rotateAmount.y, rotateAmount.z), settings.pivotPos, 1.0f);
                    else
                        m_target.ApplyRotate(Quaternion.Euler(rotateAmount.x, rotateAmount.y, rotateAmount.z));
                    m_target.PushUndo();
                }
            }
            else if (settings.editMode == EditMode.Scale)
            {
                scaleAmount = EditorGUILayout.Vector3Field("Scale Amount", scaleAmount);
                if (GUILayout.Button("Scale"))
                {
                    m_target.ApplyScale(scaleAmount, settings.pivotPos);
                    m_target.PushUndo();
                }
            }
            else if (settings.editMode == EditMode.Equalize)
            {
                equalizeRadius = EditorGUILayout.FloatField("Equalize Radius", equalizeRadius);
                equalizeAmount = EditorGUILayout.FloatField("Equalize Amount", equalizeAmount);
                if (GUILayout.Button("Equalize"))
                {
                    m_target.ApplyEqualize(equalizeRadius, equalizeAmount);
                    m_target.PushUndo();
                }
            }
            else if (settings.editMode == EditMode.Projection)
            {
                projector = EditorGUILayout.ObjectField("Projector", projector, typeof(GameObject), true) as GameObject;
                if (GUILayout.Button("Project"))
                {
                    m_target.ApplyProjection(projector);
                    m_target.PushUndo();
                }
            }
            else if (settings.editMode == EditMode.Reset)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset (Selection)"))
                {
                    m_target.ResetNormals(true);
                    m_target.PushUndo();
                }
                else if (GUILayout.Button("Reset (All)"))
                {
                    m_target.ResetNormals(false);
                    m_target.PushUndo();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        void DrawMiscPanel()
        {
            var settings = m_target.settings;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(indentSize));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(c1Width));
            EditorGUILayout.LabelField("", GUILayout.Width(c1Width));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(spaceSize));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            {
                settings.mirrorMode = (MirrorMode)EditorGUILayout.EnumPopup("Mirroring", settings.mirrorMode);
                EditorGUILayout.Space();
                if (GUILayout.Button("Recalculate Tangents"))
                    m_target.RecalculateTangents();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }


        static readonly string[] strInExport = new string[] {
            "Vertex Color",
            "Bake Texture",
            "Load Texture",
            "Export .obj",
        };

        void DrawInExportPanel()
        {
            var settings = m_target.settings;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(indentSize));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(c1Width));
            inexportIndex = GUILayout.SelectionGrid(inexportIndex, strInExport, 1);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(spaceSize));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();

            if (inexportIndex == 0)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Convert To Vertex Color"))
                    m_target.BakeToVertexColor();
                if (GUILayout.Button("Convert From Vertex Color"))
                    m_target.LoadVertexColor();
                GUILayout.EndHorizontal();
            }
            else if (inexportIndex == 1)
            {
                settings.bakeFormat = (ImageFormat)EditorGUILayout.EnumPopup("Format", settings.bakeFormat);
                settings.bakeWidth = EditorGUILayout.IntField("Width", settings.bakeWidth);
                settings.bakeHeight = EditorGUILayout.IntField("Height", settings.bakeHeight);

                if (GUILayout.Button("Bake"))
                {
                    string path = settings.bakeFormat == ImageFormat.PNG ?
                        EditorUtility.SaveFilePanel("Export .png file", "", m_target.name + "_normal", "png") :
                        EditorUtility.SaveFilePanel("Export .exr file", "", m_target.name + "_normal", "exr");
                    m_target.BakeToTexture(settings.bakeWidth, settings.bakeHeight, path);
                }
            }
            else if (inexportIndex == 2)
            {
                settings.bakeSource = EditorGUILayout.ObjectField("Source Texture", settings.bakeSource, typeof(Texture), true) as Texture;

                if (GUILayout.Button("Load"))
                    m_target.LoadTexture(settings.bakeSource);
            }
            else if (inexportIndex == 3)
            {
                settings.objFlipHandedness = EditorGUILayout.Toggle("Flip Handedness", settings.objFlipHandedness);
                settings.objFlipFaces = EditorGUILayout.Toggle("Flip Faces", settings.objFlipFaces);
                settings.objMakeSubmeshes = EditorGUILayout.Toggle("Make Submeshes", settings.objMakeSubmeshes);
                settings.objApplyTransform = EditorGUILayout.Toggle("Apply Transform", settings.objApplyTransform);
                settings.objIncludeChildren = EditorGUILayout.Toggle("Include Children", settings.objIncludeChildren);

                if (GUILayout.Button("Export .obj file"))
                {
                    string path = EditorUtility.SaveFilePanel("Export .obj file", "", m_target.name, "obj");
                    ObjExporter.Export(m_target.gameObject, path, new ObjExporter.Settings
                    {
                        flipFaces = settings.objFlipFaces,
                        flipHandedness = settings.objFlipHandedness,
                        includeChildren = settings.objIncludeChildren,
                        makeSubmeshes = settings.objMakeSubmeshes,
                        applyTransform = settings.objApplyTransform,
                    });
                }

            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }


        static readonly string[] strDisplay = new string[] {
            "Display",
            "Settings",
        };

        void DrawDisplayPanel()
        {
            var settings = m_target.settings;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(indentSize));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(c1Width));
            displayIndex = GUILayout.SelectionGrid(displayIndex, strDisplay, 1);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(spaceSize));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            if (displayIndex == 0)
            {
                settings.showVertices = EditorGUILayout.Toggle("Vertices", settings.showVertices);
                settings.showNormals = EditorGUILayout.Toggle("Normals", settings.showNormals);
                settings.showTangents = EditorGUILayout.Toggle("Tangents", settings.showTangents);
                settings.showBinormals = EditorGUILayout.Toggle("Binormals", settings.showBinormals);
                EditorGUI.indentLevel++;
                settings.showSelectedOnly = EditorGUILayout.Toggle("Only Selected", settings.showSelectedOnly);
                EditorGUI.indentLevel--;
                settings.modelOverlay = (ModelOverlay)EditorGUILayout.EnumPopup("Overlay", settings.modelOverlay);
            }
            else if (displayIndex == 1)
            {
                settings.vertexSize = EditorGUILayout.Slider("Vertex Size", settings.vertexSize, 0.0f, 0.05f);
                settings.normalSize = EditorGUILayout.Slider("Normal Size", settings.normalSize, 0.0f, 1.00f);
                settings.tangentSize = EditorGUILayout.Slider("Tangent Size", settings.tangentSize, 0.0f, 1.00f);
                settings.binormalSize = EditorGUILayout.Slider("Binormal Size", settings.binormalSize, 0.0f, 1.00f);

                EditorGUILayout.Space();

                settings.vertexColor = EditorGUILayout.ColorField("Vertex Color", settings.vertexColor);
                settings.vertexColor2 = EditorGUILayout.ColorField("Vertex Color (Selected)", settings.vertexColor2);
                settings.vertexColor3 = EditorGUILayout.ColorField("Vertex Color (Highlighted)", settings.vertexColor3);
                settings.normalColor = EditorGUILayout.ColorField("Normal Color", settings.normalColor);
                settings.tangentColor = EditorGUILayout.ColorField("Tangent Color", settings.tangentColor);
                settings.binormalColor = EditorGUILayout.ColorField("Binormal Color", settings.binormalColor);
                if (GUILayout.Button("Reset"))
                {
                    settings.ResetDisplayOptions();
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }


        void DrawNormalEditor()
        {
            if (m_target == null || !m_target.isActiveAndEnabled)
                return;

            EditorGUI.BeginChangeCheck();
            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);

            EditorGUILayout.Space();

            foldEdit = EditorGUILayout.Foldout(foldEdit, "Edit");
            if (foldEdit)
                DrawEditPanel();

            foldMisc = EditorGUILayout.Foldout(foldEdit, "Misc");
            if (foldMisc)
                DrawMiscPanel();

            EditorGUILayout.Space();

            foldInExport = EditorGUILayout.Foldout(foldInExport, "Import / Export");
            if (foldInExport)
                DrawInExportPanel();

            EditorGUILayout.Space();

            foldDisplay = EditorGUILayout.Foldout(foldDisplay, "Display");
            if (foldDisplay)
                DrawDisplayPanel();

            EditorGUILayout.EndScrollView();
            if (EditorGUI.EndChangeCheck())
                RepaintAllViews();
        }
    }
}