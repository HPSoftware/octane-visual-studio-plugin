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
using MicroFocus.Adm.Octane.VisualStudio.Tests.Utilities;
using MicroFocus.Adm.Octane.VisualStudio.Tests.Utilities.Entity;
using MicroFocus.Adm.Octane.VisualStudio.ViewModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Utility = MicroFocus.Adm.Octane.VisualStudio.Common.Utility;

namespace MicroFocus.Adm.Octane.VisualStudio.Tests.ViewModel
{
    /// <summary>
    /// Test class for <see cref="DetailedItemViewModel"/>
    /// </summary>
    [TestClass]
    public class DetailedItemViewModelTests : BaseOctanePluginTest
    {
        private static Story _story;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            _story = StoryUtilities.CreateStory();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            EntityService.DeleteById<Story>(WorkspaceContext, _story.Id);
        }

        #region EntitySupportsComments

        [TestMethod]
        public void DetailedItemViewModelTests_EntitySupportsComments_EntitySupportsComments_True()
        {
            var viewModel = new DetailedItemViewModel(_story);
            Assert.IsTrue(viewModel.EntitySupportsComments, "Entity should support comments");
        }

        [TestMethod]
        public void DetailedItemViewModelTests_EntitySupportsComments_EntityDoesntSupportComments_False()
        {
            var task = new Task("1001");
            task.SetValue(WorkItem.SUBTYPE_FIELD, "task");
            var viewModel = new DetailedItemViewModel(task);
            Assert.IsFalse(viewModel.EntitySupportsComments, "Entity shouldn't support comments");
        }

        #endregion

        #region RefreshCommand

        [TestMethod]
        public void DetailedItemViewModelTests_RefreshCommand_RefreshWithoutAnyChanges_Success()
        {
            var viewModel = new DetailedItemViewModel(_story);
            viewModel.InitializeAsync().Wait();

            var expectedVisibleFields = viewModel.VisibleFields.Select(f => f.Name).ToList();

            viewModel.RefreshCommand.Execute(null);

            Utilities.Utility.WaitUntil(() => viewModel.Mode == WindowMode.Loaded,
                "Timeout while refreshing the entity", new TimeSpan(0, 0, 30));

            var actualVisibleFields = viewModel.VisibleFields.Select(f => f.Name).ToList();

            CollectionAssert.AreEqual(expectedVisibleFields, actualVisibleFields, "Mismathed visible fields");
        }

        [TestMethod]
        public void DetailedItemViewModelTests_RefreshCommand_RefreshAfterChangingVisibleFields_Succes()
        {
            var viewModel = new DetailedItemViewModel(_story);
            viewModel.InitializeAsync().Wait();

            foreach (var field in viewModel.FilteredEntityFields)
            {
                field.IsSelected = true;
                viewModel.ToggleEntityFieldVisibilityCommand.Execute(null);
            }

            var expectedVisibleFields = viewModel.VisibleFields.Select(f => f.Name).ToList();

            viewModel.RefreshCommand.Execute(null);

            Utilities.Utility.WaitUntil(() => viewModel.Mode == WindowMode.Loaded,
                "Timeout while refreshing the entity", new TimeSpan(0, 0, 30));

            var actualVisibleFields = viewModel.VisibleFields.Select(f => f.Name).ToList();

            CollectionAssert.AreEqual(expectedVisibleFields, actualVisibleFields, "Mismathed visible fields");
        }

        #endregion

        #region Filter

        [TestMethod]
        public void DetailedItemViewModelTests_Filter_NullFilter_ReturnAllFields()
        {
            var viewModel = new DetailedItemViewModel(_story);
            viewModel.InitializeAsync().Wait();

            var expectedFilteredFields = viewModel.FilteredEntityFields.Select(f => f.Name).ToList();

            viewModel.Filter = null;

            var actualFilteredFields = viewModel.FilteredEntityFields.Select(f => f.Name).ToList();

            CollectionAssert.AreEqual(expectedFilteredFields, actualFilteredFields, "Mismathed filtered fields");
        }

