﻿using System;
using Xamarin.Forms;
using Isop.Client.Transfer;

namespace Isop.Xamarin
{
    public class SelectControllerPage : MasterDetailPage
    {
        public SelectControllerPage()
        {
            Label header = new Label
                {
                    Text = "Select controller",
                    HorizontalOptions = LayoutOptions.Center
                };
            var controllers = new []{
                new ControllerViewModel(){
                    Name="test ",
                    Methods=new []{
                        new MethodViewModel(new Method(){Name="test 0"})
                    },
                },
                new ControllerViewModel(){
                    Name="test 2",
                    Methods=new []{
                        new MethodViewModel(new Method(){Name="test -1"})
                    },
                }
            };
            // Create ListView for the master page.
            ListView controllersView = new ListView
                {
                    ItemsSource = controllers
                };
            this.Master = new ContentPage
                {
                    Title = header.Text,
                    Content = new StackLayout
                        {
                            Children = 
                                {
                                    header, 
                                    controllersView
                                }
                        }
                };
            this.Detail = new NavigationPage(new SelectMethodPage());


            this.IsPresented = true;
            // For Windows Phone, provide a way to get back to the master page.
            if (Device.OS == TargetPlatform.WinPhone)
            {
                (this.Detail as ContentPage).Content.GestureRecognizers.Add(
                    new TapGestureRecognizer
                    {
                    
                        Command = new Command( () => 
                        {
                            this.IsPresented = true;
                        })
                   });
            }

            // Define a selected handler for the ListView.
            controllersView.ItemSelected += (sender, args) =>
                {
                    // Set the BindingContext of the detail page.
                    this.Detail.BindingContext = args.SelectedItem;

                    // Show the detail page.
                    this.IsPresented = false;
                };

            // Initialize the ListView selection.
            controllersView.SelectedItem = new Controller[0];

        }
    }

}



