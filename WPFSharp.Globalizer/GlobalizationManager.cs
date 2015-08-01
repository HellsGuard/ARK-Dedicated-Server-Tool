// See license at bottom of file
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace WPFSharp.Globalizer
{
    public sealed class GlobalizationManager : ResourceDictionaryManagerBase
    {
        #region Members

        private const string FallBackLanguage = "en-US";

        #endregion

        #region Contructor
        public GlobalizationManager(Collection<ResourceDictionary> inMergedDictionaries)
            : base(inMergedDictionaries)
        {
            SubDirectory = "Globalization";
        }

        #endregion

        #region Functions

        /// <summary>
        /// Dynamically load a Localization ResourceDictionary from a file
        /// </summary>
        public void SwitchLanguage(string inFiveCharLang, bool inForceSwitch = false)
        {
            if (CultureInfo.CurrentCulture.Name.Equals(inFiveCharLang) && !inForceSwitch)
                return;

            if(!AvailableLanguages.Instance.Contains(inFiveCharLang))
            {
                throw new CultureNotFoundException(String.Format("The language {0} is not available.", inFiveCharLang));
            }

            // Set the new language
            var ci = new CultureInfo(inFiveCharLang);
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
            
            string[] xamlFiles = Directory.GetFiles(Path.Combine(DefaultPath, inFiveCharLang), "*.xaml");

            // If there are no files, do nothing
            if (xamlFiles.Length == 0)
                return;

            FileNames = new List<string>(xamlFiles);

            // Remove previous ResourceDictionaries
            RemoveGlobalizationResourceDictionaries();

            // Add new Resource Dictionaries
            LoadDictionariesFromFiles(FileNames);
            var args = new ResourceDictionaryChangedEventArgs { ResourceDictionaryPaths = FileNames };
            NotifyResourceDictionaryChanged(args);
        }

        private void RemoveGlobalizationResourceDictionaries()
        {
            var dictionariesToRemove = new List<ResourceDictionary>();
            foreach (ResourceDictionary erd in GlobalizedApplication.Instance.Resources.MergedDictionaries)
            {
                if (erd is GlobalizationResourceDictionary)
                {
                    dictionariesToRemove.Add(erd);
                }
            }
            foreach (EnhancedResourceDictionary erd in dictionariesToRemove)
            {
                GlobalizedApplication.Instance.Resources.MergedDictionaries.Remove(erd);
                // Also remove any associated LinkedStyles
                var globalizationResourceDictionary = erd as GlobalizationResourceDictionary;
                if (globalizationResourceDictionary != null && globalizationResourceDictionary.LinkedStyle != null)
                    Remove((erd as GlobalizationResourceDictionary).LinkedStyle);
            }
        }

        public override EnhancedResourceDictionary LoadFromFile(String inFile)
        {
            return LoadFromFile(inFile, true);
        }

        public EnhancedResourceDictionary LoadFromFile(String inFile, bool inRequireGlobalizationType = true)
        {
            string file = inFile;
            // Determine if the path is absolute or relative
            if (!Path.IsPathRooted(inFile))
            {
                file = Path.Combine(DefaultPath, CultureInfo.CurrentCulture.Name, inFile);
            }

            if (!File.Exists(file))
                return null;

            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // Read in an EnhancedResourceDictionary File or preferably an GlobalizationResourceDictionary file
                var erd = XamlReader.Load(fs) as EnhancedResourceDictionary;

                if (erd != null)
                {
                    if (inRequireGlobalizationType)
                    {
                        if (erd is GlobalizationResourceDictionary)
                            return erd;

                        return null;
                    }
                }
                return erd;
            }
        }

        public override void LoadDictionariesFromFiles(List<string> inList)
        {
            foreach (var filePath in inList)
            {
                // Only Globalization resource dictionaries should be added
                // Ignore other types
                var globalizationResourceDictionary = LoadFromFile(filePath) as GlobalizationResourceDictionary;
                if (globalizationResourceDictionary == null)
                    continue;

                MergedDictionaries.Add(globalizationResourceDictionary);

                if (globalizationResourceDictionary.LinkedStyle == null)
                    continue;

                var styleFile = globalizationResourceDictionary.LinkedStyle + ".xaml";
                if (globalizationResourceDictionary.Source != null)
                {
                    var path = Path.Combine(Path.GetDirectoryName(globalizationResourceDictionary.Source), styleFile);
                    // Todo: Check for file and if not there, look in the Styles dir

                    var tempStyleDict = LoadFromFile(path, false);
                    if (tempStyleDict != null)
                    {
                        MergedDictionaries.Add(tempStyleDict);
                    }
                    return;
                }

                var styleDict = LoadFromFile(styleFile, false);
                if (styleDict != null)
                {
                    MergedDictionaries.Add(styleDict);
                }
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

