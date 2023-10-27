using System;
using System.Windows;
using Microsoft.Toolkit.Wpf.UI.Controls;

namespace RazorCX.FindMissingDrawings.ViewModels
{
    public class WebBrowserUtility
    {
        public static readonly DependencyProperty BodyProperty =
            DependencyProperty.RegisterAttached("Body", typeof(string), typeof(WebBrowserUtility), new PropertyMetadata(OnBodyChanged));

        public static string GetBody(DependencyObject dependencyObject)
        {
            return (string)dependencyObject.GetValue(BodyProperty);
        }

        public static void SetBody(DependencyObject dependencyObject, string body)
        {
            dependencyObject.SetValue(BodyProperty, body);
        }

        private static void OnBodyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                var webBrowser = (WebView)d;
                string uri = e.NewValue as string;
                webBrowser.Navigate(new Uri(uri, UriKind.Absolute));
            }
            catch (Exception ex)
            {

            }
        }
    }
}