        [TestMethod]
        public void DetailedItemViewModelTests_Filter_EmptyFilter_ReturnAllFields()
        {
            var viewModel = new DetailedItemViewModel(_story);
            viewModel.InitializeAsync().Wait();

            var expectedFilteredFields = viewModel.FilteredEntityFields.Select(f => f.Name).ToList();

            viewModel.Filter = string.Empty;

            var actualFilteredFields = viewModel.FilteredEntityFields.Select(f => f.Name).ToList();

            CollectionAssert.AreEqual(expectedFilteredFields, actualFilteredFields, "Mismathed filtered fields");
        }

        [TestMethod]
        public void DetailedItemViewModelTests_Filter_FilterDoesntMatchAnyItem_ReturnEmptyList()
        {
            var viewModel = new DetailedItemViewModel(_story);
            viewModel.InitializeAsync().Wait();

            viewModel.Filter = "FilterDoesntMatchAnyItem";

            var actualFilteredFields = viewModel.FilteredEntityFields.Select(f => f.Name).ToList();

            Assert.AreEqual(0, actualFilteredFields.Count,
                "Filter that doesn't match any item should return an empty search result");
        }

        [TestMethod]
        public void DetailedItemViewModelTests_Filter_FilterPartialMatch_ReturnMatchList()
        {
            var viewModel = new DetailedItemViewModel(_story);
            viewModel.InitializeAsync().Wait();

            var expectedFilteredFields = new List<string> { "Blocked reason", "Creation time", "Feature", "Release", "Team" };
            viewModel.Filter = "ea";
            var actualFilteredFields = viewModel.FilteredEntityFields.Select(f => f.Label).ToList();
            CollectionAssert.AreEqual(expectedFilteredFields, actualFilteredFields,
                $"Mismathed filtered fields for filter '{viewModel.Filter}'");

            expectedFilteredFields = new List<string> { "Creation time", "Feature", };
            viewModel.Filter = "eat";
            actualFilteredFields = viewModel.FilteredEntityFields.Select(f => f.Label).ToList();
            CollectionAssert.AreEqual(expectedFilteredFields, actualFilteredFields,
                $"Mismathed filtered fields for filter '{viewModel.Filter}'");

            expectedFilteredFields = new List<string> { "Feature" };
            viewModel.Filter = "feature";
            actualFilteredFields = viewModel.FilteredEntityFields.Select(f => f.Label).ToList();
            CollectionAssert.AreEqual(expectedFilteredFields, actualFilteredFields,
                $"Mismathed filtered fields for filter '{viewModel.Filter}'");
        }

        [TestMethod]
        public void DetailedItemViewModelTests_Filter_FilterIgnoreCase_ReturnMatchList()
        {
            var viewModel = new DetailedItemViewModel(_story);
            viewModel.InitializeAsync().Wait();

            var expectedFilteredFields = new List<string> { "Creation time", "Feature", };
            viewModel.Filter = "EaT";
            var actualFilteredFields = viewModel.FilteredEntityFields.Select(f => f.Label).ToList();
            CollectionAssert.AreEqual(expectedFilteredFields, actualFilteredFields,
                $"Mismathed filtered fields for filter 'EaT'");
        }

        #endregion

        #region VisibleFields

        [TestMethod]
        public void DetailedItemViewModelTests_VisibleFields_ShowHideFields_ShowSelectedFields()
        {
            var viewModel = new DetailedItemViewModel(_story);
            viewModel.InitializeAsync().Wait();

            ChangeFieldVisibility(viewModel, "Release", false);
            ChangeFieldVisibility(viewModel, "Committers", true);

            var expectedVisibleFields = viewModel.FilteredEntityFields.Where(f => f.IsSelected).Select(f => f.Name).OrderBy(f => f).ToList();
            var actualVisibleFields = viewModel.VisibleFields.Select(f => f.Name).OrderBy(f => f).ToList();
            CollectionAssert.AreEqual(expectedVisibleFields, actualVisibleFields, "Mismathed visible fields");
        }

