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
using MicroFocus.Adm.Octane.VisualStudio.Tests.Utilities.Entity;
using MicroFocus.Adm.Octane.VisualStudio.ViewModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Utility = MicroFocus.Adm.Octane.VisualStudio.Tests.Utilities.Utility;

namespace MicroFocus.Adm.Octane.VisualStudio.Tests.ViewModel
{
    /// <summary>
    /// Test class for <see cref="OctaneMyItemsViewModel"/>
    /// </summary>
    [TestClass]
    public class OctaneMyItemsViewModelTests : BaseOctanePluginTest
    {
        [TestMethod]
        public void OctaneMyItemsViewModelTests_Constructor_DefaultValues_Success()
        {
            var viewModel = new OctaneMyItemsViewModel();
            Assert.AreEqual(MainWindowMode.FirstTime, viewModel.Mode, "Mismatched mode");
            Assert.IsTrue(string.IsNullOrEmpty(viewModel.LastExceptionMessage), "Mismatched exception message");
            Assert.AreEqual(0, viewModel.MyItems.Count(), "Mismatched MyItems count");
        }

        #region MyItems

        [TestMethod]
        public void OctaneMyItemsViewModelTests_MyItems_Supported_Story_Success()
        {
            ValidateType(StoryUtilities.CreateStory(), 1);
        }

        [TestMethod]
        public void OctaneMyItemsViewModelTests_MyItems_Supported_Task_Success()
        {
            var story = StoryUtilities.CreateStory();
            try
            {
                ValidateType(TaskUtilities.CreateTask(story), 1);
            }
            finally
            {
                EntityService.DeleteById<Story>(WorkspaceContext, story.Id);
            }
        }

        [TestMethod]
        public void OctaneMyItemsViewModelTests_MyItems_Supported_QualityStory_Success()
        {
            ValidateType(QualityStoryUtilities.CreateQualityStory(), 1);
        }

        [TestMethod]
        public void OctaneMyItemsViewModelTests_MyItems_Supported_Defect_Success()
        {
            ValidateType(DefectUtilities.CreateDefect(), 1);
        }

        [TestMethod]
        public void OctaneMyItemsViewModelTests_MyItems_Supported_GherkinTest_Success()
        {
            ValidateType(TestGherkinUtilities.CreateGherkinTest(), 1);
        }

        [TestMethod]
        public void OctaneMyItemsViewModelTests_MyItems_Supported_ManualTest_Success()
        {
            ValidateType(TestManualUtilities.CreateManualTest(), 1);
        }

        [TestMethod]
        public void OctaneMyItemsViewModelTests_MyItems_Supported_ManualRun_Success()
        {
            var manualTest = TestManualUtilities.CreateManualTest();
            try
            {
                ValidateType(RunManualUtilities.CreateManualRun(manualTest), 1);
            }
            finally
            {
                EntityService.DeleteById<TestManual>(WorkspaceContext, manualTest.Id);
            }
        }

        [TestMethod]
        public void OctaneMyItemsViewModelTests_MyItems_Supported_SuiteRun_Success()
        {
            var testSuite = TestSuiteUtilities.CreateTestSuite();
            try
            {
                ValidateType(RunSuiteUtilities.CreateSuiteRun(testSuite), 1);
            }
            finally
            {
                EntityService.DeleteById<TestSuite>(WorkspaceContext, testSuite.Id);
            }
        }

        [TestMethod]
        public void OctaneMyItemsViewModelTests_MyItems_NotSupported_Epic_Success()
        {
            ValidateType(EpicUtilities.CreateEpic(), 0);
        }

        [TestMethod]
        public void OctaneMyItemsViewModelTests_MyItems_NotSupported_Feature_Success()
        {
            var epic = EpicUtilities.CreateEpic();
            try
            {
                ValidateType(FeatureUtilities.CreateFeature(epic), 0);
            }
            finally
            {
                EntityService.DeleteById<Epic>(WorkspaceContext, epic.Id);
            }
        }

        private void ValidateType<T>(T entity, int expectedCount) where T : BaseEntity
        {
            try
            {
                var viewModel = new OctaneMyItemsViewModel();
                viewModel.LoadMyItemsAsync().Wait();

                Assert.AreEqual(expectedCount, viewModel.MyItems.Count(i => i.ID == entity.Id && i.Entity.Name == entity.Name),
                    $"Couldn't find exactly one entity with the name {entity.Name}");
            }
            finally
            {
                EntityService.DeleteById<T>(WorkspaceContext, entity.Id);
            }
        }

        #endregion

        #region Refresh

