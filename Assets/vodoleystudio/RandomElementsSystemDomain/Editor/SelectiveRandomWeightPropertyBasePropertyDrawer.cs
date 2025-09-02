#if UNITY_EDITOR

using RandomElementsSystem.Types;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace RandomElementsSystem.Editor
{
    [CustomPropertyDrawer(typeof(SelectiveRandomWeightPropertyBase<,>), true)]
    public class SelectiveRandomWeightPropertyBasePropertyDrawer : PropertyDrawer
    {
        protected bool _isEqualWeightForAllItems;
        protected SerializedProperty _selectableValues;
        protected readonly Dictionary<string, int> _propertyToArraySize = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);

            var isEqualWeightForAllItems = property.FindPropertyRelative("_isEqualWeightForAllItems");
            _isEqualWeightForAllItems = isEqualWeightForAllItems.boolValue;

            _selectableValues = property.FindPropertyRelative("_selectableValues");
            SetDefaultWeightForNewElements(property);

            if (property.isExpanded && _selectableValues.isExpanded)
            {
                var rect = new Rect(position.x + 150, position.y + 84.6f, position.width, EditorGUIUtility.singleLineHeight);
                var minProbability = GetMinProbability();
                var maxProbability = GetMaxProbability();
                for (int i = 0; i < _selectableValues.arraySize; i++)
                {
                    var element = _selectableValues.GetArrayElementAtIndex(i);

                    EditorGUI.LabelField(rect, new GUIContent("Probability : "));

                    var probability = GetProbabilityFor(i);
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
        }

            protected float GetMinProbability()
            {
                var minValue = float.MaxValue;
                for (int i = 0; i < _selectableValues.arraySize; i++)
                {
                    var probability = GetProbabilityFor(i);
                    if (minValue > probability)
                    {
                        minValue = probability;
                    }
                }
                return minValue;
            }

            protected float GetMaxProbability()
            {
                var maxValue = 0f;
                for (int i = 0; i < _selectableValues.arraySize; i++)
                {
                    var probability = GetProbabilityFor(i);
                    if (maxValue < probability)
                    {
                        maxValue = probability;
                    }
                }
                return maxValue;
            }

            protected float GetProbabilityFor(int index)
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

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private void SetDefaultWeightForNewElements(SerializedProperty property)
        {
            if (_selectableValues == null)
            {
                return;
            }

            var propertyKey = property.propertyPath;
            _propertyToArraySize.TryGetValue(propertyKey, out var previousSize);

            var currentSize = _selectableValues.arraySize;
            if (currentSize > previousSize)
            {
                for (int i = previousSize; i < currentSize; i++)
                {
                    var element = _selectableValues.GetArrayElementAtIndex(i);
                    var weight = element.FindPropertyRelative("_weight");
                    weight.floatValue = currentSize == 1 && i == 0 ? 1f : 0f;
                }

                property.serializedObject.ApplyModifiedProperties();
            }

            _propertyToArraySize[propertyKey] = currentSize;
        }
    }
}

#endif