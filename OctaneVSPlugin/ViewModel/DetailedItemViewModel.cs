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
using MicroFocus.Adm.Octane.VisualStudio.Common;
using NSoup.Nodes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Task = System.Threading.Tasks.Task;

namespace MicroFocus.Adm.Octane.VisualStudio.ViewModel
{
    /// <summary>
    /// Detailed view model for an entity
    /// </summary>
    public class DetailedItemViewModel : BaseItemViewModel, INotifyPropertyChanged, IFieldsObserver
    {
        private readonly OctaneServices _octaneService;

        private ObservableCollection<CommentViewModel> _commentViewModels;
        private readonly List<FieldViewModel> _allEntityFields;

        private string _filter = string.Empty;

        internal static readonly string TempPath = Path.GetTempPath() + "\\Octane_pictures\\";

        /// <summary>
        /// Constructor
        /// </summary>
        public DetailedItemViewModel(BaseEntity entity)
            : base(entity)
        {
            RefreshCommand = new DelegatedCommand(Refresh);
            OpenInBrowserCommand = new DelegatedCommand(OpenInBrowser);
            ToggleCommentSectionCommand = new DelegatedCommand(SwitchCommentSectionVisibility);
            ToggleEntityFieldVisibilityCommand = new DelegatedCommand(ToggleEntityFieldVisibility);
            ResetFieldsCustomizationCommand = new DelegatedCommand(ResetFieldsCustomization);

            _commentViewModels = new ObservableCollection<CommentViewModel>();
            _allEntityFields = new List<FieldViewModel>();

            Mode = WindowMode.Loading;

            EntitySupportsComments = EntityTypesSupportingComments.Contains(Utility.GetConcreteEntityType(entity));

            _octaneService = new OctaneServices(
                OctaneConfiguration.Url,
                OctaneConfiguration.SharedSpaceId,
                OctaneConfiguration.WorkSpaceId,
                OctaneConfiguration.Username,
                OctaneConfiguration.Password);

            Id = (long)entity.Id;
            EntityType = Utility.GetConcreteEntityType(Entity);
            FieldsCache.Instance.Attach(this);
        }

        /// <summary>
        /// Initialize detailed information about the cached entity
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                await _octaneService.Connect();

                List<FieldMetadata> fields = await FieldsMetadataService.GetFieldMetadata(Entity);
                var updatedFields = fields.Select(fm => fm.Name).ToList();
                // TODO - investigate why not all entities receive the subtype field by default
                if (MyWorkMetadata.IsAggregateEntity(Entity.GetType()))
                {
                    updatedFields.Add(CommonFields.SubType);
                }

                Entity = await _octaneService.FindEntity(Entity, updatedFields);

                await HandleImagesInDescription();

                _allEntityFields.Clear();

                var visibleFieldsHashSet = FieldsCache.Instance.GetVisibleFieldsForEntity(EntityType);
                var fieldsToHideHashSet = FieldsCache.GetFieldsToHide(Entity);
                foreach (var field in fields.Where(f => !fieldsToHideHashSet.Contains(f.Name)))
                {
                    var fieldViewModel = new FieldViewModel(Entity, field.Name, field.Label, visibleFieldsHashSet.Contains(field.Name));

                    _allEntityFields.Add(fieldViewModel);
                }

                if (EntitySupportsComments)
                    await RetrieveComments();

