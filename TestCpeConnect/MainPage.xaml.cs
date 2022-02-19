using System;
using System.Collections.Generic;
using System.ComponentModel;
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
           DependencyService.Get<IApi>()?.UploadRegFile();

        }

        void Button_Clicked(System.Object sender, System.EventArgs e)
        {
            UploadRegFile();
        }
    }
}