        [TestMethod]
        public void DetailedItemViewModelTests_VisibleFields_ShowAllFields_ShowSelectedFields()
        {
            var viewModel = new DetailedItemViewModel(_story);
            viewModel.InitializeAsync().Wait();

            foreach (var field in viewModel.FilteredEntityFields)
            {
                field.IsSelected = true;
                viewModel.ToggleEntityFieldVisibilityCommand.Execute(null);
            }

            var expectedVisibleFields = viewModel.FilteredEntityFields.Where(f => f.IsSelected).Select(f => f.Name).OrderBy(f => f).ToList();
            var actualVisibleFields = viewModel.VisibleFields.Select(f => f.Name).OrderBy(f => f).ToList();
            CollectionAssert.AreEqual(expectedVisibleFields, actualVisibleFields, "Mismathed visible fields");
        }

        [TestMethod]
        public void DetailedItemViewModelTests_VisibleFields_HideAllFields_ShowSelectedFields()
        {
            var viewModel = new DetailedItemViewModel(_story);
            viewModel.InitializeAsync().Wait();

            foreach (var field in viewModel.FilteredEntityFields)
            {
                field.IsSelected = false;
                viewModel.ToggleEntityFieldVisibilityCommand.Execute(null);
            }

            var expectedVisibleFields = viewModel.FilteredEntityFields.Where(f => f.IsSelected).Select(f => f.Name).ToList();
            var actualVisibleFields = viewModel.VisibleFields.Select(f => f.Name).ToList();
            CollectionAssert.AreEqual(expectedVisibleFields, actualVisibleFields, "Mismathed visible fields");
        }

        [TestMethod]
        public void DetailedItemViewModelTests_VisibleFields_MultipleEntitiesOfSameTime_ChangesAreReflectedInAllEntities()
        {
            var viewModel = new DetailedItemViewModel(_story);
            viewModel.InitializeAsync().Wait();

            var secondStory = StoryUtilities.CreateStory();
            try
            {
                var secondViewModel = new DetailedItemViewModel(_story);
                secondViewModel.InitializeAsync().Wait();

                ChangeFieldVisibility(viewModel, "Release", false);
                ChangeFieldVisibility(viewModel, "Committers", true);

                var expectedVisibleFields = viewModel.VisibleFields.Select(f => f.Name).OrderBy(f => f).ToList();
                var actualVisibleFields = secondViewModel.FilteredEntityFields.Where(f => f.IsSelected).Select(f => f.Name).OrderBy(f => f).ToList();
                CollectionAssert.AreEqual(expectedVisibleFields, actualVisibleFields, "Mismathed visible fields");
            }
            finally
            {
                EntityService.DeleteById<Story>(WorkspaceContext, secondStory.Id);
            }
        }

        #endregion

        #region DefaultFields

        [TestMethod]
        public void DetailedItemViewModelTests_ResetFieldsCustomizationCommand_AllFieldsAreVisible_ReturnToDefault()
        {
            ValidateResetCommand(true);
        }

        [TestMethod]
        public void DetailedItemViewModelTests_ResetFieldsCustomizationCommand_NoFieldIsVisible_ReturnToDefault()
        {
            ValidateResetCommand(false);
        }

        private void ValidateResetCommand(bool allFieldsVisible)
        {
            var viewModel = new DetailedItemViewModel(_story);
            viewModel.InitializeAsync().Wait();

            Assert.IsTrue(viewModel.OnlyDefaultFieldsAreShown, "Only default fields should be visible when detailed item is initialized");

            foreach (var field in viewModel.FilteredEntityFields)
            {
                field.IsSelected = allFieldsVisible;
                viewModel.ToggleEntityFieldVisibilityCommand.Execute(null);
            }

            Assert.IsFalse(viewModel.OnlyDefaultFieldsAreShown, "Visible fields should be different than default fields");

            viewModel.ResetFieldsCustomizationCommand.Execute(null);

            var dynamicFieldsCache = ExposedClass.From(typeof(FieldsCache));
            var cache = dynamicFieldsCache._defaultFieldsCache as FieldsCache.Metadata;

            var persistedVisibleFields = cache.data[Utility.GetConcreteEntityType(_story)];

            Assert.AreEqual(persistedVisibleFields.Count, viewModel.VisibleFields.Count(), "Inconsistent number of visible fields");
            foreach (var field in viewModel.VisibleFields)
            {
                Assert.IsTrue(persistedVisibleFields.Contains(field.Name), $"Field {field.Name} should be visible");
            }

            Assert.IsTrue(viewModel.OnlyDefaultFieldsAreShown, "Only default fields should be visible after reset command");
        }

