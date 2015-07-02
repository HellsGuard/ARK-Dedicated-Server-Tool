using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Windows;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace ARK_Server_Manager.Lib
{
    public class Level : DependencyObject
    {
        public static readonly Regex XPRegex = new Regex(@"ExperiencePointsForLevel\[(?<level>\d*)]=(?<xp>\d*)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
        public static readonly Regex EngramRegex = new Regex(@"OverridePlayerLevelEngramPoints=(?<points>\d*)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        public static readonly DependencyProperty EngramPointsProperty =
            DependencyProperty.Register("EngramPoints", typeof(int), typeof(Level), new PropertyMetadata(0));
        public static readonly DependencyProperty XPRequiredProperty =
            DependencyProperty.Register("XPRequired", typeof(int), typeof(Level), new PropertyMetadata(0));
        public static readonly DependencyProperty LevelIndexProperty =
            DependencyProperty.Register("LevelIndex", typeof(int), typeof(Level), new PropertyMetadata(0));


        public int LevelIndex
        {
            get { return (int)GetValue(LevelIndexProperty); }
            set { SetValue(LevelIndexProperty, value); }
        }

        public int XPRequired
        {
            get { return (int)GetValue(XPRequiredProperty); }
            set { SetValue(XPRequiredProperty, value); }
        }

        public int EngramPoints
        {
            get { return (int)GetValue(EngramPointsProperty); }
            set { SetValue(EngramPointsProperty, value); }
        }
        
        public static string ToINIValueForXP(IEnumerable<Level> levels)
        {
            var builder = new StringBuilder();
            builder.Append("LevelExperienceRampOverrides=(");
            builder.Append(String.Join(",", levels.OrderBy(l => l.LevelIndex).Select(l => l.GetINISubValueForXP())));
            builder.Append(')');

            return builder.ToString();
        }

        public static List<string> ToINIValuesForEngramPoints(IEnumerable<Level> levels)
        {
            var entries = new List<string>();
            foreach (var level in levels.OrderBy(l => l.LevelIndex))
            {
                entries.Add(level.GetINIValueForEngramPointsEarned());
            }

            return entries;
        }

        public static List<Level> FromINIValues(string xpValue, IEnumerable<string> engramValues = null)
        {
            var levels = new List<Level>();
            var xpResult = XPRegex.Match(xpValue);
            var engramResult = engramValues == null ? null : EngramRegex.Match(String.Join(" ", engramValues));

            while (xpResult.Success && (engramValues == null || engramResult.Success))
            {
                int levelIndex;
                if (!int.TryParse(xpResult.Groups["level"].Value, out levelIndex))
                {
                    Debug.WriteLine(String.Format("Invalid level index value: '{0}'", xpResult.Groups["level"].Value));
                    break;
                }

                int xpRequired;
                if (!int.TryParse(xpResult.Groups["xp"].Value, out xpRequired))
                {
                    Debug.WriteLine(String.Format("Invalid xm required value: '{0}'", xpResult.Groups["xp"].Value));
                    break;
                }

                int engramPoints = 0;
                if(engramResult != null)
                {
                    if(!int.TryParse(engramResult.Groups["points"].Value, out engramPoints))
                    {
                        Debug.WriteLine(String.Format("Invalid engram points value: '{0}'", engramResult.Groups["points"].Value));
                        break;
                    }
                }

                levels.Add(new Level { LevelIndex = levelIndex, XPRequired = xpRequired, EngramPoints = engramPoints });
                xpResult = xpResult.NextMatch();
                if (engramResult != null)
                {
                    engramResult = engramResult.NextMatch();
                }
            }

            return levels;
        }

        private string GetINISubValueForXP()
        {
            return String.Format("ExperiencePointsForLevel[{0}]={1}", this.LevelIndex, this.XPRequired);
        }

        private string GetINIValueForEngramPointsEarned()
        {
            return String.Format("OverridePlayerLevelEngramPoints={0}", this.EngramPoints);
        }
    }
}