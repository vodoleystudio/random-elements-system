#if UNITY_EDITOR

using RandomElementsSystem.Types;

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace RandomElementsSystem.Editor
{
    [CustomPropertyDrawer(typeof(SelectiveRandomWeightPropertyBase<,>), true)]
    public class SelectiveRandomWeightPropertyBasePropertyDrawer : PropertyDrawer
    {
        protected bool _isEqualWeightForAllItems;
        protected SerializedProperty _selectableValues;
        private ReorderableList _reorderableList;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnsureReorderableList(property);

            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width,
                EditorGUI.GetPropertyHeight(property, label, false)), property, label, false);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var rect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                    position.width, EditorGUIUtility.singleLineHeight);

                var isUseEachItemOncePerCycle = property.FindPropertyRelative("_isUseEachItemOncePerCycle");
                EditorGUI.PropertyField(rect, isUseEachItemOncePerCycle);

                rect.y += EditorGUI.GetPropertyHeight(isUseEachItemOncePerCycle, true) + EditorGUIUtility.standardVerticalSpacing;

                var isEqualWeightForAllItems = property.FindPropertyRelative("_isEqualWeightForAllItems");
                EditorGUI.PropertyField(rect, isEqualWeightForAllItems);
                _isEqualWeightForAllItems = isEqualWeightForAllItems.boolValue;

                rect.y += EditorGUI.GetPropertyHeight(isEqualWeightForAllItems, true) + EditorGUIUtility.standardVerticalSpacing;

                _selectableValues.isExpanded = true;
                var listRect = new Rect(rect.x, rect.y, rect.width, _reorderableList.GetHeight());
                _reorderableList.DoList(listRect);

                if (_selectableValues.isExpanded)
                {
                    var probRect = new Rect(listRect.x + 150, listRect.y + EditorGUIUtility.singleLineHeight + 7f,
                        listRect.width, EditorGUIUtility.singleLineHeight);
                    var minProbability = GetMinProbability();
                    var maxProbability = GetMaxProbability();
                    for (int i = 0; i < _selectableValues.arraySize; i++)
                    {
                        var element = _selectableValues.GetArrayElementAtIndex(i);

                        EditorGUI.LabelField(probRect, new GUIContent("Probability : "));

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

                        EditorGUI.LabelField(new Rect(probRect.x + 70, probRect.y + 2, probRect.width, probRect.height),
                            new GUIContent(probability + "%"), style);

                        probRect.y += _reorderableList.elementHeightCallback != null
                            ? _reorderableList.elementHeightCallback(i)
                            : _reorderableList.elementHeight;
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        private void EnsureReorderableList(SerializedProperty property)
        {
            _selectableValues = property.FindPropertyRelative("_selectableValues");
            if (_reorderableList != null && _reorderableList.serializedProperty == _selectableValues)
            {
                return;
            }

            _reorderableList = new ReorderableList(property.serializedObject, _selectableValues, true, true, true, true);

            _reorderableList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, _selectableValues.displayName);
            };

            _reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = _selectableValues.GetArrayElementAtIndex(index);
                rect.height = EditorGUI.GetPropertyHeight(element, true);
                EditorGUI.PropertyField(rect, element, GUIContent.none, true);
            };

            _reorderableList.elementHeightCallback = index =>
                EditorGUI.GetPropertyHeight(_selectableValues.GetArrayElementAtIndex(index), true) + 4f;

            _reorderableList.onAddCallback = list =>
            {
                var array = list.serializedProperty;
                array.InsertArrayElementAtIndex(list.count);
                var newElement = array.GetArrayElementAtIndex(list.count - 1);
                var weight = newElement.FindPropertyRelative("_weight");
                weight.floatValue = list.count == 1 ? 1f : 0f;
                array.serializedObject.ApplyModifiedProperties();
            };
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
            EnsureReorderableList(property);

            var height = EditorGUI.GetPropertyHeight(property, label, false);
            if (property.isExpanded)
            {
                var isUseEachItemOncePerCycle = property.FindPropertyRelative("_isUseEachItemOncePerCycle");
                var isEqualWeightForAllItems = property.FindPropertyRelative("_isEqualWeightForAllItems");
                height += EditorGUI.GetPropertyHeight(isUseEachItemOncePerCycle, true) + EditorGUIUtility.standardVerticalSpacing;
                height += EditorGUI.GetPropertyHeight(isEqualWeightForAllItems, true) + EditorGUIUtility.standardVerticalSpacing;
                height += _reorderableList.GetHeight();
            }

            return height;
        }
    }
}

#endif
