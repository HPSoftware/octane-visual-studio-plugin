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


using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MicroFocus.Adm.Octane.Api.Core.Entities;
using MicroFocus.Adm.Octane.Api.Core.Services;
using MicroFocus.Adm.Octane.VisualStudio.ViewModel;

namespace MicroFocus.Adm.Octane.VisualStudio.View
{
    /// <summary>
    /// Interaction logic for EntityComboBox.xaml
    /// </summary>
    public partial class EntityComboBox : UserControl
    {
        public Visibility HasSearchBox
        {
            get { return SearchBox.Visibility; }
            set { SearchBox.Visibility = value;}
        }

        private Visibility _MultiSelectVisibility;
        
        public Visibility MultiSelectVisibility {
            get { return _MultiSelectVisibility; }
            set { _MultiSelectVisibility = value; }
        }

        public EntityComboBox()
        {
            InitializeComponent();
        }

        public ObservableCollection<BaseEntity> SelectedValues;

        private void SelectionHandler(object sender, MouseButtonEventArgs e)
        {
            var item = ((sender as ListViewItem).DataContext as BaseEntityWrapper);
            
            if (item != null)
            {
                if (MultiSelectVisibility == Visibility.Visible)
                {
                    if (item.IsSelected.Equals(true))
                    {
                        //take the selected item out of the selected items list
                        EntityList<BaseEntity> selectedEntities = ((FieldViewModel)DataContext).GetSelectedEntities();
                        
                        BaseEntity entityToRemove = null;
                        foreach (BaseEntity baseEntity in selectedEntities.data)
                        {
                            if(item.Equals(new BaseEntityWrapper(baseEntity)))
                            {
                                entityToRemove = baseEntity;
                                break;
                            }
                        }
                        selectedEntities.data.Remove(entityToRemove);
                        ((FieldViewModel)EditorLabelName.DataContext).Content = selectedEntities;

                        item.IsSelected = false;
                    }
                    else
                    {
                        //insert the selected value into the selected items list
                        EntityList<BaseEntity> selectedEntities = ((FieldViewModel)DataContext).GetSelectedEntities();
                        
                        selectedEntities.data.Add(item.BaseEntity);
                        ((FieldViewModel)EditorLabelName.DataContext).Content = selectedEntities;

                        item.IsSelected = true;
                    }

                    // Set the focus back to the list view item, if the combo box was clicked directly the popup would no longer close, because the focus would go crazy
                    FocusManager.SetFocusedElement(this, (sender as ListViewItem));
                } else
                {
                    ComboBoxPopup.IsOpen = false;
                    ((FieldViewModel)EditorLabelName.DataContext).Content = item.BaseEntity;
                }
                listView.Items.Refresh();
            }
            
        }
        
        private async void ShowPopup(object sender, RoutedEventArgs e)
        {
            var items = await ((FieldViewModel)EditorLabelName.DataContext).taskRetrieveData();

            listView.Items.Refresh();
            ComboBoxPopup.IsOpen = true;
        }
    }
}
