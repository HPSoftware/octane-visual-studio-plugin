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

using System.Windows;
using System.Windows.Controls;

namespace MicroFocus.Adm.Octane.VisualStudio.View
{
    /// <summary>
    /// Interaction logic for ConnectionSettingsView.xaml
    /// </summary>
    public partial class ConnectionSettingsView : UserControl
    {
        public ConnectionSettingsView()
        {
            InitializeComponent();
        }

        internal ConnectionSettings optionsPage;

        public void Initialize()
        {
            //textBox1.Text = optionsPage.OptionString;
            //populate the details from persistance url shid wid user and pass
            this.DataContext = optionsPage;
            passwordTextBox.Password = optionsPage.Password;
        }

        private void TestConnection(object sender, RoutedEventArgs e)
        {
            optionsPage.InfoLabel = "";
            optionsPage.TestConnection();
        }

        private void ClearSettings(object sender, RoutedEventArgs e)
        {
            serverUrlTextBox.Text = "";
            sharedSpaceTextBox.Text = "";
            workspaceTextBox.Text = "";
            usernameTextBox.Text = "";
            passwordTextBox.Password = "";
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (optionsPage != null)
            {
                optionsPage.Password = ((PasswordBox)sender).Password;
            }
        }
    }
}