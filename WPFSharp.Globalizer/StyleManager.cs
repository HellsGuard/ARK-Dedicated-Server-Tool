// See license at bottom of file
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Markup;

namespace WPFSharp.Globalizer
{
    public sealed class StyleManager : ResourceDictionaryManagerBase
    {
        #region Members

        public static String DefaultStyle = "Default Style.xaml";

        #endregion

        #region Contructor

        public StyleManager(Collection<ResourceDictionary> inMergedDictionaries)
            : base(inMergedDictionaries)
        {
            SubDirectory = "Styles";
        }

        #endregion

        #region Functions
        /// <summary>
        /// Dynamically load a Localization ResourceDictionary from a file
        /// </summary>
        public void SwitchStyle(String inFileName, string inResourceDictionaryName = null)
        {
            string path = inFileName;
            if (!File.Exists(path) && !path.Contains(@":\"))
                path = Path.Combine(GlobalizedApplication.Instance.Directory, SubDirectory, inFileName);
            if (File.Exists(path))
            {
                RemoveResourceDictionaries();
                MergedDictionaries.Add(LoadFromFile(path) as StyleResourceDictionary);
                var args = new ResourceDictionaryChangedEventArgs();
                args.ResourceDictionaryNames.Add(Path.GetFileNameWithoutExtension(inFileName));
                args.ResourceDictionaryPaths.Add(path);

                NotifyResourceDictionaryChanged(args);
            }
            else
            {
                Debug.WriteLine("ResourceDictionary not found: " + inFileName);
            }
        }

        public override EnhancedResourceDictionary LoadFromFile(string inFile)
        {
            string file = inFile;
            // Determine if the path is absolute or relative
            if (!Path.IsPathRooted(inFile))
            {
                string exedir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (exedir != null)
                    file = Path.Combine(exedir, inFile);
            }

            if (!File.Exists(file))
                return null;

            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // Read in ResourceDictionary File or preferably an EnhancedResourceDictionary file
                var styleResourceDictionary = XamlReader.Load(fs) as StyleResourceDictionary;

                if (styleResourceDictionary == null)
                    return null;

                styleResourceDictionary.Source = inFile;
                return styleResourceDictionary;
            }
        }

        private void RemoveResourceDictionaries()
        {
            var dictionariesToRemove = new List<ResourceDictionary>();
            foreach (ResourceDictionary srd in GlobalizedApplication.Instance.Resources.MergedDictionaries)
            {
                // Make sure to only remove Styles, but don't remove styles owned by the language
                if (srd is StyleResourceDictionary && !(srd as StyleResourceDictionary).IsLinkedToLanguage)
                    dictionariesToRemove.Add(srd);
            }
            foreach (var rd in dictionariesToRemove)
            {
                GlobalizedApplication.Instance.Resources.MergedDictionaries.Remove(rd);
            }
        }
        #endregion
    }
}

#region License
/*
WPFSharp.Globalizer - A project deisgned to make localization and styling
                      easier by decoupling both process from the build.

Copyright (c) 2015, Jared Barneck (Rhyous)
All rights reserved.
 
Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
 
1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.
3. Use of the source code or binaries that in any way competes with WPFSharp.Globalizer
   or competes with distribution, whether open source or commercial, is 
   prohibited unless permission is specifically granted under a separate
   license by Jared Barneck (Rhyous).
4. Forking for personal or internal, or non-competing commercial use is allowed.
   Distributing compiled releases as part of your non-competing project is 
   allowed.
5. Public copies, or forks, of source is allowed, but from such, public
   distribution of compiled releases is forbidden.
6. Source code enhancements or additions are the property of the author until
   the source code is contributed to this project. By contributing the source
   code to this project, the author immediately grants all rights to the
   contributed source code to Jared Barneck (Rhyous).
 
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
#endregion
