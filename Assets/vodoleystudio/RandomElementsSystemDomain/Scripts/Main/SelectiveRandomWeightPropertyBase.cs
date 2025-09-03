using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Random = UnityEngine.Random;

namespace RandomElementsSystem.Types
{
    [Serializable]
    public class SelectiveRandomWeightPropertyBase<T, V> : RandomPropertyBase<T> where V : WeightProperty<T>
    {
        /// <summary>
        /// Set this flag to true if you want to use each item once per cycle. (non-repetitions random during each cycle)
        /// Probability of remain items after each GetRandomValue() call will be recalculated.
        /// Cycle is a period returning all items from the list.
        /// After the cycle is over, the list of items reseted to the initial state.
        /// </summary>
        [SerializeField]
        [JsonProperty]
        private bool _isUseEachItemOncePerCycle = false;

        /// <summary>
        /// Set this flag to true if you want that all items have equal weight. (equal probability)
        /// If true, the weight of each item will be ignored.
        /// </summary>
        [SerializeField]
        [JsonProperty]
        private bool _isEqualWeightForAllItems = false;

        /// <summary>
        /// Keeps all selectable values.
        /// </summary>
        [SerializeField]
        [JsonProperty]
        private List<V> _selectableValues = new List<V>();

        /// <summary>
        /// Keeps all used selectable values. Affected only if _isUseEachItemOncePerCycle is true.
        /// Cleared after each cycle.
        /// </summary>
        [JsonProperty]
        private List<V> _usedSelectableValues = new List<V>();

        /// <summary>
        /// Do not use this default constructor. It is used only for serialization.
        /// </summary>
        public SelectiveRandomWeightPropertyBase()
        {
        }

        /// <summary>
        /// Creates new instance of SelectiveRandomWeightPropertyBase with equal weight for all items.
        /// </summary>
        /// <param name="selectableValues">T types items</param>
        /// <param name="isUseEachItemOncePerCycle">Set this flag to true if you want to use each item once per cycle. (non-repetitions random during each cycle). More info in _isUseEachItemOncePerCycle comment.</param>
        public SelectiveRandomWeightPropertyBase(IEnumerable<T> selectableValues, bool isUseEachItemOncePerCycle) : this(GetValues(selectableValues), isUseEachItemOncePerCycle, true)
        {
        }

        /// <summary>
        /// Creates new instance of SelectiveRandomWeightPropertyBase from collection of T values and their weights.
        /// </summary>
        /// <param name="selectableValues">Collection of T type items as Keys and their weights as values</param>
        /// <param name="isUseEachItemOncePerCycle">Set this flag to true if you want to use each item once per cycle. (non-repetitions random during each cycle). More info in _isUseEachItemOncePerCycle comment.</param>
        public SelectiveRandomWeightPropertyBase(ICollection<KeyValuePair<T, float>> selectableValues, bool isUseEachItemOncePerCycle) : this(GetValues(selectableValues), isUseEachItemOncePerCycle, false)
        {
        }

        /// <summary>
        /// Creates new instance of SelectiveRandomWeightPropertyBase from collection of WeightProperty<T> and their weights.
        /// </summary>
        /// <param name="selectableValues">Collection of WeightProperty<T> items</param>
        /// <param name="isUseEachItemOncePerCycle">Set this flag to true if you want to use each item once per cycle. (non-repetitions random during each cycle). More info in _isUseEachItemOncePerCycle comment.</param>
        /// <param name="isEqualWeightForAllItems">Set this flag to true if you want that all items have equal weight.</param>
        public SelectiveRandomWeightPropertyBase(IEnumerable<V> selectableValues, bool isUseEachItemOncePerCycle, bool isEqualWeightForAllItems)
        {
            _selectableValues.AddRange(selectableValues);
            _isEqualWeightForAllItems = isEqualWeightForAllItems;
            _isUseEachItemOncePerCycle = isUseEachItemOncePerCycle;
        }

        /// <summary>
        /// Get all WeightProperty<T> from collection with their weights.
        /// </summary>
        /// <returns>Collection of WeightProperty<T> as Keys and their weights as Values</returns>
        public IReadOnlyDictionary<V, float> GetWeightPropertyToProbabilityCollection()
        {
            var weightPropertyToProbabilityCollection = _selectableValues.ToDictionary(x => x, x => GetProbabilityByIndex(_selectableValues.IndexOf(x)));
            foreach (var item in _usedSelectableValues)
            {
                weightPropertyToProbabilityCollection.Add(item, WeightProperty<T>.MinWeight);
            }

            return weightPropertyToProbabilityCollection;
        }

