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

using MicroFocus.Adm.Octane.VisualStudio.Common;
using MicroFocus.Adm.Octane.VisualStudio.Tests.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MicroFocus.Adm.Octane.VisualStudio.Tests.Common
{
    /// <summary>
    /// Test class for <see cref="WorkspaceSessionPersistanceManager"/>
    /// </summary>
    [TestClass]
    public class SearchHistoryManagerTests : BaseOctanePluginTest
    {
        private string _url;
        private long _ssid;
        private long _wsid;
        private string _username;
        private string _password;

        protected override void TestInitializeInternal()
        {
            _url = OctaneConfiguration.Url;
            _ssid = OctaneConfiguration.SharedSpaceId;
            _wsid = OctaneConfiguration.WorkSpaceId;
            _username = OctaneConfiguration.Username;
            _password = OctaneConfiguration.Password;
        }

        protected override void TestCleanupInternal()
        {
            OctaneConfiguration.Url = _url;
            OctaneConfiguration.SharedSpaceId = _ssid;
            OctaneConfiguration.WorkSpaceId = _wsid;
            OctaneConfiguration.Username = _username;
            OctaneConfiguration.Password = _password;
        }

        [TestMethod]
        public void SearchHistoryManagerTests_UpdateHistory_NullFilter_Success()
        {
            OctaneConfiguration.Url = Guid.NewGuid().ToString();

            CollectionAssert.AreEqual(new List<string>(), WorkspaceSessionPersistanceManager.History, "Invalid initial history");

            WorkspaceSessionPersistanceManager.UpdateHistory("a");
            WorkspaceSessionPersistanceManager.UpdateHistory("b");
            var expectedHistory = new List<string> { "b", "a" };
            CollectionAssert.AreEqual(expectedHistory, WorkspaceSessionPersistanceManager.History, "Mismatched history");

            WorkspaceSessionPersistanceManager.UpdateHistory(string.Empty);

            CollectionAssert.AreEqual(expectedHistory, WorkspaceSessionPersistanceManager.History, "Mismatched history after trying to add null");
        }

        [TestMethod]
        public void SearchHistoryManagerTests_UpdateHistory_EmptyFilter_Success()
        {
            OctaneConfiguration.Url = Guid.NewGuid().ToString();

            CollectionAssert.AreEqual(new List<string>(), WorkspaceSessionPersistanceManager.History, "Invalid initial history");

            WorkspaceSessionPersistanceManager.UpdateHistory("a");
            WorkspaceSessionPersistanceManager.UpdateHistory("b");
            var expectedHistory = new List<string> { "b", "a" };
            CollectionAssert.AreEqual(expectedHistory, WorkspaceSessionPersistanceManager.History, "Mismatched history");

            WorkspaceSessionPersistanceManager.UpdateHistory(string.Empty);

            CollectionAssert.AreEqual(expectedHistory, WorkspaceSessionPersistanceManager.History, "Mismatched history after trying to add empty string");
        }

        [TestMethod]
        public void SearchHistoryManagerTests_UpdateHistory_SaveMoreElementsThanMax_Success()
        {
            OctaneConfiguration.Url = Guid.NewGuid().ToString();

            CollectionAssert.AreEqual(new List<string>(), WorkspaceSessionPersistanceManager.History, "Invalid initial history");

            var expectedHistory = new List<string>();
            for (var i = 0; i < WorkspaceSessionPersistanceManager.MaxSearchHistorySize + 2; i++)
            {
                expectedHistory.Add(i.ToString());
                WorkspaceSessionPersistanceManager.UpdateHistory(i.ToString());
            }

            expectedHistory.Reverse();
            expectedHistory = expectedHistory.Take(WorkspaceSessionPersistanceManager.MaxSearchHistorySize).ToList();

            CollectionAssert.AreEqual(expectedHistory, WorkspaceSessionPersistanceManager.History, "Mismatched history");
        }

        [TestMethod]
        public void SearchHistoryManagerTests_UpdateHistory_SaveExactlyMaxElements_Success()
        {
            OctaneConfiguration.Url = Guid.NewGuid().ToString();

            CollectionAssert.AreEqual(new List<string>(), WorkspaceSessionPersistanceManager.History, "Invalid initial history");

            var expectedHistory = new List<string>();
            for (var i = 0; i < WorkspaceSessionPersistanceManager.MaxSearchHistorySize; i++)
            {
                expectedHistory.Add(i.ToString());
                WorkspaceSessionPersistanceManager.UpdateHistory(i.ToString());
            }

            expectedHistory.Reverse();

            CollectionAssert.AreEqual(expectedHistory, WorkspaceSessionPersistanceManager.History, "Mismatched history");
        }

        [TestMethod]
        public void SearchHistoryManagerTests_UpdateHistory_ResetApplication_Success()
        {
            OctaneConfiguration.Url = Guid.NewGuid().ToString();

            CollectionAssert.AreEqual(new List<string>(), WorkspaceSessionPersistanceManager.History, "Invalid initial history");

            WorkspaceSessionPersistanceManager.UpdateHistory("1");

            var exposedHistoryManager = ExposedClass.From(typeof(WorkspaceSessionPersistanceManager));
            exposedHistoryManager._metadata = null;

            CollectionAssert.AreEqual(new List<string> { "1" }, WorkspaceSessionPersistanceManager.History, "Mismatched history after reset");

            WorkspaceSessionPersistanceManager.UpdateHistory("2");

            CollectionAssert.AreEqual(new List<string> { "2", "1" }, WorkspaceSessionPersistanceManager.History, "Mismatched history");
        }


        [TestMethod]
        public void SearchHistoryManagerTests_UpdateHistory_SwitchUrl_Success()
        {
            ValidateConnectionSwitch(() => OctaneConfiguration.Url = Guid.NewGuid().ToString());
        }

        [TestMethod]
        public void SearchHistoryManagerTests_UpdateHistory_SwitchWorkspace_Success()
        {
            ValidateConnectionSwitch(() => OctaneConfiguration.WorkSpaceId = new Random().Next());
        }

        [TestMethod]
        public void SearchHistoryManagerTests_UpdateHistory_SwitchSharedSpace_Success()
        {
            ValidateConnectionSwitch(() => OctaneConfiguration.SharedSpaceId = new Random().Next());
        }

        [TestMethod]
        public void SearchHistoryManagerTests_UpdateHistory_SwitchUser_Success()
        {
            ValidateConnectionSwitch(() => OctaneConfiguration.Username = Guid.NewGuid().ToString());
        }

        private void ValidateConnectionSwitch(Action connectionSwitchAction)
        {
            OctaneConfiguration.Url = Guid.NewGuid().ToString();

            CollectionAssert.AreEqual(new List<string>(), WorkspaceSessionPersistanceManager.History, "Invalid initial history");

            WorkspaceSessionPersistanceManager.UpdateHistory("1");

            connectionSwitchAction();

            CollectionAssert.AreEqual(new List<string>(), WorkspaceSessionPersistanceManager.History, "Mismatched history after reset");

            WorkspaceSessionPersistanceManager.UpdateHistory("2");

            CollectionAssert.AreEqual(new List<string> { "2" }, WorkspaceSessionPersistanceManager.History, "Mismatched history");
        }
    }
}
