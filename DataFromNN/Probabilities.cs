using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Paint.DataFromNN
{
    public class Probabilities
    {
        public ObservableCollection<Probability> Values { get; private set; }

        public Probabilities()
        {
            Values = new ObservableCollection<Probability>();
            for (int i = 0; i < 10; i++)
                Values.Add(new Probability(i));
        }

        public void ChangeValues(IList<double> values)
        {
            if (values.Count != 10)
                throw new ArgumentException("Invalid number of values in the list!");

            for (int i = 0; i < 10; i++)
            {
                Values[i].Width = (int)(values[i] * 300);
                Values[i].Value = values[i];
            }
        }
    }

    public class Probability : INotifyPropertyChanged
    {
        int width = 30;
        double prob = 0.1d;

        public Probability(int index)
            => this.Index = index;

        public int Index { get; }

        public double Value
        {
            get => prob;
            set
            {
                if (prob != value)
                {
                    prob = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public int Width
        {
            get => width;
            set
            {
                if (width != value)
                {
                    width = value;
                    NotifyPropertyChanged();
                }
            }
        }


        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
