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

using EnvDTE;
using MicroFocus.Adm.Octane.VisualStudio;
using MicroFocus.Adm.Octane.VisualStudio.Common;
using MicroFocus.Adm.Octane.VisualStudio.View;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace octane_visual_studio_plugin
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(MainWindow), Style = VsDockStyle.Tabbed, Window = EnvDTE.Constants.vsWindowKindOutput)]
    [ProvideToolWindow(typeof(DetailsToolWindow), MultiInstances = true, Style = VsDockStyle.Tabbed, Window = MainWindow.WINDOW_ID, Transient = true)]
    [ProvideToolWindow(typeof(SearchToolWindow), MultiInstances = true, Style = VsDockStyle.Tabbed, Window = MainWindow.WINDOW_ID, Transient = true)]
    [Guid(MainWindowPackage.PackageGuidString)]
    [ProvideOptionPage(typeof(OptionsPage), "ALM Octane", "Server Details", 0, 0, true)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.SkipWhenUIContextRulesActive)]
    [ExcludeFromCodeCoverage]
    public sealed class MainWindowPackage : Package
    {
        /// <summary>
        /// MainWindowPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "3e3ad6fc-724d-49a3-8249-317a835472c1";

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindowPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        protected override int QueryClose(out bool pfCanClose)
        {
            pfCanClose = true;
            WorkspaceSessionPersistanceManager.MarkIsShuttingDownOperation();
            return VSConstants.S_OK;
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            IVsActivityLog log = GetService(typeof(SVsActivityLog)) as IVsActivityLog;
            log.LogEntry(3, "Octane", "Started Octane plugin");
            MainWindowCommand.Initialize(this);
            base.Initialize();
            OctaneCommand.Initialize(this);

            var optionsPage = (OptionsPage)GetDialogPage(typeof(OptionsPage));
            optionsPage.LoadSettingsFromStorage();
        }

        #endregion


        private void TestCommitMessage()
        {
            //GitFileStatusTracker tracker;
            //var repositoryGraph = tracker.RepositoryGraph;
            //var commit = repositoryGraph.GetCommit(commitId);
            //// commit.Message;
            //IVsScc
        }

        internal void CreateFile(string fileName, string content)
        {
            DTE dte = (DTE)GetService(typeof(DTE));
            dte.ItemOperations.NewFile("General\\Text File", fileName);

            TextSelection textSel = (TextSelection)dte.ActiveDocument.Selection;
            TextDocument textDoc = (TextDocument)dte.ActiveDocument.Object();

            textSel.SelectAll();
            textSel.Delete();
            textSel.Insert(content);

            textSel.GotoLine(1);
        }
    }
}
