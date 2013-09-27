﻿using Skadoosh.Common.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Skadoosh.Store.Common;
// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Skadoosh.Store.Views.Presenter
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class PresenterProfile : Skadoosh.Store.Common.LayoutAwarePage
    {
        public PresenterProfile()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
          
            this.DataContext = (PresenterVM)navigationParameter; 
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var vm = (PresenterVM)this.DataContext;
            if (!string.IsNullOrEmpty(vm.User.Email) && !string.IsNullOrEmpty(vm.User.FirstName) && !string.IsNullOrEmpty(vm.User.LastName))
            {
                if (vm.User.Email.IsValidEmail())
                {
                    var result = await vm.CreateProfile();
                    if (result)
                    {
                        vm.ErrorMessage = string.Empty;
                        Frame.Navigate(typeof(SurveyLibrary), vm);
                    }
                    vm.ErrorMessage = "There Was A Problem Creating Your Profile";
                }
                else
                {
                    vm.ErrorMessage = "Not A Valid Email Address";
                }

            }
            else
            {
                vm.ErrorMessage = "Not All The Required Fields Have Values";

            }
        }
        private void ShowHelp(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Help), new ParticipateStaticVM());
        }
    }
}
