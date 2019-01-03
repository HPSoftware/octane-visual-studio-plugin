﻿using MicroFocus.Adm.Octane.Api.Core.Connector.Authentication;
using System;
using System.Diagnostics;

namespace MicroFocus.Adm.Octane.VisualStudio
{
    class SsoConnectionListener : ConnectionListener
    {
        public void OpenBrowser(string url)
        {

			// Currently won't work because the polling is blocking the main thread, need to disucss futher
			// BrowserDialog browserDialog = new BrowserDialog(url);
			// browserDialog.Show();

			// Open the URL in the user's default browser.
			Process.Start(url);
        }
    }
}
