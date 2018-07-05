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

using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MicroFocus.Adm.Octane.VisualStudio.Common
{
    /// <summary>
    /// Implement ICommandHandler which delegate the Execute to a delegate.
    /// </summary>
    public class DelegatedCommand : ICommand
    {
        private readonly Action<object> handler;
        private bool canExecute;

        public DelegatedCommand(Action<object> handler)
        {
            this.handler = handler;
            canExecute = true;
        }
        public DelegatedCommand(Action<object> handler, bool val)
        {
            this.handler = handler;
            canExecute = val;
        }

        public event EventHandler CanExecuteChanged;

        public void SetCanExecute(bool value)
        {
            if (canExecute == value)
            {
                return;
            }

            canExecute = value;

            if (CanExecuteChanged != null)
            {
                CanExecuteChanged.Invoke(this, EventArgs.Empty);
            }
        }

        public bool CanExecute(object parameter)
        {
            return canExecute;
        }

        public void Execute(object parameter)
        {
            handler(parameter);
        }
    }
}
