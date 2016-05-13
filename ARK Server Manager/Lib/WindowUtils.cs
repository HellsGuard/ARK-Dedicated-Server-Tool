using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ARK_Server_Manager.Lib
{
    public static class WindowUtils
    {
        private static string DEFAULT_RESOURCE_DICTIONARY = @"Globalization\en-US\en-US.xaml";

        public static void RemoveDefaultResourceDictionary(Window window)
        {
            if (window == null)
                return;

            var dictToRemove = window.Resources.MergedDictionaries.FirstOrDefault(d => d.Source.OriginalString.Contains(DEFAULT_RESOURCE_DICTIONARY));
            if (dictToRemove != null)
            {
                window.Resources.MergedDictionaries.Remove(dictToRemove);
            }
        }

        public static void RemoveDefaultResourceDictionary(UserControl control)
        {
            if (control == null)
                return;

            var dictToRemove = control.Resources.MergedDictionaries.FirstOrDefault(d => d.Source.OriginalString.Contains(DEFAULT_RESOURCE_DICTIONARY));
            if (dictToRemove != null)
            {
                control.Resources.MergedDictionaries.Remove(dictToRemove);
            }
        }
    }
}
