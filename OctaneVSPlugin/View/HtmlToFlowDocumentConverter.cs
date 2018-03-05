﻿using HTMLConverter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace MicroFocus.Adm.Octane.VisualStudio.View
{
    public class HtmlToFlowDocumentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            var xaml = HtmlToXamlConverter.ConvertHtmlToXaml(value.ToString(), false);

            var flowDocument = new FlowDocument();
            using (var stream = new MemoryStream((new ASCIIEncoding()).GetBytes(xaml)))
            {
                var text = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
                text.Load(stream, DataFormats.Xaml);
            }

            SubscribeToAllHyperlinks(flowDocument);

            return flowDocument;
        }

        private void SubscribeToAllHyperlinks(FlowDocument flowDocument)
        {
            var hyperlinks = GetVisuals(flowDocument).OfType<Hyperlink>();
            foreach (var link in hyperlinks)
                link.RequestNavigate += LinkRequestNavigate;
        }

        private static IEnumerable<DependencyObject> GetVisuals(DependencyObject root)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            {
                yield return child;
                foreach (var descendants in GetVisuals(child))
                    yield return descendants;
            }
        }

        private void LinkRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var hyperLink = (Hyperlink)e.OriginalSource;
            var uri = e.Uri.AbsoluteUri + "#" + hyperLink.TargetName;
            Process.Start(new ProcessStartInfo(uri));
            e.Handled = true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
