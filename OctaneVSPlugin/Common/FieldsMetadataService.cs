/*!
* (c) 2016-2018 EntIT Software LLC, a Micro Focus company
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using MicroFocus.Adm.Octane.Api.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroFocus.Adm.Octane.VisualStudio.Common
{
    /// <summary>
    /// Class for providing field metadata based on the entity type
    /// </summary>
    internal static class FieldsMetadataService
    {
        /// <summary>
        /// Converters for formatting information based on the metadata type
        /// </summary>
        private static readonly Dictionary<string, Func<object, object>> Converter = new Dictionary<string, Func<object, object>>();

        private static readonly MetadataCache Cache = new MetadataCache();

        private static OctaneServices _octaneService;

        static FieldsMetadataService()
        {
            Converter.Add("date_time", value => DateTime.Parse(value.ToString()).ToString("MM/dd/yyyy HH:mm:ss"));
        }

        /// <summary>
        /// Reset the service
        /// </summary>
        internal static void Reset()
        {
            _octaneService = null;

            Cache.Clear();
        }

        /// <summary>
        /// Initialize field metadata service
        /// </summary>
        internal static async System.Threading.Tasks.Task Initialize()
        {
            if (_octaneService != null)
                return;

            _octaneService = new OctaneServices(
                OctaneConfiguration.Url,
                OctaneConfiguration.SharedSpaceId,
                OctaneConfiguration.WorkSpaceId,
                OctaneConfiguration.Username,
                OctaneConfiguration.Password);

            await _octaneService.Connect();
        }

        /// <summary>
        /// Return the list of field metadata for the given entity type
        /// </summary>
        internal static async Task<List<FieldMetadata>> GetFieldMetadata(BaseEntity entity)
        {
            if (entity == null)
                return new List<FieldMetadata>();

            if (_octaneService == null)
                await Initialize();

            List<FieldMetadata> fields = Cache.GetFieldMetadataList(entity);
            if (fields == null)
            {
                var entityType = Utility.GetConcreteEntityType(entity);
                var fieldsMetadata = await _octaneService.GetFieldsMetadata(entityType);
                fields = fieldsMetadata.Where(fm => fm.VisibleInUI).ToList();

                Cache.Add(entity, fields);
            }

            return fields;
        }

        /// <summary>
        /// Return the formatted value for the given entity field using the property datatype
        /// </summary>
        internal static object GetFormattedValue(BaseEntity entity, string fieldName)
        {
            if (entity == null || string.IsNullOrEmpty(fieldName))
                return null;

            try
            {
                FieldMetadata fieldMetadata = Cache.GetFieldMetadata(entity, fieldName);
                if (fieldMetadata == null)
                    return null;

                Func<object, object> func;
                if (!Converter.TryGetValue(fieldMetadata.FieldType, out func))
                    return null;

                var value = entity.GetValue(fieldName);
                return func(value);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private class MetadataCache
        {
            private readonly Dictionary<Tuple<string, string>, FieldMetadata> _fieldMetadataDictionary = new Dictionary<Tuple<string, string>, FieldMetadata>();

            /// <summary>
            /// Dictionary containing the field metadata for each entity type
            /// </summary>
            private readonly Dictionary<string, List<FieldMetadata>> _fieldListDictionary = new Dictionary<string, List<FieldMetadata>>();

            internal void Clear()
            {
                _fieldMetadataDictionary.Clear();
                _fieldListDictionary.Clear();
            }

            internal void Add(BaseEntity entity, List<FieldMetadata> entityFieldMetadata)
            {
                var entityType = Utility.GetConcreteEntityType(entity);
                _fieldListDictionary[entityType] = entityFieldMetadata;
                foreach (var fieldMetadata in entityFieldMetadata)
                {
                    _fieldMetadataDictionary[new Tuple<string, string>(entityType, fieldMetadata.Name)] = fieldMetadata;
                }
            }

            internal List<FieldMetadata> GetFieldMetadataList(BaseEntity entity)
            {
                var entityType = Utility.GetConcreteEntityType(entity);
                List<FieldMetadata> result;
                _fieldListDictionary.TryGetValue(entityType, out result);
                return result;
            }

            internal FieldMetadata GetFieldMetadata(BaseEntity entity, string propertyName)
            {
                var entityType = Utility.GetConcreteEntityType(entity);

                FieldMetadata fieldMetadata;
                if (!_fieldMetadataDictionary.TryGetValue(new Tuple<string, string>(entityType, propertyName), out fieldMetadata))
                    return null;
                return fieldMetadata;
            }
        }
    }
}
