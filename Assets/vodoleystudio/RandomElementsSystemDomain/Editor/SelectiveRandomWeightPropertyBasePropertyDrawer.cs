#if UNITY_EDITOR

using RandomElementsSystem.Types;

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace RandomElementsSystem.Editor
{
    [CustomPropertyDrawer(typeof(SelectiveRandomWeightPropertyBase<,>), true)]
    public class SelectiveRandomWeightPropertyBasePropertyDrawer : PropertyDrawer
    {
        private bool _isEqualWeightForAllItems;
        private SerializedProperty _selectableValues;

        private readonly Dictionary<string, int> _selectableValuesArraySizes = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var key = property.serializedObject.targetObject.GetInstanceID() + property.propertyPath;
            _selectableValuesArraySizes.TryGetValue(key, out var previousSize);

            EditorGUI.PropertyField(position, property, label, true);

            _selectableValues = property.FindPropertyRelative("_selectableValues");
            if (_selectableValues.arraySize > previousSize)
            {
                for (int i = previousSize; i < _selectableValues.arraySize; i++)
                {
                    var element = _selectableValues.GetArrayElementAtIndex(i);
                    var weight = element.FindPropertyRelative("_weight");
                    weight.floatValue = 0f;
                }

                property.serializedObject.ApplyModifiedProperties();
            }

            _selectableValuesArraySizes[key] = _selectableValues.arraySize;

            var isEqualWeightForAllItems = property.FindPropertyRelative("_isEqualWeightForAllItems");
            _isEqualWeightForAllItems = isEqualWeightForAllItems.boolValue;

            if (property.isExpanded && _selectableValues.isExpanded)
            {
                var rect = new Rect(position.x + 150, position.y + 84.6f, position.width, EditorGUIUtility.singleLineHeight);
                var minProbability = getMinProbability();
                var maxProbability = getMaxProbability();
                for (int i = 0; i < _selectableValues.arraySize; i++)
                {
                    var element = _selectableValues.GetArrayElementAtIndex(i);

                    EditorGUI.LabelField(rect, new GUIContent("Probability : "));

                    var probability = getProbabilityFor(i);
                    var style = new GUIStyle();

                    style.normal.textColor = Color.red;
                    if (probability > 0)
                    {
                        style.normal.textColor = Color.cyan;
                        if (Mathf.Approximately(maxProbability, probability))
                        {
                            style.normal.textColor = Color.green;
                        }
                        else if (Mathf.Approximately(minProbability, probability))
                        {
                            style.normal.textColor = new Color(1f, 0.6f, 0.04f, 1f);
                        }
                    }
                    else if (Mathf.Approximately(probability, 0f))
                    {
                        style.normal.textColor = Color.yellow;
                    }

                    EditorGUI.LabelField(new Rect(rect.x + 70, rect.y + 2, rect.width, rect.height), new GUIContent(probability + "%"), style);

                    rect.y += GetPropertyHeight(element, label) + 2f;
                }
            }

            float getMinProbability()
            {
                var minValue = float.MaxValue;
                for (int i = 0; i < _selectableValues.arraySize; i++)
                {
                    var probability = getProbabilityFor(i);
                    if (minValue > probability)
                    {
                        minValue = probability;
                    }
                }
                return minValue;
            }

            float getMaxProbability()
            {
                var maxValue = 0f;
                for (int i = 0; i < _selectableValues.arraySize; i++)
                {
                    var probability = getProbabilityFor(i);
                    if (maxValue < probability)
                    {
                        maxValue = probability;
                    }
                }
                return maxValue;
            }

            float getProbabilityFor(int index)
            {
                var value = 1f;
                var total = 0f;
                if (_isEqualWeightForAllItems)
                {
                    total = _selectableValues.arraySize;
                }
                else
                {
                    SerializedProperty element;
                    for (int i = 0; i < _selectableValues.arraySize; i++)
                    {
                        element = _selectableValues.GetArrayElementAtIndex(i);
                        element = element.FindPropertyRelative("_weight");
                        total += element.floatValue;
                    }

                    element = _selectableValues.GetArrayElementAtIndex(index);
                    element = element.FindPropertyRelative("_weight");
                    value = element.floatValue;
                }

                return value / total * 100f;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}

#endif
