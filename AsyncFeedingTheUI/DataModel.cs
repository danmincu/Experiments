using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Navigation;
using System.Windows.Threading;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;

namespace AsyncFeedingTheUI
{
    public class DataModel : BindableBase
    {
        //lock object for synchronization;
        private static object _syncLock = new object();
        public DataModel()
        {
            ItemCollection = new ObservableCollection<Country>();
            //Enable the cross acces to this collection elsewhere
            BindingOperations.EnableCollectionSynchronization(ItemCollection, _syncLock);

            FetchRecordsCommand = new DelegateCommand(async () => await ExecuteFetchAsync(), CanExecuteFetch);
            //FetchRecordsCommand = new DelegateCommand(ExecuteFetchAsync1,CanExecuteFetch);
            FetchFooCommand = new DelegateCommand(async () => await ExecuteFetchFooAsync(), CanExecuteFetch);

            // this is the tricky part where you need to initialize the collection in here
            // view here otherwise you get an error about accessing from a diferent thread of the collection
            if (this.CollectionView == null)
            {
                this.CollectionView = new ListCollectionView(this.ItemCollection);
            }
        }

        //this is for the old way of syncroninzing the async collections
        private async Task ExecuteFetchAsyncOld()
        {
            Action action = async () =>
            {
                var countries = await this.GetListOfCountries().ConfigureAwait(false);
                foreach (var country in countries)
                {
                    lock (_syncLock)
                    {
                        ItemCollection.Add(country);
                    }
                }
            };
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(action));
        }

        //this is the correct usage of a list item view being fed from an item collection
        // https://stackoverflow.com/questions/14336750/upgrading-to-net-4-5-an-itemscontrol-is-inconsistent-with-its-items-source
        private async Task ExecuteFetchAsync()
        {
            ItemCollection.Clear();
            var countries = await this.GetListOfCountries().ConfigureAwait(false);
            foreach (var country in countries)
            {
                lock (_syncLock)
                {
                    ItemCollection.Add(country);
                }
            }
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,new Action( async () =>
            {
                this.collectionView?.MoveCurrentToFirst();
                await Task.Delay(100).ConfigureAwait(false);
            }));
        }

        private async Task ExecuteFetchFooAsync()
        {
            var foo = await Task<Foo>.Run(() => new Foo { Name = DateTime.Now.Ticks.ToString() }).ConfigureAwait(false);

            foreach (var countryItem in this.ItemCollection)
            {
                
                lock (_syncLock)
                {
                    countryItem.Foo = foo;
                }
            }
        }


        // this is an odd usage.. not returning task being problem #1 and using the delegate in a non-async maner being problem #2
        private async void ExecuteFetchAsync1()
        {
            ItemCollection.Clear();
            var countries = await this.GetListOfCountries().ConfigureAwait(false);
            foreach (var country in countries)
            {
                lock (_syncLock)
                {
                    ItemCollection.Add(country);
                }
            }
        }

        private async Task ExecuteFetchAsyncAsync()
        {
            await Task.Run(async () =>
            {
                await Task.Delay(100).ConfigureAwait(false);
                ItemCollection.Clear();

                var countries = await this.GetListOfCountries().ConfigureAwait(false);
                foreach (var country in countries)
                {
                    lock (_syncLock)
                    {
                        ItemCollection.Add(country);
                    }
                }
            }).ConfigureAwait(false);
        }

        private bool CanExecuteFetch()
        {
            return true;
        }

        public async Task<List<Country>> GetListOfCountries()
        {
            var result = new List<Country>();

            // read the countries from an online source
            var url =
                "https://pkgstore.datahub.io/core/country-list/data_json/data/8c458f2d15d9f2119654b29ede6e45b8/data_json.json";
            using (var client = new HttpClient())
            using (var response = await client.GetAsync(url))
            using (var content = response.Content)
            {
                var data = await content.ReadAsStringAsync();
                var countries = JsonConvert.DeserializeObject<Country[]>(data);

                foreach (var country in countries)
                {
                    result.Add(country);
                }
            }

            //var data = await Task<string>.Run(() => System.IO.File.ReadAllText(@"C:\temp\countries.txt")).ConfigureAwait(false);
            //var countries = JsonConvert.DeserializeObject<Country[]>(data);

            //foreach (var country in countries)
            //{
            //    result.Add(country);
            //}
            return result;
        }

        private DelegateCommand fetchRecordsCommand;

        public DelegateCommand FetchRecordsCommand
        {
            get => fetchRecordsCommand;
            set => SetProperty(ref fetchRecordsCommand, (DelegateCommand)value);
        }

        private DelegateCommand fetchFooCommand;

        public DelegateCommand FetchFooCommand
        {
            get => fetchFooCommand;
            set => SetProperty(ref fetchFooCommand, (DelegateCommand)value);
        }

        protected ListCollectionView collectionView;

        public ListCollectionView CollectionView
        {
            get => collectionView;
            set => SetProperty(ref collectionView, value);
        }

        public ObservableCollection<Country> ItemCollection { get; set; }

    }
}
