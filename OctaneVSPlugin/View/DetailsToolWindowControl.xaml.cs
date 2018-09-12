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
using MicroFocus.Adm.Octane.VisualStudio.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MicroFocus.Adm.Octane.VisualStudio.View
{
    /// <summary>
    /// Interaction logic for OctaneToolWindowControl.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class OctaneToolWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OctaneToolWindowControl"/> class.
        /// </summary>
        public OctaneToolWindowControl()
        {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty BindableSourceProperty =
            DependencyProperty.RegisterAttached("BindableSource", typeof(string), typeof(OctaneToolWindowControl), new UIPropertyMetadata(null, BindableSourcePropertyChanged));

        public static string GetBindableSource(DependencyObject obj)
        {
            return (string)obj.GetValue(BindableSourceProperty);
        }

        public static void SetBindableSource(DependencyObject obj, string value)
        {
            obj.SetValue(BindableSourceProperty, value);
        }

        private static void BindableSourcePropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            WebBrowser browser = o as WebBrowser;
            if (browser == null)
                return;

            var itemViewModel = browser.DataContext as DetailedItemViewModel;
            if (itemViewModel == null)
                return;

            string octaneImageBaseUrl = string.Format("{0}/api/shared_spaces/{1}/workspaces/{2}/attachments/",
                OctaneConfiguration.Url,
                OctaneConfiguration.SharedSpaceId,
                OctaneConfiguration.WorkSpaceId);

            string htmlWithImageUrls = itemViewModel.Description.Replace("file://[IMAGE_BASE_PATH_PLACEHOLDER]", octaneImageBaseUrl);

            if (!string.IsNullOrWhiteSpace(htmlWithImageUrls))
            {
                browser.NavigateToString(htmlWithImageUrls);
            }
        }

        private void DescBrowser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (e.Uri != null)
            {
                e.Cancel = true;
                System.Diagnostics.Process.Start(e.Uri.ToString());
            }
        }

        private void FlowDocumentScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled)
                return;

            // The FlowDocumentScrollViewer control captures the scroll event when the scroll bars are hidden
            // and it is placed inside a scrollable parent, so we need to raise the scroll event to its parent
            // For more details see discussion at:
            // https://social.msdn.microsoft.com/Forums/vstudio/en-US/4223ebb1-863e-4109-bb02-ee972d406f4c/make-it-scroll-flowdocumentscrollviewer?forum=wpf
            e.Handled = true;
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = MouseWheelEvent,
                Source = sender
            };
            if (((Control)sender).Parent is UIElement parent)
                parent.RaiseEvent(eventArg);
        }

        private void ButtonDateClick(object sender, RoutedEventArgs e)
        {
            this.Resources["DateButtonVisibility"] = Visibility.Hidden;
            this.Resources["DatePickerVisibility"] = Visibility.Visible;
        }
    }
}