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
using System.Runtime.Serialization;

namespace MicroFocus.Adm.Octane.VisualStudio.Common
{
    /// <summary>
    /// Class for managing the user's search history
    /// </summary>
    public static class WorkspaceSessionPersistanceManager
    {
        private static WorkspaceSessionMetadata _metadata;

        /// <summary>
        /// Maximum number of elements saved for the search history
        /// </summary>
        internal const int MaxSearchHistorySize = 5;

        /// <summary>
        /// Update the search history with the given filter
        /// </summary>
        internal static void UpdateHistory(string filter)
        {
            if (string.IsNullOrEmpty(filter))
                return;

            LoadMetadataIfNeeded();

            var newHistory = _metadata.queries.ToList();
            var oldHistory = _metadata.queries.ToList();

            newHistory.Clear();
            newHistory.Add(filter);

            oldHistory.Remove(filter);

            newHistory.AddRange(oldHistory.Take(MaxSearchHistorySize - 1));

            _metadata.queries = newHistory;

            SaveMetadata();
        }

        /// <summary>
        /// Returns the current search history
        /// </summary>
        internal static List<string> History
        {
            get
            {
                LoadMetadataIfNeeded();

                return _metadata.queries.ToList();
            }
        }

        internal static void RegisterEntityWithDetailedView(BaseEntity entity)
        {
            LoadMetadataIfNeeded();

            _metadata.entities.Add(new SimpleEntity { id = entity.Id, typeName = Utility.GetBaseEntityType(entity) });

            SaveMetadata();
        }

        internal static void UnregisterEntityWithDetailedView(BaseEntity entity)
        {
            LoadMetadataIfNeeded();

            var baseEntityType = Utility.GetBaseEntityType(entity);
            _metadata.entities.RemoveAll(e => e.id == entity.Id && e.typeName == baseEntityType);

            SaveMetadata();
        }

        internal static void UnregisterAllEntities()
        {
            LoadMetadataIfNeeded();

            _metadata.entities = new List<SimpleEntity>();

            SaveMetadata();
        }

        internal static List<BaseEntity> GetAllEntities()
        {
            LoadMetadataIfNeeded();

            return _metadata.entities.Select(e =>
            {
                var entity = new BaseEntity(e.id);
                entity.SetValue(BaseEntity.TYPE_FIELD, e.typeName);
                return entity;
            }).ToList();
        }

        private static void LoadMetadataIfNeeded()
        {
            if (_metadata != null)
                return;

            HandleDifferentContext();

            _metadata = Utility.DeserializeFromJson(OctanePluginSettings.Default.WorkspaceSession, new WorkspaceSessionMetadata
            {
                id = ConstructId(),
                queries = new List<string>()
            });
        }

        private static void HandleDifferentContext()
        {
            if (_metadata.id == ConstructId())
            {
                return;
            }

            _metadata = new WorkspaceSessionMetadata
            {
                id = ConstructId(),
                queries = new List<string>()
            };

            SaveMetadata();
        }

        private static void SaveMetadata()
        {
            try
            {
                OctanePluginSettings.Default.WorkspaceSession = Utility.SerializeToJson(_metadata);
                OctanePluginSettings.Default.Save();
            }
            catch (Exception)
            {
            }
        }

        private static string ConstructId()
        {
            return OctaneConfiguration.Url + OctaneConfiguration.SharedSpaceId + OctaneConfiguration.WorkSpaceId + OctaneConfiguration.Username;
        }

        [DataContract]
        public class WorkspaceSessionMetadata
        {
            [DataMember]
            public string id;

            [DataMember]
            public List<string> queries;

            [DataMember]
            public List<SimpleEntity> entities;
        }

        [DataContract]
        public class SimpleEntity
        {
            [DataMember]
            public string id;

            [DataMember]
            public string typeName;
        }
    }
}
