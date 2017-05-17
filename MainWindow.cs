﻿//------------------------------------------------------------------------------
// <copyright file="MainWindow.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Hpe.Nga.Octane.VisualStudio
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;
    using octane_visual_studio_plugin;

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
    [Guid("af5c5224-1b4a-444f-923f-2fc9e06f7a40")]
    public class MainWindow : ToolWindowPane
    {
        private readonly MainWindowControl mainWindowControl;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow() : base(null)
        {
            this.Caption = "ALM Octane";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            mainWindowControl = new MainWindowControl();
            this.Content = mainWindowControl;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            var mainWindowPackage = (MainWindowPackage)Package;
            mainWindowControl.Package = mainWindowPackage;
        }
    }
}
