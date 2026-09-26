using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(MovingObject))]
public class MovingObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MovingObject movingObject = (MovingObject)target;

        if (GUILayout.Button("Add Node"))
        {
            Undo.IncrementCurrentGroup();
            int undoGroupIndex = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Moving Object - Add Node");
            GameObject newNode = new GameObject();
            Undo.RegisterCreatedObjectUndo(newNode, "Moving Object - Create Node");
            Undo.RecordObject(newNode, "Moving Object - Configure Moving Object");
            Undo.RecordObject(movingObject.transform.parent, "Moving Object - Add Node as child");
            newNode.name = movingObject.name + " Node" + movingObject.nodes.Count;
            Undo.SetTransformParent(newNode.transform, movingObject.transform.parent, "Moving Object - Add Node transform as Child");
            Undo.RecordObject(newNode.transform, "Moving Object - Move new node");
            newNode.transform.position = new Vector3(
                SceneView.lastActiveSceneView.camera.transform.position.x,
                SceneView.lastActiveSceneView.camera.transform.position.y,
                0
            );
            newNode.transform.localScale = Vector3.one;
            MovingObjectNode nodeIcon = Undo.AddComponent<MovingObjectNode>(newNode);
            nodeIcon.nodeColor = movingObject.nodeColor;
            nodeIcon.nodeIcon = movingObject.movingObjectIcon;
            nodeIcon.movementCurve = movingObject.defaultMovementCurve;
            nodeIcon.SetMovingObject(movingObject);
            Undo.RecordObject(movingObject, "Moving Object - Add Node to Moving Object list");
            movingObject.nodes.Add(nodeIcon);
            PrefabUtility.RecordPrefabInstancePropertyModifications(movingObject);
            EditorUtility.SetDirty(movingObject);
            Undo.CollapseUndoOperations(undoGroupIndex);
        }
    }
}