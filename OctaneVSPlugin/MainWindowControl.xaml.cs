﻿//------------------------------------------------------------------------------
// <copyright file="MainWindowControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using MicroFocus.Adm.Octane.Api.Core.Entities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using octane_visual_studio_plugin;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hpe.Nga.Octane.VisualStudio
{
    /// <summary>
    /// Interaction logic for MainWindowControl.
    /// </summary>
    public partial class MainWindowControl : UserControl
    {
        private readonly OctaneMyItemsViewModel viewModel;
        private MainWindowPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowControl"/> class.
        /// </summary>
        public MainWindowControl()
        {
            this.InitializeComponent();
            viewModel = new OctaneMyItemsViewModel();
            this.DataContext = viewModel;
        }

        public void SetPackage(MainWindowPackage package)
        {
            this.package = package;
            viewModel.SetPackage(package);
        }

        OctaneItemViewModel SelectedItem
        {
            get
            {
                return (OctaneItemViewModel)results.SelectedItem;
            }
        }

        private void OpenInBrowser_Click(object sender, RoutedEventArgs e)
        {
            // url: http://myd-vm10629.hpeswlab.net:8081
            // http://myd-vm10629.hpeswlab.net:8081/ui/entity-navigation?p=1001/1002&entityType=work_item&id=1111

            string url = string.Format("{0}/ui/entity-navigation?p={1}/{2}&entityType={3}&id={4}",
                package.AlmUrl,
                package.SharedSpaceId,
                package.WorkSpaceId,
                SelectedItem.TypeName,
                SelectedItem.ID);

            try
            {
                // Open the URL in the user's default browser.
                System.Diagnostics.Process.Start(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fail to open the browser\n\n" + ex.Message, "Octane ALM", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            ViewEntityDetails(SelectedItem.ID);
        }

        private void ViewParentDetails_Click(object sender, RoutedEventArgs e)
        {
            var commentViewModel = SelectedItem as CommentViewModel;
            if (commentViewModel == null)
            {
                Debug.Fail("Entity should be a comment.");
                return;
            }

            ViewEntityDetails(commentViewModel.ParentEntity.Id);
        }

        private async void ViewEntityDetails(EntityId id)
        {
            var entity = await viewModel.GetItem(id);

            ToolWindowPane window = CreateDetailsWindow(entity);
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        private ToolWindowPane CreateDetailsWindow(OctaneItemViewModel item)
        {
            // Create the window with the first free ID.   
            DetailsToolWindow toolWindow = (DetailsToolWindow)this.package.FindToolWindow(typeof(DetailsToolWindow), GetItemIDAsInt(item), true);

            if ((null == toolWindow) || (null == toolWindow.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            toolWindow.SetWorkItem(item);

            return toolWindow;
        }

        /// <summary>
        /// Octane treat WorkItem ID as long (64 bit) and Visual Studio needs int (32 bit) to identify tool windows.
        /// This function safely convert long to int.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private int GetItemIDAsInt(OctaneItemViewModel item)
        {
            return item.GetHashCode();
        }

        private void GenerateCommitMsg_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem.IsSupportCopyCommitMessage)
            {
                string message = SelectedItem.CommitMessage;
                Clipboard.SetText(message);
            }
        }

        private void results_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    GenerateCommitMsg_Click(sender, e);
                }
                else
                {
                    OpenInBrowser_Click(sender, e);
                }
            }
            else
            {
                if (SelectedItem is CommentViewModel)
                {
                    ViewParentDetails_Click(sender, e);
                }
                else
                {
                    ViewDetails_Click(sender, e);
                }
            }
        }

        private async void DownloadGherkinScript_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Test test = (Test)SelectedItem.Entity;
                string script = await viewModel.GetGherkinScript(test);

                package.CreateFile(test.Name, script);
            }
            catch
            {
                MessageBox.Show("Fail to get test script");
            }
        }

        private void ListMenu_Opened(object sender, RoutedEventArgs e)
        {
            var cm = (ContextMenu)sender;

            var selectedItemIsComment = SelectedItem is CommentViewModel;

            // Show the "View details" item for all items except comment
            var viewDetailsMenuItem = (MenuItem)cm.Items[0];
            viewDetailsMenuItem.Visibility = !selectedItemIsComment ? Visibility.Visible : Visibility.Collapsed;

            // Show the "View parent details" item only to comment entities
            var viewParentDetailsMenuItem = (MenuItem)cm.Items[1];
            viewParentDetailsMenuItem.Visibility = selectedItemIsComment ? Visibility.Visible : Visibility.Collapsed;

            // Show the "Copy Comment Message" item only to items which supports it
            var copyCommitMessageMenuItem = (MenuItem)cm.Items[3];
            copyCommitMessageMenuItem.Visibility = SelectedItem.IsSupportCopyCommitMessage ? Visibility.Visible : Visibility.Collapsed;

            // Show the "Download Gherkin Test" item only for gherkind test items
            var gherkinTestMenuItem = (MenuItem)cm.Items[4];
            gherkinTestMenuItem.Visibility = (SelectedItem.SubType == "gherkin_test") ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}