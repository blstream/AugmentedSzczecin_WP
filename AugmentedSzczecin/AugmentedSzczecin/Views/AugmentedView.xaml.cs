﻿using AugmentedSzczecin.Converters;
using AugmentedSzczecin.Events;
using AugmentedSzczecin.Models;
using AugmentedSzczecin.UserControls;
using Caliburn.Micro;
using Microsoft.Maps.SpatialToolbox;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Geolocation;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace AugmentedSzczecin.Views
{
    public sealed partial class AugmentedView : Page, IHandle<PointOfInterestLoadedEvent>, IHandle<PointOfInterestLoadFailedEvent>
    {
        private Compass _compass;
        private Geolocator _gps;

        private uint _movementThreshold = 100;
        private double _angleOfView = 45.0;

        private Geopoint _currentLocation = null;
        private double _currentHeading = 0;

        private ObservableCollection<PointOfInterest> _poiLocations;
        readonly object _eventAgg;
        public AugmentedView()
        {
            InitializeComponent();

            _eventAgg = IoC.GetInstance(typeof(IEventAggregator), null);
            ((EventAggregator)_eventAgg).Subscribe(this);
        }

        ~AugmentedView()
        {
            ((EventAggregator)_eventAgg).Unsubscribe(this);
        }
        private async Task StartCamera()
        {
            var mediaCapture = new MediaCapture();
            (App.Current as App)._mediaCapture = mediaCapture;;
            var cameraId = await FindRearFacingCamera();
            if (!string.IsNullOrEmpty(cameraId))
            {
                var settings = new MediaCaptureInitializationSettings();
                settings.VideoDeviceId = cameraId;
                settings.StreamingCaptureMode = StreamingCaptureMode.Video;
                await mediaCapture.InitializeAsync(settings);
                PreviewScreen.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();
            }
        }

        private async void StopCamera()
        {
            if (PreviewScreen.Source != null)
            {
                await PreviewScreen.Source.StopPreviewAsync();
                PreviewScreen.Source.Dispose();
                PreviewScreen.Source = null;
            }
        }

        private async Task<string> FindRearFacingCamera()
        {
            var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            var device = (from d in devices
                          where d.EnclosureLocation != null &&
                                d.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back
                          select d).FirstOrDefault();

            if (device == null)
            {
                device = (from d in devices
                          where d.Name.ToLower().Contains("back") ||
                                d.Id.ToLower().Contains("back") ||
                                d.Name.ToLower().Contains("rear") ||
                                d.Id.ToLower().Contains("rear")
                          select d).FirstOrDefault();
            }

            if (device != null)
            {
                return device.Id;
            }

            return null;
        }

        private void UpdateARView()
        {
            if (_currentLocation != null)
            {
                ItemCanvas.Children.Clear();

                if (_poiLocations != null && _poiLocations.Count > 0)
                {
                    var currentCoord = new Coordinate(_currentLocation.Position.Latitude,
                        _currentLocation.Position.Longitude);

                    foreach (var poi in _poiLocations)
                    {
                        if (Math.Abs(poi.Location.Latitude) > 90 || Math.Abs(poi.Location.Longitude) > 180)
                        {
                            continue;
                        }
                        var poiCoord = new Coordinate(poi.Location.Latitude, poi.Location.Longitude);
                        var poiHeading = SpatialTools.CalculateHeading(currentCoord, poiCoord);
                        var diff = _currentHeading - poiHeading;

                        if (diff > 180)
                        {
                            diff = _currentHeading - (poiHeading + 360);
                        }
                        else if (diff < -180)
                        {
                            diff = _currentHeading + 360 - poiHeading;
                        }

                        if (Math.Abs(diff) <= (_angleOfView / 2))
                        {
                            var distance = SpatialTools.HaversineDistance(currentCoord, poiCoord, DistanceUnits.Meters);

                            double left = 0;

                            if (diff > 0)
                            {
                                left = ItemCanvas.ActualWidth / 2 * (((_angleOfView / 2) - diff) / (_angleOfView / 2));
                            }
                            else
                            {
                                left = ItemCanvas.ActualWidth / 2 * (1 + -diff / (_angleOfView / 2));
                            }

                            double top = (ItemCanvas.ActualHeight - 90) * (1 - distance / RadiusSlider.Value) + 45;

                            var converter = new CategoryToPinSignConverter();
                            var symbol = (Symbol) converter.Convert(poi.Category, null, null, null);
                            var pin = new ArPin() { PinSign = symbol, PinDist = ((int)distance).ToString() };

                            Canvas.SetLeft(pin, left - 32);
                            Canvas.SetTop(pin, top - 45);
                            ItemCanvas.Children.Add(pin);
                        }
                    }
                }
            }
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            await StartCamera();

            _compass = Compass.GetDefault();
            if (_compass != null)
            {
                _compass.ReadingChanged += CompassReadingChanged;
                CompassChanged(_compass.GetCurrentReading());
            }

            _gps = new Geolocator();
            _gps.MovementThreshold = _movementThreshold;
            _gps.PositionChanged += GpsPositionChanged;

            if (_gps.LocationStatus == PositionStatus.Ready)
            {
                var pos = await _gps.GetGeopositionAsync();
                if (pos != null && pos.Coordinate != null && pos.Coordinate.Point != null)
                {
                    GpsChanged(pos.Coordinate.Point.Position);
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _compass.ReadingChanged -= CompassReadingChanged;
            _gps.PositionChanged -= GpsPositionChanged;
            StopCamera();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
            base.OnNavigatedFrom(e);
        }

        private void CompassReadingChanged(Compass sender, CompassReadingChangedEventArgs args)
        {
            var reading = sender.GetCurrentReading();
            CompassChanged(reading);
        }

        private async void CompassChanged(CompassReading reading)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (reading.HeadingAccuracy != MagnetometerAccuracy.High)
                {
                    CompassCalibration.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
                else
                {
                    CompassCalibration.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    _currentHeading = reading.HeadingMagneticNorth + 90;
                    UpdateARView();
                }
                
            });
        }

        private void GpsPositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            if (args.Position != null && args.Position.Coordinate != null && args.Position.Coordinate.Point != null)
            {
                GpsChanged(args.Position.Coordinate.Point.Position);
            }
        }

        private async void GpsChanged(BasicGeoposition position)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _currentLocation = new Geopoint(new BasicGeoposition() { Latitude = position.Latitude, Longitude = position.Longitude });
                UpdateARView();
            });
        }

        public async void Handle(PointOfInterestLoadedEvent e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _poiLocations = e.PointOfInterestList;
                PoiCount.Text = _poiLocations.Count.ToString();
            });
        }

        public void Handle(PointOfInterestLoadFailedEvent e)
        {
            var msg = new MessageDialog(e.PointOfInterestLoadException.Message);
            msg.ShowAsync();
        }
    }
}
