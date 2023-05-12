using System.ComponentModel;
using System.Runtime.CompilerServices;
using WagemasterEvents.Models;

namespace WagemasterEvents
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private Event _selectedEvent;

        public Event SelectedEvent
        {
            get { return _selectedEvent; }
            set
            {
                _selectedEvent = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

