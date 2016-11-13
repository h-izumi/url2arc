using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;


namespace Url2Arc.SampleApp.ViewModel
{
    public class MainWindow : INotifyPropertyChanged
    {
        private String urlToAdd = String.Empty;
        private String pathToSave = String.Empty;

        public String UrlToAdd
        {
            get
            {
                return urlToAdd;
            }
            set
            {
                urlToAdd = value;
                NotifyPropertyChanged();
            }
        }

        public String PathToSave
        {
            get
            {
                return pathToSave;
            }

            set
            {
                pathToSave = value;
                NotifyPropertyChanged();
            }
        }

        public ObservableCollection<String> Urls { get; private set; } = new ObservableCollection<String>();

        public String LastResult = String.Empty;

        public void AddUrlToUrls()
        {
            if (UrlToAdd != String.Empty)
            {
                Urls.Add(UrlToAdd);
                UrlToAdd = String.Empty;
            }
        }

        public void RemoveUrl(int index)
        {
            if (index >= 0)
            {
                Urls.RemoveAt(index);
            }
        }

        public void ClearUrls()
        {
            Urls.Clear();
        }

        public async Task Run()
        {
            if (Urls.Count < 1)
            {
                LastResult = Properties.Resources.MessageSourceUrlsEmpty;
                return;
            }
            if (PathToSave == String.Empty)
            {
                LastResult = Properties.Resources.MessagePathToSaveEmpty;
                return;
            }

            var resultBuilder = new StringBuilder();

            using (var archiver = new Url2Arc.Archiver())
            {
                archiver.AddItems(Urls);

                using (var file = new FileStream(PathToSave, FileMode.Create))
                {
                    await archiver.CreateArchiveAsync(file);

                    resultBuilder.AppendLine($"{Properties.Resources.MessageSucceeded}:");
                    foreach (var item in archiver.SucceededSourceItems)
                    {
                        resultBuilder.AppendLine($"\t{item.Url}");
                    }
                    if (archiver.FailedSourceItems.Count > 0)
                    {
                        resultBuilder.AppendLine($"{Properties.Resources.MessageFailed}:");
                        foreach (var item in archiver.FailedSourceItems)
                        {
                            resultBuilder.AppendLine($"\t{item.Url} - {item.LastException.Message}");
                        }
                    }
                }
            }

            LastResult = resultBuilder.ToString();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