        [TestMethod]
        public void OctaneMyItemsViewModelTests_Refresh_ItemAdded_Success()
        {
            var defect = DefectUtilities.CreateDefect();
            Defect newDefect = null;
            try
            {
                var viewModel = new OctaneMyItemsViewModel();
                viewModel.LoadMyItemsAsync().Wait();

                var myItems = viewModel.MyItems.ToList();
                Assert.AreEqual(1, myItems.Count(item => item.ID == defect.Id), $"Couldn't find entity {defect.Name}");

                newDefect = DefectUtilities.CreateDefect();

                viewModel.RefreshCommand.Execute(null);

                Utility.WaitUntil(() => viewModel.Mode == MainWindowMode.ItemsLoaded,
                    "Timeout waiting for Refresh to finish", new TimeSpan(0, 0, 30));

                myItems = viewModel.MyItems.ToList();
                Assert.AreEqual(1, myItems.Count(item => item.ID == defect.Id), $"Couldn't find entity {defect.Name} after refresh");
                Assert.AreEqual(1, myItems.Count(item => item.ID == newDefect.Id), $"Couldn't find entity {newDefect.Name} after refresh");
            }
            finally
            {
                EntityService.DeleteById<Defect>(WorkspaceContext, defect.Id);
                if (newDefect != null)
                    EntityService.DeleteById<Defect>(WorkspaceContext, newDefect.Id);
            }
        }

        [TestMethod]
        public void OctaneMyItemsViewModelTests_Refresh_ItemRemoved_Success()
        {
            var defect = DefectUtilities.CreateDefect();
            bool removed = false;
            try
            {
                var viewModel = new OctaneMyItemsViewModel();
                viewModel.LoadMyItemsAsync().Wait();

                var myItems = viewModel.MyItems.ToList();
                Assert.AreEqual(1, myItems.Count(item => item.ID == defect.Id), $"Couldn't find entity {defect.Name}");

                EntityService.DeleteById<Defect>(WorkspaceContext, defect.Id);
                removed = true;

                viewModel.RefreshCommand.Execute(null);

                Utility.WaitUntil(() => viewModel.Mode == MainWindowMode.ItemsLoaded,
                    "Timeout waiting for Refresh to finish", new TimeSpan(0, 0, 30));

                myItems = viewModel.MyItems.ToList();
                Assert.AreEqual(0, myItems.Count(item => item.ID == defect.Id), $"Found previously deleted entity {defect.Name} after refresh");
            }
            finally
            {
                if (!removed)
                    EntityService.DeleteById<Defect>(WorkspaceContext, defect.Id);
            }
        }

        #endregion

        #region SearchHistory

        [TestMethod]
        public void OctaneMyItemsViewModelTests_SearchHistory_ItemsNotLoaded_Success()
        {
            var viewModel = new OctaneMyItemsViewModel();
            Assert.IsNull(viewModel.SearchFilter, "Invalid initial state for SearchFilter");
            CollectionAssert.AreEqual(new List<string>(), viewModel.SearchHistory.ToList(), "Invalid search history");
        }

        [TestMethod]
        public void OctaneMyItemsViewModelTests_SearchHistory_SearchForMoreThanMax_Success()
        {
            var viewModel = new OctaneMyItemsViewModel();
            viewModel.LoadMyItemsAsync().Wait();

            var expectedHistory = ExecuteSearches(viewModel, WorkspaceSessionPersistanceManager.MaxSearchHistorySize + 1);

            expectedHistory.Reverse();

            CollectionAssert.AreEqual(expectedHistory.Take(WorkspaceSessionPersistanceManager.MaxSearchHistorySize).ToList(), viewModel.SearchHistory.ToList(), "Invalid search history");
        }

        [TestMethod]
        public void OctaneMyItemsViewModelTests_SearchHistory_SwitchBackToValidWorkspaceAfterTryingInvalidWorkspace_Success()
        {
            var originalWorkspaceId = OctaneConfiguration.WorkSpaceId;
            try
            {
                var viewModel = new OctaneMyItemsViewModel();
                viewModel.LoadMyItemsAsync().Wait();

                var expectedHistory = ExecuteSearches(viewModel, WorkspaceSessionPersistanceManager.MaxSearchHistorySize + 1);
                expectedHistory.Reverse();
                expectedHistory = expectedHistory.Take(WorkspaceSessionPersistanceManager.MaxSearchHistorySize).ToList();

                OctaneConfiguration.WorkSpaceId = 1000000;

                viewModel.LoadMyItemsAsync().Wait();

                Assert.AreEqual(MainWindowMode.FailToLoad, viewModel.Mode, "Mismatched window mode after switching to invalid workspace");
                CollectionAssert.AreEqual(new List<string>(), viewModel.SearchHistory.ToList(),
                    "Invalid search history after switching to invalid workspace");

                OctaneConfiguration.WorkSpaceId = originalWorkspaceId;

                viewModel.LoadMyItemsAsync().Wait();

                Assert.AreEqual(MainWindowMode.ItemsLoaded, viewModel.Mode, "Mismatched window mode after reverting to working workspace");
                Assert.AreEqual(string.Empty, viewModel.SearchFilter, "Mismatched search filter after reverting to working workspace");
                CollectionAssert.AreEqual(expectedHistory, viewModel.SearchHistory.ToList(), "Invalid search history after reverting to working workspace");
            }
            finally
            {
                OctaneConfiguration.WorkSpaceId = originalWorkspaceId;
            }
        }

        #endregion

        private List<string> ExecuteSearches(OctaneMyItemsViewModel viewModel, int count)
        {
            var expectedHistory = new List<string>();
            for (int i = 0; i < count; i++)
            {
                var guid = Guid.NewGuid().ToString();
                expectedHistory.Add(guid);

                viewModel.SearchFilter = guid;
                viewModel.SearchCommand.Execute(null);
            }

            return expectedHistory;
        }
    }
}
