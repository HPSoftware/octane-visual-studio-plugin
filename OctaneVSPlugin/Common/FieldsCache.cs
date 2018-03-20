﻿/*!
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace MicroFocus.Adm.Octane.VisualStudio.Common
{
    /// <summary>
    /// Cache responsible for maintaining the default fields for each entity type
    /// </summary>
    public class FieldsCache
    {
        private static FieldsCache _fieldsCache;
        private static readonly object _obj = new object();

        private static readonly DataContractJsonSerializer _serializer = new DataContractJsonSerializer(typeof(Metadata));

        private static Metadata _cache = null;

        private FieldsCache()
        {
            DeserializeDefaultFieldsMetadata();
        }

        public static FieldsCache Instance
        {
            get
            {
                if (_fieldsCache == null)
                {
                    lock (_obj)
                    {
                        if (_fieldsCache == null)
                        {
                            _fieldsCache = new FieldsCache();
                        }
                    }
                }

                return _fieldsCache;
            }
        }

        private static void Serialize()
        {
            MemoryStream myms = new MemoryStream();
            _serializer.WriteObject(myms, _cache);

            myms.Position = 0;
            StreamReader sr = new StreamReader(myms);
            var json = sr.ReadToEnd();
        }

        private static Metadata Deserialize(string json)
        {
            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return (Metadata)_serializer.ReadObject(stream);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void DeserializeDefaultFieldsMetadata()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream("MicroFocus.Adm.Octane.VisualStudio.Resources.DefaultFields.json"))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                _cache = Deserialize(result);
            }
        }

        public bool IsFieldVisible(string entityType, string fieldName)
        {
            if (_cache == null)
                return false;

            HashSet<string> visibleFields;
            if (!_cache.data.TryGetValue(entityType, out visibleFields))
                return false;

            return visibleFields.Contains(fieldName);
        }

        public void SetFieldVisibility(string entityType, List<FieldGetterViewModel> allFields)
        {
            if (_cache == null)
                return;

            HashSet<string> visibleFields;
            if (!_cache.data.TryGetValue(entityType, out visibleFields))
            {
                visibleFields = new HashSet<string>();
                _cache.data[entityType] = visibleFields;
            }

            visibleFields.Clear();

            foreach (var field in allFields.Where(f => f.IsSelected))
            {
                visibleFields.Add(field.Name);
            }
        }

        #region Data contracts

        [DataContract]
        private class Metadata
        {
            [DataMember]
            public int version;

            [DataMember]
            public Dictionary<string, HashSet<string>> data;
        }

        #endregion
    }
}
