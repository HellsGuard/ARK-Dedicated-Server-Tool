﻿using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

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

        /// <summary>
        /// Finds a parent of a given item on the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="child">A direct or indirect child of the  queried item.</param>
        /// <returns>The first parent item that matches the submitted type parameter. If not matching item can be found, a null reference is being returned.</returns>
        public static T TryFindParent<T>(DependencyObject child)
          where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = GetParentObject(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
                return parent;

            //use recursion to proceed with next level
            return TryFindParent<T>(parentObject);
        }

        /// <summary>
        /// This method is an alternative to WPF's <see cref="VisualTreeHelper.GetParent"/> method, which also supports content elements. 
        /// Do note, that for content element, this method falls back to the logical tree of the element.
        /// </summary>
        /// <param name="child">The item to be processed.</param>
        /// <returns>The submitted item's parent, if available. Otherwise  null.</returns>
        public static DependencyObject GetParentObject(DependencyObject child)
        {
            if (child == null) return null;
            var contentElement = child as ContentElement;

            if (contentElement != null)
            {
                var parent = ContentOperations.GetParent(contentElement);
                if (parent != null) return parent;

                var fce = contentElement as FrameworkContentElement;
                return fce?.Parent;
            }

            //if it's not a ContentElement, rely on VisualTreeHelper
            return VisualTreeHelper.GetParent(child);
        }

        /// <summary>
        /// Recursively processes a given dependency object and all its children, and updates sources of all objects that use a binding expression on a given property.
        /// </summary>
        /// <param name="obj">The dependency object that marks a starting point. This could be a dialog window or a panel control that hosts bound controls.</param>
        /// <param name="properties">The properties to be updated if 
        /// <paramref name="obj"/> or one of its childs provide it along with a binding expression.</param>
        public static void UpdateBindingSources(DependencyObject obj, params DependencyProperty[] properties)
        {
            foreach (var depProperty in properties)
            {
                //check whether the submitted object provides a bound property that matches the property parameters
                var be = BindingOperations.GetBindingExpression(obj, depProperty);
                be?.UpdateSource();
            }

            int count = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < count; i++)
            {
                //process child items recursively
                var childObject = VisualTreeHelper.GetChild(obj, i);
                UpdateBindingSources(childObject, properties);
            }
        }

        /// <summary>
        /// Tries to locate a given item within the visual tree, starting with the dependency object at a given position. 
        /// </summary>
        /// <typeparam name="T">The type of the element to be found  on the visual tree of the element at the given location.</typeparam>
        /// <param name="reference">The main element which is used to perform  hit testing.</param>
        /// <param name="point">The position to be evaluated on the origin.</param>
        public static T TryFindFromPoint<T>(UIElement reference, Point point)
          where T : DependencyObject
        {
            var element = reference.InputHitTest(point) as DependencyObject;
            if (element == null) return null;
            if (element is T) return (T)element;
            return TryFindParent<T>(element);
        }
    }
}
