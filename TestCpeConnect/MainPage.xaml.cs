using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace TestCpeConnect
{
    public partial class MainPage : ContentPage
    {
        ApiService networking = new ApiService();

        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }


        async Task InitData() {
            logLabel.Text = await networking.UploadRegFile();
        }

        void UploadRegFile() {
            Debug.WriteLine("UploadRegFile");
            Debug.WriteLine("x-"+ DependencyService.Get<IApi>());

            //DependencyService.Get<IApi>()?.UploadRegFile();

            (new ApiService()).UploadRegFile();

        }

        void Button_Clicked(System.Object sender, System.EventArgs e)
        {
            UploadRegFile();
        }
    }
}
