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
        /// Flag used to know whether VS is shutting down
        /// </summary>
        private static bool _isShuttingDown;

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

            ValidateMetadata();

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
                ValidateMetadata();

                return _metadata.queries.ToList();
            }
        }

        /// <summary>
        /// Notify WorkspaceSessionPersistanceManager that Visual Studio is shutting down
        /// </summary>
        internal static void MarkIsShuttingDownOperation()
        {
            _isShuttingDown = true;
        }

        /// <summary>
        /// Register given entity so that it's detailed window can be restored after VS restarts
        /// </summary>
        internal static void RegisterEntityWithDetailedView(BaseEntity entity)
        {
            ValidateMetadata();

            var baseEntityType = Utility.GetBaseEntityType(entity);
            if (_metadata.entities.Any(e => e.id == entity.Id && e.typeName == baseEntityType))
                return;

            _metadata.entities.Add(new SimpleEntity
            {
                id = entity.Id,
                typeName = baseEntityType,
                subTypeName = entity.GetStringValue(WorkItem.SUBTYPE_FIELD)
            });

            SaveMetadata();
        }

        /// <summary>
        /// Unregister given entity
        /// </summary>
        internal static void UnregisterEntityWithDetailedView(BaseEntity entity)
        {
            if (_isShuttingDown)
                return;

            ValidateMetadata();

            var baseEntityType = Utility.GetBaseEntityType(entity);
            _metadata.entities.RemoveAll(e => e.id == entity.Id && e.typeName == baseEntityType);

            SaveMetadata();
        }

        /// <summary>
        /// Unregister all entities
        /// </summary>
        internal static void UnregisterAllEntities()
        {
            if (_isShuttingDown)
                return;

            ValidateMetadata();

            _metadata.entities = new List<SimpleEntity>();

            SaveMetadata();
        }

        /// <summary>
        /// Returns all registered entities
        /// </summary>
        internal static List<BaseEntity> GetAllRegisteredEntities()
        {
            ValidateMetadata();

            return _metadata.entities.Select(e =>
            {
                var entity = new BaseEntity(e.id);
                entity.SetValue(BaseEntity.TYPE_FIELD, e.typeName);
                if (e.subTypeName != null)
                    entity.SetValue(WorkItem.SUBTYPE_FIELD, e.subTypeName);
                return entity;
            }).ToList();
        }

        private static void ValidateMetadata()
        {
            LoadMetadataIfNeeded();
            HandleDifferentContext();
        }

        private static void LoadMetadataIfNeeded()
        {
            if (_metadata != null)
                return;

            _metadata = Utility.DeserializeFromJson(OctanePluginSettings.Default.WorkspaceSession, new WorkspaceSessionMetadata
            {
                id = ConstructId(),
                queries = new List<string>(),
                entities = new List<SimpleEntity>()
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
                queries = new List<string>(),
                entities = new List<SimpleEntity>()
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

            [DataMember]
            public string subTypeName;
        }
    }
}
