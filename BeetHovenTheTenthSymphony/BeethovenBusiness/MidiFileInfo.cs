using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeethovenBusiness
{
    public class MidiFileInfo : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Difficulty { get; set; }
        public bool _favourite { get; set; }
        public bool Favourite
        {
            get => _favourite;
            set
            {
                if (_favourite != value)
                {
                    _favourite = value;
                    OnPropertyChanged(nameof(Favourite));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
