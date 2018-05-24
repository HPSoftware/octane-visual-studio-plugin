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
using Microsoft.VisualStudio.Shell.Interop;
using octane_visual_studio_plugin;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MicroFocus.Adm.Octane.VisualStudio.View
{
    /// <summary>
    /// Manager for keeping track of all the opened plugin windows
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal static class PluginWindowManager
    {
        private static readonly object Lock = new object();

        private static int _windowCounter;
        private static readonly Dictionary<string, WindowInfo> OpenedDetailWindows = new Dictionary<string, WindowInfo>();

        /// <summary>
        /// Return the details window for the given entity
        /// </summary>
        internal static DetailsToolWindow ObtainDetailsWindow(MainWindowPackage package, BaseEntity entity)
        {
            WorkspaceSessionPersistanceManager.RegisterEntityWithDetailedView(entity);
            return ObtainWindow<DetailsToolWindow>(package, GetUniqueIdentifier(entity));
        }

        /// <summary>
        /// Return the search window
        /// </summary>
        internal static SearchToolWindow ObtainSearchWindow(MainWindowPackage package)
        {
            return package == null ? null : ObtainWindow<SearchToolWindow>(package, "SearchWindow");
        }

        private static T ObtainWindow<T>(MainWindowPackage package, string uniqueId) where T : ToolWindowPane
        {
            bool windowAlreadyOpened = false;
            int windowId = _windowCounter + 1;

            lock (Lock)
            {
                WindowInfo windowInfo;
                if (OpenedDetailWindows.TryGetValue(uniqueId, out windowInfo))
                {
                    windowAlreadyOpened = true;
                    windowId = windowInfo.WindowId;
                }

                // open the existing window with the current windowId
                // if a window wasn't found, then a new one is created
                var toolWindow = (T)package.FindToolWindow(typeof(T), windowId, true);
                if (toolWindow?.Frame == null)
                {
                    throw new NotSupportedException("Cannot create tool window");
                }

                if (!windowAlreadyOpened)
                {
                    OpenedDetailWindows.Add(uniqueId, new WindowInfo(++_windowCounter, toolWindow));
                }

                IVsWindowFrame windowFrame = (IVsWindowFrame)toolWindow.Frame;
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());

                return toolWindow;
            }
        }

        private static string GetUniqueIdentifier(BaseEntity entity)
        {
            return Utility.GetConcreteEntityType(entity) + entity.Id;
        }

        /// <summary>
        /// Unregister the given details window from the window manager
        /// </summary>
        internal static void UnregisterDetailsWindow(DetailsToolWindow detailWindow)
        {
            var windowControl = detailWindow.Content as OctaneToolWindowControl;
            if (windowControl == null)
                return;

            var viewModel = windowControl.DataContext as DetailedItemViewModel;
            if (viewModel == null)
                return;

            var uniqueId = GetUniqueIdentifier(viewModel.Entity);
            lock (Lock)
            {
                OpenedDetailWindows.Remove(uniqueId);
                WorkspaceSessionPersistanceManager.UnregisterEntityWithDetailedView(viewModel.Entity);
            }
        }

        /// <summary>
        /// Manually close all the currently opened details windows
        /// </summary>
        internal static void CloseAllDetailsWindows()
        {
            lock (Lock)
            {
                foreach (var windowInfo in OpenedDetailWindows.Values.ToList())
                {
                    var frame = windowInfo.WindowPane.Frame as IVsWindowFrame;
                    if (frame != null)
                        frame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
                }
                OpenedDetailWindows.Clear();

                WorkspaceSessionPersistanceManager.UnregisterAllEntities();
            }
        }

        [ExcludeFromCodeCoverage]
        private struct WindowInfo
        {
            public int WindowId { get; }
            public ToolWindowPane WindowPane { get; }

            public WindowInfo(int windowId, ToolWindowPane windowPane)
            {
                WindowId = windowId;
                WindowPane = windowPane;
            }
        }
    }
}
