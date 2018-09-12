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
using MicroFocus.Adm.Octane.VisualStudio.ViewModel;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace MicroFocus.Adm.Octane.VisualStudio.View
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("d19915c9-3dea-4d5c-aa56-bd1fed3a7ab3")]
    [ExcludeFromCodeCoverage]
    public class DetailsToolWindow : ToolWindowPane
    {
        private readonly OctaneToolWindowControl detailsControl;

        private DetailedItemViewModel _divm;

        private static readonly HashSet<string> supportedEntityTypes = new HashSet<string>
        {
            // work item
            WorkItem.SUBTYPE_DEFECT,
            WorkItem.SUBTYPE_STORY,
            WorkItem.SUBTYPE_QUALITY_STORY,

            // task
            Api.Core.Entities.Task.TYPE_TASK,

            // test
            TestGherkin.SUBTYPE_GHERKIN_TEST,
            Test.SUBTYPE_MANUAL_TEST,

            // run
            RunManual.SUBTYPE_RUN_MANUAL,
            RunSuite.SUBTYPE_RUN_SUITE,

            // requirement
            Requirement.SUBTYPE_DOCUMENT
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="DetailsToolWindow"/> class.
        /// </summary>
        public DetailsToolWindow() : base(null)
        {
            Caption = "Loading backlog item...";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            detailsControl = new OctaneToolWindowControl();
            this.Content = detailsControl;
        }

        /// <inheritdoc/>
        protected override void OnClose()
        {
            DetachViewModelFromFieldsCache();
            PluginWindowManager.UnregisterDetailsWindow(this);
            base.OnClose();
        }

        private void DetachViewModelFromFieldsCache()
        {
            var control = Content as OctaneToolWindowControl;
            if (control == null)
                return;

            var detailedItemViewModel = control.DataContext as DetailedItemViewModel;
            if (detailedItemViewModel == null)
                return;

            FieldsCache.Instance.Detach(detailedItemViewModel);
        }

        void PropertyChangedInDetailedItemViewModel(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsAFieldChanged")
            {
                var entityTypeInfo = EntityTypeRegistry.GetEntityTypeInformation(_divm.Entity);
                Caption = $"{entityTypeInfo?.ShortLabel} *{_divm.ID}";
            }

        }
        /// <summary>
        /// Load the necessary information for the given entity
        /// </summary>
        internal void LoadEntity(BaseEntity entity)
        {
            var viewModel = new DetailedItemViewModel(entity);
            _divm = viewModel;
            viewModel.InitializeAsync();

            var entityTypeInformation = EntityTypeRegistry.GetEntityTypeInformation(viewModel.Entity);
            Caption = $"{entityTypeInformation?.ShortLabel} {viewModel.ID}";
            viewModel.PropertyChanged += PropertyChangedInDetailedItemViewModel;
            detailsControl.DataContext = viewModel;
        }

        /// <summary>
        /// Returns whether the given type can be shown by the details window.
        /// </summary>
        public static bool IsEntityTypeSupported(string type)
        {
            return supportedEntityTypes.Contains(type);
        }
    }
}
