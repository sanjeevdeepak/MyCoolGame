using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(MovingObjectNode))]
[CanEditMultipleObjects]
public class MovingObjectNodeEditor : Editor
{
    public void OnDestroy()
    {
        RemoveNullNodes();
    }

    public void RemoveNullNodes()
    {
        MovingObjectNode node = (MovingObjectNode)target;
        MovingObject movingObject = node.movingObject;
        bool cleanNodes = false;
        for (int i = 0; i < movingObject.nodes.Count; i++)
        {
            MovingObjectNode eachNode = movingObject.nodes[i];
            if (eachNode == null) cleanNodes = true;
            else eachNode.lastKnownIndex = i;
        }
        if (cleanNodes)
        {
            movingObject.nodes.RemoveAll(x => x == null);
            EditorUtility.SetDirty(movingObject);
        }
    }
}
