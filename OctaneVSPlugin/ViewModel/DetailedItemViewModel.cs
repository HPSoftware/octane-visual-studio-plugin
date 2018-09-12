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
using NSoup.Nodes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Input;
using Task = System.Threading.Tasks.Task;

namespace MicroFocus.Adm.Octane.VisualStudio.ViewModel
{
    /// <summary>
    /// Detailed view model for an entity
    /// </summary>
    public class DetailedItemViewModel : BaseItemViewModel, IFieldsObserver
    {
        private readonly OctaneServices _octaneService;

        private ObservableCollection<CommentViewModel> _commentViewModels;
        private readonly List<FieldViewModel> _allEntityFields;

        private readonly List<Phase> _phaseTransitions;

        private string _filter = string.Empty;

        private bool _selectIsEnabled;

        private string _commentText;

        internal static readonly string TempPath = Path.GetTempPath() + "\\Octane_pictures\\";

        /// <summary>
        /// Lets you enable or disable the phase ComboBox
        /// </summary>
        public bool SelectIsEnabled
        {
            get
            {
                return this._selectIsEnabled;
            }
            set
            {
                if (this._selectIsEnabled != value)
                {
                    this._selectIsEnabled = value;
                    NotifyPropertyChanged("SelectIsEnabled");
                }
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public DetailedItemViewModel(BaseEntity entity)
            : base(entity)
        {
            RefreshCommand = new DelegatedCommand(Refresh);
            SaveEntityCommand = new DelegatedCommand(SaveEntity);
            OpenInBrowserCommand = new DelegatedCommand(OpenInBrowser);
            ToggleCommentSectionCommand = new DelegatedCommand(SwitchCommentSectionVisibility);
            AddCommentCommand = new DelegatedCommand(AddComment);
            ToggleEntityFieldVisibilityCommand = new DelegatedCommand(ToggleEntityFieldVisibility);
            ResetFieldsCustomizationCommand = new DelegatedCommand(ResetFieldsCustomization);

            _commentViewModels = new ObservableCollection<CommentViewModel>();
            _allEntityFields = new List<FieldViewModel>();

            _phaseTransitions = new List<Phase>();

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
                Entity = await _octaneService.FindEntityAsync(Entity, fields.Select(fm => fm.Name).ToList());

                await HandleImagesInDescription();

                _allEntityFields.Clear();

                var visibleFieldsHashSet = FieldsCache.Instance.GetVisibleFieldsForEntity(EntityType);
                var fieldsToHideHashSet = FieldsCache.GetFieldsToHide(Entity);
                foreach (var field in fields.Where(f => !fieldsToHideHashSet.Contains(f.Name)))
                {
                    var fieldViewModel = new FieldViewModel(Entity, field, visibleFieldsHashSet.Contains(field.Name));
                    if (!string.Equals(fieldViewModel.Metadata.FieldType, "memo", StringComparison.OrdinalIgnoreCase))
                    {
                        _allEntityFields.Add(fieldViewModel);
                    }

                }
                if (EntitySupportsComments)
                    await RetrieveComments();

                var transitions = await _octaneService.GetTransitionsForEntityType(EntityType);
                if (Entity.TypeName != "run" && Entity.TypeName != "run_manual" && Entity.TypeName != "run_suite")
                {

                    var phaseEntity = Entity.GetValue(CommonFields.Phase) as BaseEntity;
                    var currentPhaseName = phaseEntity.Name;

                    _phaseTransitions.Clear();
                    SelectedNextPhase = null;

                    foreach (var transition in transitions.Where(t => t.SourcePhase.Name == currentPhaseName))
                    {
                        if (transition.IsPrimary)
                            _phaseTransitions.Insert(0, transition.TargetPhase);
                        else
                            _phaseTransitions.Add(transition.TargetPhase);
                    }
                    this._selectIsEnabled = _phaseTransitions.Count != 0;
                }
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
            bool needToUpdateDescription = false;
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

                    needToUpdateDescription = true;
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

            if (doc != null && needToUpdateDescription)
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

        private async Task RetrieveComments()
        {
            try
            {
                var viewModels = new List<CommentViewModel>();
                var commentEntities = await _octaneService.GetAttachedCommentsToEntity(Entity);
                foreach (var comment in commentEntities)
                {
                    viewModels.Add(new CommentViewModel(comment));
                }

                _commentViewModels = new ObservableCollection<CommentViewModel>(viewModels.OrderByDescending(c =>
                {
                    try
                    {
                        return DateTime.ParseExact(c.CreationTime, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                    }
                    catch (Exception)
                    {
                        return DateTime.Now;
                    }
                }));
            }
            catch (Exception)
            {
                _commentViewModels.Clear();
            }
        }

        #region Phase

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
        /// List of names for possible next phases
        /// </summary>
        public List<string> NextPhaseNames
        {
            get { return _phaseTransitions.Select(pt => pt.Name).ToList(); }
        }

        /// <summary>
        /// Name of the currently selected next phase
        /// </summary>
        public string SelectedNextPhase { get; set; }

        #endregion

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

        #region SaveEntity

        /// <summary>
        /// Save entity command
        /// </summary>
        public ICommand SaveEntityCommand { get; }

        private async void SaveEntity(object param)
        {
            try
            {
                Mode = WindowMode.Loading;
                NotifyPropertyChanged("Mode");
                Console.WriteLine("First notify happened");

                var entityToUpdate = new BaseEntity(Entity.Id);
                entityToUpdate.SetValue(BaseEntity.TYPE_FIELD, Entity.TypeName);

                foreach (var field in _allEntityFields.Where(f => f.IsChanged))
                {
                    entityToUpdate.SetValue(field.Name, field.Content);
                }

                entityToUpdate.Name = Entity.Name;

                if (SelectedNextPhase != null)
                {
                    var nextPhase = _phaseTransitions.FirstOrDefault(t => t.Name == SelectedNextPhase);
                    if (nextPhase != null)
                        entityToUpdate.SetValue(CommonFields.Phase, nextPhase);
                }
                await _octaneService.UpdateEntityAsync(entityToUpdate);
                await InitializeAsync();
            }
            catch (Exception ex)
            {
                Mode = WindowMode.FailedToLoad;
                ErrorMessage = ex.Message;
            }
            NotifyPropertyChanged();
            Console.WriteLine("Last notify happened");
        }

        #endregion

        #region AddComments

        public ICommand AddCommentCommand { get; private set; }

        public string CommentText
        {
            get { return _commentText; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    value = value.TrimStart();
                }

                if (value != _commentText)
                {
                    if (value.Contains("\n"))
                    {
                        string valueTrimmed = value.Replace("\n", "<br>");
                        value = valueTrimmed;
                    }
                    _commentText = value;
                    NotifyPropertyChanged("CommentText");
                }
            }
        }


        private async void AddComment(object param)
        {
            try
            {
                Mode = WindowMode.LoadingComments;
                NotifyPropertyChanged("Mode");
                if (EntitySupportsComments)
                {
                    await AddCommentAsync();
                    NotifyPropertyChanged();
                    await RetrieveComments();
                }
            }
            catch (Exception ex)
            {
                Mode = WindowMode.FailedToLoad;
                ErrorMessage = ex.Message;
            }
            Mode = WindowMode.Loaded;
            NotifyPropertyChanged();
        }

        public async Task AddCommentAsync()
        {
            var commentToAdd = new Api.Core.Entities.Comment();
            string encodedCommment = "<html><body>" + CommentText + "</body></html>";
            commentToAdd.Text = encodedCommment;

            string entityAggregateType = Entity.AggregateType;

            switch (entityAggregateType)
            {
                case "work_item":
                    BaseEntity commentOwnerWorkItem = new BaseEntity();
                    commentOwnerWorkItem.Id = Entity.Id;
                    commentOwnerWorkItem.TypeName = Entity.TypeName;
                    commentToAdd.OwnerWorkItem = commentOwnerWorkItem;
                    break;
                case "test":
                    Test commentOwnerTest = new Test();
                    commentOwnerTest.Id = Entity.Id;
                    commentOwnerTest.TypeName = Entity.TypeName;
                    commentToAdd.OwnerTest = commentOwnerTest;
                    break;
                case "requirement":
                    Requirement commentOwnerRequirement = new Requirement();
                    commentOwnerRequirement.Id = Entity.Id;
                    commentOwnerRequirement.TypeName = Entity.TypeName;
                    commentToAdd.OwnerRequirement = commentOwnerRequirement;
                    break;
                case "run":
                    Run commentOwnerRun = new Run();
                    commentOwnerRun.Id = Entity.Id;
                    commentOwnerRun.TypeName = Entity.TypeName;
                    commentToAdd.OwnerRun = commentOwnerRun;
                    break;
                default:
                    break;
            }
            CommentText = "";

            await _octaneService.CreateCommentAsync(commentToAdd);
            NotifyPropertyChanged();
        }

        #endregion

        #region Refresh

        /// <summary>
        /// Refresh entity information
        /// </summary>
        public ICommand RefreshCommand { get; }

        private void Refresh(object param)
        {
            try
            {
                Mode = WindowMode.Loading;
                NotifyPropertyChanged("Mode");

                InitializeAsync();
            }
            catch (Exception ex)
            {
                Mode = WindowMode.FailedToLoad;
                ErrorMessage = ex.Message;
            }
            NotifyPropertyChanged();
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

        #region CheckIfFieldModified

        private bool _isAFieldChanged;

        public bool IsAFieldChanged
        {
            get
            {
                return _isAFieldChanged;
            }
            set
            {
                foreach (var field in _allEntityFields.Where(f => f.IsChanged))
                {
                    _isAFieldChanged = field.IsAFieldChangedEntity;
                }
                NotifyPropertyChanged("IsAFieldChanged");
            }
        }
        #endregion
    }
}
