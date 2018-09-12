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
using MicroFocus.Adm.Octane.Api.Core.Services;
using MicroFocus.Adm.Octane.VisualStudio.Common;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace MicroFocus.Adm.Octane.VisualStudio.ViewModel
{
    /// <summary>
    /// View model representation for an entity field
    /// </summary>
    public class FieldViewModel
    {
        private readonly BaseEntity _parentEntity;
        private readonly string _emptyPlaceholder;
        private readonly Func<BaseEntity, object> _customContentFunc;

        public FieldViewModel(BaseEntity entity, string fieldName, string fieldValue, bool isSelected)
        {
            _parentEntity = entity;
            Name = fieldName;
            Label = fieldValue;
            IsSelected = isSelected;
        }

        public FieldViewModel(BaseEntity entity, FieldMetadata metadata, bool isSelected)
        {
            _parentEntity = entity;
            Metadata = metadata;

            Name = metadata.Name;
            Label = metadata.Label;
            IsSelected = isSelected;
        }

        public FieldViewModel(BaseEntity entity, FieldInfo fieldInfo)
        {
            _parentEntity = entity;
            Name = fieldInfo.Name;
            Label = fieldInfo.Title;
            _emptyPlaceholder = fieldInfo.EmptyPlaceholder;
            _customContentFunc = fieldInfo.ContentFunc;
        }

        public FieldMetadata Metadata { get; }

        public string Label { get; }

        public string Name { get; }

        public bool HideLabel
        {
            get { return string.IsNullOrEmpty(Label); }
        }

        public bool IsSelected { get; set; }

        public object Content
        {
            get
            {
                if (_customContentFunc != null)
                    return _customContentFunc(_parentEntity);

                var formattedValue = FieldsMetadataService.GetFormattedValue(_parentEntity, Name);
                if (formattedValue != null)
                    return formattedValue;

                object value = _parentEntity.GetValue(Name);
                switch (value)
                {
                    case null:
                        return _emptyPlaceholder;
                    case BaseEntity entity:
                        return entity.Name;
                    case EntityList<BaseEntity> entityList:
                        return FormatEntityList(entityList);
                    default:
                        return value;
                }
            }
            set
            {
                switch (Metadata.FieldType)
                {
                    case "boolean":
                        _parentEntity.SetValue(Name, value);
                        IsChanged = true;
                        IsAFieldChangedEntity = true;
                        break;
                    case "integer":
                        try
                        {
                            _parentEntity.SetIntValue(Name, int.Parse(value.ToString()));
                            IsChanged = true;
                        }
                        catch (Exception ex)
                        {
                            if (ex is FormatException || ex is OverflowException)
                            {
                                _parentEntity.SetValue(Name, "");
                                IsChanged = true;
                                IsAFieldChangedEntity = true;
                            }
                        }
                        break;
                    case "string":
                        _parentEntity.SetValue(Name, value.ToString());
                        IsChanged = true;
                        IsAFieldChangedEntity = true;
                        break;
                    case "date_time":
                        DateTime newValue = DateTime.MinValue;
                        if (value is DateTime)
                        {
                            newValue = (DateTime)value;
                            _parentEntity.SetDateTimeValue(Name, newValue.ToUniversalTime());
                        }
                        else
                        {
                            _parentEntity.SetDateTimeValue(Name, DateTime.UtcNow);
                        }
                        IsChanged = true;
                        IsAFieldChangedEntity = true;
                        break;
                }
            }
        }

        public bool IsChanged { get; private set; }

        public bool IsAFieldChangedEntity{ get; set; }

        private string FormatEntityList(EntityList<BaseEntity> value)
        {
            if (value.data.Count == 0)
            {
                return _emptyPlaceholder;
            }

            string[] entityNames = value.data.Select(x => x.Name).ToArray();
            return string.Join(", ", entityNames);
        }
    }
}
