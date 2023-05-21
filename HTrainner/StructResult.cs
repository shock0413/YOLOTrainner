using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HsrAITrainner
{
    public class StructResult : INotifyPropertyChanged
    {
        public StructResult(string name)
        {
            m_LabelName = name;
            labledCount = 0;
            findedCount = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    
        public string LabelName { get { return m_LabelName; } set { m_LabelName = value;NotifyPropertyChanged("LabelName"); } }
        private string m_LabelName;
        public int LabledCount { get { return labledCount; } set { labledCount = value; NotifyPropertyChanged("LabledCount"); } }
        private int labledCount;
        public int FindedCount { get { return findedCount; } set { findedCount = value; NotifyPropertyChanged("FindedCount"); } }
        private int findedCount;
    }
}
