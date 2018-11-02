using Qt.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace sample.ViewModel
{
    abstract class ViewModelBase : INotifyPropertyChanged
    {
        [RaisePropertyChanged]
        public string Title { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
