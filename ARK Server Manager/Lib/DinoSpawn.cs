using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace ARK_Server_Manager.Lib
{
    public class DinoSpawnList : SortableObservableCollection<DinoSpawn>
    {

        public void InsertSpawns(IEnumerable<DinoSpawn> spawns)
        {
            foreach(var spawn in spawns)
            {
                base.Add(spawn);
            }
        }

        public static DinoSpawnList FromINIValues(IEnumerable<string> iniValues)
        {
            var spawns = new DinoSpawnList();
            spawns.InsertSpawns(iniValues.Select(v => DinoSpawn.FromINIValue(v)));
            return spawns;
        }

        public List<string> ToINIValues()
        {
            var values = new List<string>();
            values.AddRange(this.Select(d => String.Format("DinoSpawnWeightMultipliers={0}", d.ToINIValue())));
            return values;
        }
    }

    public class DinoSpawn : DependencyObject
    {
        public static readonly Regex DinoNameRegex = new Regex(@"DinoNameTag=\s*(?<dinoname>\w*)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
        public static readonly Regex SpawnWeightRegex = new Regex(@"SpawnWeightMultiplier=\s*(?<weight>\d*(\.\d*)?)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
        public static readonly Regex OverrideSpawnLimitRegex = new Regex(@"OverrideSpawnLimitPercentage=\s*(?<flag>\w*)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
        public static readonly Regex SpawnLimitPercentageRegex = new Regex(@"SpawnLimitPercentage=\s*(?<limit>\d*(\.\d*)?)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        public static readonly DependencyProperty SpawnLimitPercentageProperty = DependencyProperty.Register("SpawnLimitPercentage", typeof(float), typeof(DinoSpawn), new PropertyMetadata(10.0F));
        public static readonly DependencyProperty NameProperty = DependencyProperty.Register("Name", typeof(string), typeof(DinoSpawn), new PropertyMetadata("--SET ME--"));
        public static readonly DependencyProperty SpawnWeightMultiplierProperty = DependencyProperty.Register("SpawnWeightMultiplier", typeof(float), typeof(DinoSpawn), new PropertyMetadata(0.0F));
        public static readonly DependencyProperty OverrideSpawnLimitPercentageProperty = DependencyProperty.Register("OverrideSpawnLimitPercentage", typeof(bool), typeof(DinoSpawn), new PropertyMetadata(false));

        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        public float SpawnWeightMultiplier
        {
            get { return (float)GetValue(SpawnWeightMultiplierProperty); }
            set { SetValue(SpawnWeightMultiplierProperty, value); }
        }

        public bool OverrideSpawnLimitPercentage
        {
            get { return (bool)GetValue(OverrideSpawnLimitPercentageProperty); }
            set { SetValue(OverrideSpawnLimitPercentageProperty, value); }
        }

        public float SpawnLimitPercentage
        {
            get { return (float)GetValue(SpawnLimitPercentageProperty); }
            set { SetValue(SpawnLimitPercentageProperty, value); }  
        }

        public string ToINIValue()
        {
            var entry = new StringBuilder();
            entry.AppendFormat("(DinoNameTag={0},SpawnWeightMultiplier={1},OverrideSpawnLimitPercentage={2}", this.Name, this.SpawnWeightMultiplier, this.OverrideSpawnLimitPercentage);
            if(this.OverrideSpawnLimitPercentage)
            {
                entry.AppendFormat(",SpawnLimitPercentage={0}", this.SpawnLimitPercentage);
            }
            entry.Append(')');
            return entry.ToString();
        }

        public static DinoSpawn FromINIValue(string iniValue)
        {
            var newSpawn = new DinoSpawn();

            var match = DinoNameRegex.Match(iniValue);

            if(!match.Success)
            {
                return null;
            }
            
            newSpawn.Name = match.Groups["dinoname"].Value;

            match = SpawnWeightRegex.Match(iniValue);
            if (!match.Success)
            {
                return null;
            }

            float floatVal;
            if(!float.TryParse(match.Groups["weight"].Value, out floatVal))
            {
                return null;
            }

            newSpawn.SpawnWeightMultiplier = floatVal;

            match = OverrideSpawnLimitRegex.Match(iniValue);
            if(!match.Success)
            {
                return null;
            }

            bool boolVal;
            if(!bool.TryParse(match.Groups["flag"].Value, out boolVal))
            {
                return null;
            }

            newSpawn.OverrideSpawnLimitPercentage = boolVal;

            if(newSpawn.OverrideSpawnLimitPercentage)
            {
                match = SpawnLimitPercentageRegex.Match(iniValue);
                if(match.Success && float.TryParse(match.Groups["limit"].Value, out floatVal))
                {
                    newSpawn.SpawnLimitPercentage = floatVal;
                }
            }

            return newSpawn;
        }
    }
}
