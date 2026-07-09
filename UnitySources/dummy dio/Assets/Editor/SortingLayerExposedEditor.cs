using UnityEngine;
using UnityEditor;
using System;
using UnityEditorInternal;
using System.Reflection;

[CustomEditor(typeof(SortingLayerExposed))]
public class SortingLayerExposedEditor : Editor
{

	int id = -1;

	public override void OnInspectorGUI()
	{
		// Get the renderer from the target object
		var renderer = (target as SortingLayerExposed).gameObject.GetComponent<Renderer>();

		// If there is no renderer, we can't do anything
		if (!renderer)
		{
			return;
		}

		// Get sorting layers for drop-down
		Type internalEditorUtilityType = typeof(InternalEditorUtility);
		PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
		string[] sortingLayers = (string[])sortingLayersProperty.GetValue(null, new object[0]);

		// Expose the sorting layer name
		if (id < 0) id = Array.IndexOf(sortingLayers, renderer.sortingLayerName);
		id = EditorGUILayout.Popup("Sorting Layer Name", id >= 0 ? id : 0, sortingLayers);
		string newSortingLayerName = sortingLayers[id];
		if (newSortingLayerName != renderer.sortingLayerName)
		{
			Undo.RecordObject(renderer, "Edit Sorting Layer Name");
			renderer.sortingLayerName = newSortingLayerName;
			EditorUtility.SetDirty(renderer);
		}

		// Expose the sorting layer ID
		int newSortingLayerId = EditorGUILayout.IntField("Sorting Layer ID", renderer.sortingLayerID);
		if (newSortingLayerId != renderer.sortingLayerID)
		{
			Undo.RecordObject(renderer, "Edit Sorting Layer ID");
			renderer.sortingLayerID = newSortingLayerId;
			EditorUtility.SetDirty(renderer);
		}

		// Expose the manual sorting order
		int newSortingLayerOrder = EditorGUILayout.IntField("Sorting Layer Order", renderer.sortingOrder);
		if (newSortingLayerOrder != renderer.sortingOrder)
		{
			Undo.RecordObject(renderer, "Edit Sorting Order");
			renderer.sortingOrder = newSortingLayerOrder;
			EditorUtility.SetDirty(renderer);
		}
	}
}