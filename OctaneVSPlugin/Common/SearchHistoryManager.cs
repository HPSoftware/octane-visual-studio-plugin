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
    public static class SearchHistoryManager
    {
        private static SearchHistoryMetadata _metadata;

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

            LoadHistoryIfNeeded();

            var newHistory = _metadata.queries.ToList();
            var oldHistory = _metadata.queries.ToList();

            newHistory.Clear();
            newHistory.Add(filter);

            oldHistory.Remove(filter);

            newHistory.AddRange(oldHistory.Take(MaxSearchHistorySize - 1));

            _metadata.queries = newHistory;

            SaveHistory();
        }

        /// <summary>
        /// Returns the current search history
        /// </summary>
        internal static List<string> History
        {
            get
            {
                LoadHistoryIfNeeded();
                HandleDifferentContext();
                return _metadata.queries.ToList();
            }
        }

        internal static bool IsActiveItem(BaseEntity entity)
        {
            if (entity == null)
                return false;

            LoadHistoryIfNeeded();
            HandleDifferentContext();

            return _metadata.activeItemType == Utility.GetConcreteEntityType(entity)
                   && _metadata.activeItemId == entity.Id;
        }

        internal static BaseEntity GetActiveEntity()
        {
            LoadHistoryIfNeeded();
            HandleDifferentContext();

            if (string.IsNullOrEmpty(_metadata.activeItemType))
                return null;

            var entity = new BaseEntity(_metadata.activeItemId);
            entity.SetValue(BaseEntity.TYPE_FIELD, _metadata.activeItemType);
            return entity;
        }

        internal static void SetActiveEntity(BaseEntity entity)
        {
            if (entity == null)
                return;

            LoadHistoryIfNeeded();
            HandleDifferentContext();

            _metadata.activeItemType = Utility.GetConcreteEntityType(entity);
            _metadata.activeItemId = entity.Id;

            SaveHistory();
        }

        internal static void ClearActiveEntity()
        {
            LoadHistoryIfNeeded();
            HandleDifferentContext();

            _metadata.activeItemType = string.Empty;
            _metadata.activeItemId = string.Empty;

            SaveHistory();
        }

        private static void LoadHistoryIfNeeded()
        {
            if (_metadata != null)
                return;

            _metadata = Utility.DeserializeFromJson(OctanePluginSettings.Default.SearchHistory, new SearchHistoryMetadata
            {
                id = ConstructId(),
                queries = new List<string>(),
                activeItemType = string.Empty,
                activeItemId = string.Empty
            });
        }

        private static void HandleDifferentContext()
        {
            if (_metadata.id == ConstructId())
            {
                return;
            }

            _metadata = new SearchHistoryMetadata
            {
                id = ConstructId(),
                queries = new List<string>(),
                activeItemType = string.Empty,
                activeItemId = string.Empty
            };

            SaveHistory();
        }

        private static void SaveHistory()
        {
            try
            {
                OctanePluginSettings.Default.SearchHistory = Utility.SerializeToJson(_metadata);
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
        public class SearchHistoryMetadata
        {
            [DataMember]
            public string id;

            [DataMember]
            public List<string> queries;

            [DataMember]
            public string activeItemType;

            [DataMember]
            public string activeItemId;
        }
    }
}
