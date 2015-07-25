using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace ARK_Server_Manager.Lib
{
    public class EngramList : SortableObservableCollection<Engram>
    {
        public void AddRange(IEnumerable<Engram> spawns)
        {
            foreach (var spawn in spawns)
            {
                base.Add(spawn);
            }
        }

        public static EngramList FromINIValues(IEnumerable<string> iniValues)
        {
            var spawns = new EngramList();
            //spawns.AddRange(iniValues.Select(v => Engram.FromINIValue(v)));
            return spawns;
        }

        public List<string> ToINIValues()
        {
            var values = new List<string>();
            //values.AddRange(this.Select(d => String.Format("OverrideNamedEngramEntries={0}", d.ToINIValue())));
            return values;
        }
    }
    public class Engram : DependencyObject
    {

        public static readonly DependencyProperty RemovePrereqProperty =
            DependencyProperty.Register("RemovePrereq", typeof(bool), typeof(Engram), new PropertyMetadata(false));
        public static readonly DependencyProperty LevelRequirementProperty =
            DependencyProperty.Register("LevelRequirement", typeof(int), typeof(Engram), new PropertyMetadata(0));
        public static readonly DependencyProperty PointsCostProperty =
            DependencyProperty.Register("PointsCost", typeof(int), typeof(Engram), new PropertyMetadata(0));
        public static readonly DependencyProperty HiddenProperty =
            DependencyProperty.Register("Hidden", typeof(bool), typeof(Engram), new PropertyMetadata(false));
        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register("Name", typeof(string), typeof(Engram), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty ClassNameProperty =
            DependencyProperty.Register("ClassName", typeof(string), typeof(Engram), new PropertyMetadata(String.Empty));
        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }
        public string ClassName
        {
            get { return (string)GetValue(ClassNameProperty); }
            set { SetValue(ClassNameProperty, value); }
        }

        public bool Hidden
        {
            get { return (bool)GetValue(HiddenProperty); }
            set { SetValue(HiddenProperty, value); }
        }

        public int PointsCost
        {
            get { return (int)GetValue(PointsCostProperty); }
            set { SetValue(PointsCostProperty, value); }
        }
        public int LevelRequirement
        {
            get { return (int)GetValue(LevelRequirementProperty); }
            set { SetValue(LevelRequirementProperty, value); }
        }

        public bool RemovePrereq
        {
            get { return (bool)GetValue(RemovePrereqProperty); }
            set { SetValue(RemovePrereqProperty, value); }
        }
    }
}
