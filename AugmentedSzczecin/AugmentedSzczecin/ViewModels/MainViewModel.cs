﻿using AugmentedSzczecin.Interfaces;
using Caliburn.Micro;
using Windows.ApplicationModel.Resources;
using Windows.UI.Popups;

namespace AugmentedSzczecin.ViewModels
{
    public class MainViewModel : Screen
    {
        private readonly INavigationService _navigationService;
        private readonly IAccountService _accountService;

        public MainViewModel(INavigationService navigationService, IAccountService accountService)
        {
            _navigationService = navigationService;
            _accountService = accountService;
        }

        public void NavigateToAbout()
        {
            _navigationService.NavigateToViewModel<AboutViewModel>();
        }

        public void NavigateToCurrentMap()
        {
            _navigationService.NavigateToViewModel<CurrentMapViewModel>();
        }

        public void NavigateToLocationList()
        {
            _navigationService.NavigateToViewModel<LocationListViewModel>();
        }

        public void NavigateToSignUp()
        {
            _navigationService.NavigateToViewModel<SignUpViewModel>();
        }

        public void NavigateToSignIn()
        {
            _navigationService.NavigateToViewModel<SignInViewModel>();
        }

        public bool IsGuest
        {
            get
            {
                return !_accountService.IsUserSignedIn();
            }
        }

        public bool IsUser
        {
            get
            {
                return _accountService.IsUserSignedIn();
            }
        }

        public void SignOut()
        {
            var loader = new ResourceLoader();
            var msg = new MessageDialog(loader.GetString("SignOutConfirmationText"));

            msg.Commands.Add(new UICommand("Sign out", new UICommandInvokedHandler(SignOutConfirmed)));
            msg.Commands.Add(new UICommand("Cancel"));

            msg.ShowAsync();
        }

        private void SignOutConfirmed(IUICommand command)
        {
            _accountService.SignOut();
            NotifyOfPropertyChange(() => IsUser);
            NotifyOfPropertyChange(() => IsGuest);
        }

        public void NavigateToAugmentedView()
        {
            _navigationService.NavigateToViewModel<AugmentedViewModel>();
        }
    }
}