        [TestMethod]
        public void DetailedItemViewModelTests_OnlyDefaultFieldsAreShown_ToggleShowingOnlyDefaultFields_True()
        {
            var viewModel = new DetailedItemViewModel(_story);
            viewModel.InitializeAsync().Wait();

            Assert.IsTrue(viewModel.OnlyDefaultFieldsAreShown, "Only default fields should be visible when detailed item is initialized");

            ChangeFieldVisibility(viewModel, "Release", false);
            Assert.IsFalse(viewModel.OnlyDefaultFieldsAreShown, "Not all default fields are visible");

            ChangeFieldVisibility(viewModel, "Release", true);
            Assert.IsTrue(viewModel.OnlyDefaultFieldsAreShown, "All default fields should be visible");

            ChangeFieldVisibility(viewModel, "Committers", true);
            Assert.IsFalse(viewModel.OnlyDefaultFieldsAreShown, "More fields than the default fields are visible");

            ChangeFieldVisibility(viewModel, "Committers", false);
            Assert.IsTrue(viewModel.OnlyDefaultFieldsAreShown, "All default fields should be visible");
        }

        #endregion

        private void ChangeFieldVisibility(DetailedItemViewModel viewModel, string fieldLabel, bool visibility)
        {
            var field = viewModel.FilteredEntityFields.FirstOrDefault(f => f.Label == fieldLabel);
            field.IsSelected = visibility;
            viewModel.ToggleEntityFieldVisibilityCommand.Execute(null);
        }

        #region CommentSectionVisibility

        [TestMethod]
        public void DetailedItemViewModelTests_CommentSectionVisibility_ToggleCommentSectionCommand_Success()
        {
            var viewModel = new DetailedItemViewModel(_story);
            Assert.IsFalse(viewModel.CommentSectionVisibility, "Default value for CommentSectionVisibility should be false");
            Assert.AreEqual(DetailedItemViewModel.ShowCommentsTooltip, viewModel.ShowCommentTooltip, "Mismatched default ShowCommentTooltip");

            viewModel.ToggleCommentSectionCommand.Execute(null);
            Assert.IsTrue(viewModel.CommentSectionVisibility, "Executing ToggleCommentSectionCommand should change CommentSectionVisibility to true");
            Assert.AreEqual(DetailedItemViewModel.HideCommentsTooltip, viewModel.ShowCommentTooltip, "Mismatched ShowCommentTooltip after executing ToggleCommentSectionCommand");

            viewModel.ToggleCommentSectionCommand.Execute(null);
            Assert.IsFalse(viewModel.CommentSectionVisibility, "Executing ToggleCommentSectionCommand again should change CommentSectionVisibility to false");
            Assert.AreEqual(DetailedItemViewModel.ShowCommentsTooltip, viewModel.ShowCommentTooltip, "Mismatched ShowCommentTooltip after executing ToggleCommentSectionCommand again");
        }

        #endregion

        [TestMethod]
        public void DetailedItemViewModelTests_VariousProperties_BeforeAndAfterInitialize_Success()
        {
            var viewModel = new DetailedItemViewModel(_story);
            Assert.AreEqual(WindowMode.Loading, viewModel.Mode, "Mismatched initial mode");
            Assert.AreEqual(string.Empty, viewModel.Phase, "Mismatched initial phase");

            viewModel.InitializeAsync().Wait();
            Assert.AreEqual(WindowMode.Loaded, viewModel.Mode, "Mismatched initial mode");
            Assert.AreEqual("New", viewModel.Phase, "Mismatched initial phase");

            var entityTypeInformation = EntityTypeRegistry.GetEntityTypeInformation(_story);
            Assert.AreEqual(entityTypeInformation.ShortLabel, viewModel.IconText, "Mismatched icon text");
            Assert.AreEqual(entityTypeInformation.Color, viewModel.IconBackgroundColor, "Mismatched icon background color");
        }
    }
}
