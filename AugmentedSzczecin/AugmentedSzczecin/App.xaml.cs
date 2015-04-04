﻿using AugmentedSzczecin.Interfaces;
using AugmentedSzczecin.Services;
using AugmentedSzczecin.ViewModels;
using AugmentedSzczecin.Views;
using Caliburn.Micro;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml.Controls;

namespace AugmentedSzczecin
{
    public sealed partial class App
    {
        private WinRTContainer _container;

        public App()
        {
            InitializeComponent();
        }

        protected override void Configure()
        {
            _container = new WinRTContainer();

            _container.RegisterWinRTServices();

            _container.PerRequest<MainViewModel>();
            _container.PerRequest<AboutViewModel>();
            _container.PerRequest<CurrentMapViewModel>();
            _container.PerRequest<LocationListViewModel>();
            _container.PerRequest<SignUpViewModel>();
            _container.PerRequest<SignInViewModel>();
            _container.PerRequest<IPointOfInterestService, PointOfInterestService>();
            _container.PerRequest<ILocationService, LocationService>();
            _container.PerRequest<IRegisterService, RegisterService>();
            _container.PerRequest<ISignInService, SignInService>();
            _container.PerRequest<IUserDataStorageService, UserDataStorageService>();
            IoC.GetInstance = GetInstance;
        }

        protected override void PrepareViewFirst(Frame rootFrame)
        {
            _container.RegisterNavigationService(rootFrame);
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            DisplayRootView<MainView>();
        }

        protected override object GetInstance(Type service, string key)
        {
            return _container.GetInstance(service, key);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return _container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            _container.BuildUp(instance);
        }
    }
}