        /// <summary>
        /// Get all T items from collection with their weights.
        /// If there are duplicates - return only as one key-value pair with sum of weights
        /// </summary>
        /// <returns>Collection of T items as Keys and their weights as Values</returns>
        public IReadOnlyDictionary<T, float> GetValueToProbabilityCollection()
        {
            var valueToProbabilityCollection = new Dictionary<T, float>();
            foreach (var selectableValue in _selectableValues)
            {
                if (!valueToProbabilityCollection.ContainsKey(selectableValue.Value))
                {
                    valueToProbabilityCollection[selectableValue.Value] = GetProbabilityByIndex(_selectableValues.IndexOf(selectableValue));
                }
                else
                {
                    valueToProbabilityCollection[selectableValue.Value] += GetProbabilityByIndex(_selectableValues.IndexOf(selectableValue));
                }
            }

            foreach (var usedSelectableValue in _usedSelectableValues)
            {
                if (!valueToProbabilityCollection.ContainsKey(usedSelectableValue.Value))
                {
                    valueToProbabilityCollection[usedSelectableValue.Value] = WeightProperty<T>.MinWeight;
                }
            }

            return valueToProbabilityCollection;
        }

        protected override T GenerateRandomValue()
        {
            if (_selectableValues.Count == 0)
            {
                return default;
            }

            var weightMin = WeightProperty<T>.MinWeight;
            var weightSum = weightMin;

            // calculate total weight
            foreach (var selectableValue in _selectableValues)
            {
                weightSum += GetCalculatedWeightStep(selectableValue);
            }

            var random = Random.Range(WeightProperty<T>.MinWeight, weightSum);

            // find related item for random value
            V randomWeightProperty = default;
            foreach (var selectableValue in _selectableValues)
            {
                var weightStep = GetCalculatedWeightStep(selectableValue);
                var maxRandom = weightMin + weightStep;
                if ((random > weightMin || Mathf.Approximately(random, weightMin)) && (random < maxRandom || Mathf.Approximately(random, maxRandom)))
                {
                    randomWeightProperty = selectableValue;
                    break;
                }

                // move to next range
                weightMin += weightStep;
            }

            if (_isUseEachItemOncePerCycle)
            {
                // if we use each item once per cycle, we need to remove it from selectable values for using only once in current cycle
                if (_selectableValues.Count > 1)
                {
                    _selectableValues.Remove(randomWeightProperty);
                    _usedSelectableValues.Add(randomWeightProperty);
                }
                // reset to initial state
                else
                {
                    _selectableValues.AddRange(_usedSelectableValues);
                    _usedSelectableValues.Clear();
                }
            }

            return randomWeightProperty.Value;
        }

        /// <summary>
        /// Returns probability of item by index from _selectableValues collection.
        /// This is a private method as I did not provide an API that returns the number of elements in the configured collection as I didn't find the point in doing so.
        /// </summary>
        /// <param name="index">index in _selectableValues collection</param>
        /// <returns>Weight of item</returns>
        private float GetProbabilityByIndex(int index)
        {
            if (index < 0 || index >= _selectableValues.Count)
            {
                return float.NaN;
            }

            var weightSum = WeightProperty<T>.MinWeight;
            foreach (var selectableValue in _selectableValues)
            {
                weightSum += GetCalculatedWeightStep(selectableValue);
            }

            if (Mathf.Approximately(weightSum, WeightProperty<T>.MinWeight))
            {
                return float.NaN;
            }

            return GetCalculatedWeightStep(_selectableValues[index]) / weightSum * 100f;
        }

        private float GetCalculatedWeightStep(V selectableValue)
        {
            return _isEqualWeightForAllItems ? WeightProperty<T>.DefaultWeight : selectableValue.Weight;
        }

        /// <summary>
        /// Used only for constructor.
        /// </summary>
        /// <param name="selectableValues"></param>
        /// <returns></returns>
        private static IList<V> GetValues(IEnumerable<T> selectableValues)
        {
            var properties = new List<V>();
            foreach (var item in selectableValues)
            {
                properties.Add(GetNewV(item, WeightProperty<T>.DefaultWeight));
            }

            return properties;
        }

        /// <summary>
        /// Used only for constructor.
        /// </summary>
        /// <param name="selectableValues"></param>
        /// <returns></returns>
        private static IList<V> GetValues(ICollection<KeyValuePair<T, float>> selectableValues)
        {
            var properties = new List<V>();
            foreach (var item in selectableValues)
            {
                properties.Add(GetNewV(item.Key, item.Value));
            }

            return properties;
        }

        /// <summary>
        /// Creates new instance of V with T value and float weight.
        /// </summary>
        /// <param name="value">T type value</param>
        /// <param name="weight">Weight of value</param>
        /// <returns>V type with T value and their weight</returns>
        private static V GetNewV(T value, float weight)
        {
            return (V)Activator.CreateInstance(typeof(V), value, weight);
        }
    }
}