                Mode = WindowMode.Loaded;
            }
            catch (Exception ex)
            {
                Mode = WindowMode.FailedToLoad;
                ErrorMessage = ex.Message;
            }
            NotifyPropertyChanged();
        }

        private async Task HandleImagesInDescription()
        {
            List<Task> downloadTasks = new List<Task>();
            List<Tuple<Element, string>> imageNodes = new List<Tuple<Element, string>>();
            bool updatedImage = false;
            Document doc = null;
            try
            {
                var description = Entity.GetStringValue(CommonFields.Description);
                if (string.IsNullOrEmpty(description))
                    return;

                if (!Directory.Exists(TempPath))
                {
                    Directory.CreateDirectory(TempPath);
                }

                doc = NSoup.Parse.Parser.Parse(description, OctaneConfiguration.Url);
                foreach (var image in doc.Select("img"))
                {
                    var relativeUrl = image.Attr("src");

                    if (relativeUrl == null || !relativeUrl.StartsWith("/api/shared_spaces"))
                        continue;

                    var imageName = relativeUrl.Split('/').LastOrDefault();
                    if (string.IsNullOrEmpty(imageName))
                        continue;

                    var imagePath = TempPath + EntityType + Entity.Id + imageName;
                    if (!File.Exists(imagePath))
                    {
                        imageNodes.Add(new Tuple<Element, string>(image, imagePath));
                        downloadTasks.Add(_octaneService.DownloadAttachmentAsync(relativeUrl, imagePath));
                    }
                    else
                    {
                        image.Attr("src", imagePath);
                    }

                    updatedImage = true;
                }

                await Task.WhenAll(downloadTasks);
            }
            catch (Exception)
            {
            }

            for (var i = 0; i < downloadTasks.Count; i++)
            {
                if (downloadTasks[i].Status == TaskStatus.RanToCompletion)
                {
                    imageNodes[i].Item1.Attr("src", imageNodes[i].Item2);
                }
            }

            if (doc != null && updatedImage)
            {
                Entity.SetValue(CommonFields.Description, doc.ToString());
            }
        }

        /// <summary>
        /// Flag specifying whether the current entity supports comments
        /// </summary>
        public bool EntitySupportsComments { get; }

        #region IFieldsObserver

        /// <inheritdoc />
        public long Id { get; }

        /// <inheritdoc />
        public string EntityType { get; }

        /// <inheritdoc />
        public void UpdateFields()
        {
            var persistedVisibleFields = FieldsCache.Instance.GetVisibleFieldsForEntity(EntityType);
            foreach (var field in _allEntityFields)
            {
                field.IsSelected = persistedVisibleFields.Contains(field.Name);
            }

            NotifyPropertyChanged("FilteredEntityFields");
            NotifyPropertyChanged("VisibleFields");
        }

        #endregion

        /// <summary>
        /// Entity fields shown in the main section of the detailed item view
        /// </summary>
        public IEnumerable<FieldViewModel> VisibleFields
        {
            get
            {
                NotifyPropertyChanged("OnlyDefaultFieldsAreShown");
                return new ObservableCollection<FieldViewModel>(_allEntityFields.Where(f => f.IsSelected));
            }
        }

        #region FieldCustomization

        /// <summary>
        /// Entity fields shown in the field customization control after the search filter has been applied
        /// </summary>
        public IEnumerable<FieldViewModel> FilteredEntityFields
        {
            get
            {
                return new ObservableCollection<FieldViewModel>(
                    _allEntityFields.Where(f => f.Label.ToLowerInvariant().Contains(_filter))
                                    .OrderBy(f => f.Label));
            }
        }

        /// <summary>
        /// Search filter applied on the entity fields
        /// </summary>
        public string Filter
        {
            get { return _filter; }
            set
            {
                _filter = value?.ToLowerInvariant() ?? string.Empty;
                NotifyPropertyChanged("FilteredEntityFields");
            }
        }

        /// <summary>
        /// Command for handling the visibility change of an entity field
        /// </summary>
        public ICommand ToggleEntityFieldVisibilityCommand { get; }

        private void ToggleEntityFieldVisibility(object param)
        {
            FieldsCache.Instance.UpdateVisibleFieldsForEntity(EntityType, _allEntityFields);
        }

        /// <summary>
        /// Command for reseting the visible fields to the defaults for the current entity
        /// </summary>
        public ICommand ResetFieldsCustomizationCommand { get; }

        private void ResetFieldsCustomization(object param)
        {
            FieldsCache.Instance.ResetVisibleFieldsForEntity(EntityType);
        }

        /// <summary>
        /// Flag specifying whether only the default fields for the current entity are selected and shown in the view
        /// </summary>
        public bool OnlyDefaultFieldsAreShown
        {
            get
            {
                return FieldsCache.Instance.AreSameFieldsAsDefaultFields(EntityType,
                    _allEntityFields.Where(f => f.IsSelected).ToList());
            }
        }

        #endregion

        /// <summary>
        /// Message shown to the user in case of an error
        /// </summary>
        public string ErrorMessage { get; private set; }

        private async System.Threading.Tasks.Task RetrieveComments()
        {
            try
            {
                var viewModels = new List<CommentViewModel>();
                var commentEntities = await _octaneService.GetAttachedCommentsToEntity(Entity);
                foreach (var comment in commentEntities)
                {
                    viewModels.Add(new CommentViewModel(comment));
                }

                _commentViewModels = new ObservableCollection<CommentViewModel>(viewModels.OrderByDescending(c => c.CreationTime));
            }
            catch (Exception)
            {
                _commentViewModels.Clear();
            }
        }

        /// <summary>
        /// Flag specifiying whether we need to show the phase section in the UI or not
        /// </summary>
        public bool ShowPhase
        {
            get { return !(Entity is Run); }
        }

        /// <summary>
        /// Current phase for the entity
        /// </summary>
        public string Phase
        {
            get
            {
                if (Mode == WindowMode.Loaded)
                {
                    var phaseEntity = Entity.GetValue(CommonFields.Phase) as BaseEntity;
                    if (phaseEntity == null)
                        return string.Empty;

                    return phaseEntity.Name;
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Comments associated with the cached entity
        /// </summary>
        public IEnumerable<CommentViewModel> Comments
        {
            get { return _commentViewModels; }
        }

        /// <summary>
        /// Current status of the detailed view
        /// </summary>
        public WindowMode Mode { get; private set; }

        private static readonly HashSet<string> EntityTypesSupportingComments = new HashSet<string>
        {
            // work item
            WorkItem.SUBTYPE_DEFECT,
            WorkItem.SUBTYPE_STORY,
            WorkItem.SUBTYPE_QUALITY_STORY,

            // test
            TestGherkin.SUBTYPE_GHERKIN_TEST,
            Test.SUBTYPE_MANUAL_TEST,

            // run
            RunManual.SUBTYPE_RUN_MANUAL,
            RunSuite.SUBTYPE_RUN_SUITE,

            // requirement
            Requirement.SUBTYPE_DOCUMENT
        };

        #region Refresh

        /// <summary>
        /// Refresh entity information
        /// </summary>
        public ICommand RefreshCommand { get; }

        private void Refresh(object param)
        {
            Mode = WindowMode.Loading;
            NotifyPropertyChanged("Mode");

            InitializeAsync();
        }

        #endregion

        #region OpenInBrowser

        /// <summary>
        /// Open in browser command
        /// </summary>
        public ICommand OpenInBrowserCommand { get; }

        private void OpenInBrowser(object param)
        {
            Utility.OpenInBrowser(Entity);
        }

        #endregion

        #region CommentSectionVisibility

        /// <summary>
        /// Flag specifying whether the comment section is visible or not
        /// </summary>
        public bool CommentSectionVisibility { get; set; }

        /// <summary>
        /// Toggle the comment section visibility command
        /// </summary>
        public ICommand ToggleCommentSectionCommand { get; }

        private void SwitchCommentSectionVisibility(object param)
        {
            CommentSectionVisibility = !CommentSectionVisibility;
            NotifyPropertyChanged("CommentSectionVisibility");
            NotifyPropertyChanged("ShowCommentTooltip");
        }

        internal const string HideCommentsTooltip = "Hide comments";
        internal const string ShowCommentsTooltip = "Show comments";

        /// <summary>
        /// Comment button tooltip
        /// </summary>
        public string ShowCommentTooltip
        {
            get { return CommentSectionVisibility ? HideCommentsTooltip : ShowCommentsTooltip; }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void NotifyPropertyChanged(string propName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        #endregion
    }
}
