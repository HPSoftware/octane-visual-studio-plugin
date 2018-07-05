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
using MicroFocus.Adm.Octane.VisualStudio.Common;
using System;
using System.ComponentModel;
using System.Windows.Media;

namespace MicroFocus.Adm.Octane.VisualStudio.ViewModel
{
    /// <summary>
    /// Base class for an entity view model
    /// </summary>
    public class BaseItemViewModel : INotifyPropertyChanged
    {
        protected readonly EntityTypeInformation EntityTypeInformation;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="entity"></param>
        public BaseItemViewModel(BaseEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            Entity = entity;
            EntityTypeInformation = EntityTypeRegistry.GetEntityTypeInformation(entity);
        }

        /// <summary>
        /// Entity associated with the current view model
        /// </summary>
        public BaseEntity Entity { get; protected set; }

        /// <summary>
        /// Entity ID associated witht he current view model
        /// </summary>
        public EntityId ID
        {
            get { return Entity.Id; }
        }

        /// <summary>
        /// Title shown for the current item
        /// </summary>
        public virtual string Title
        {
            get { return Entity.Name; }
            set { Entity.Name = value; }
        }

        /// <summary>
        /// Description for the current item view model
        /// </summary>
        public virtual string Description
        {
            get { return Entity.GetStringValue(CommonFields.Description) ?? string.Empty; }
        }

        /// <summary>
        /// Short label for the current item view model
        /// </summary>
        public virtual string IconText
        {
            get { return EntityTypeInformation.ShortLabel; }
        }

        /// <summary>
        /// Icon color for the current item view model
        /// </summary>
        public virtual Color IconBackgroundColor
        {
            get { return EntityTypeInformation.Color; }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected void NotifyPropertyChanged(string propName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
        #endregion
    }
}
