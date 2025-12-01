using Core.Domain.Entities.ContpaqiSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasculaInterface.ViewModels.Controls
{
    public class ListControlViewModel<T>
    {
        public ObservableCollection<T> BindingList { get; set; }

        public ListControlViewModel(ObservableCollection<T> bindingList)
        {
            BindingList = bindingList;
        }
    }
}
