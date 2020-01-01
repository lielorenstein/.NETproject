﻿using BE;
using BL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
// this is the page where we display the list of hosting units
namespace PL
{
    /// <summary>
    /// Interaction logic for HostinUnitListWindow.xaml
    /// </summary>
    public partial class HostinUnitListPage : Page
    {
        private Host m_owner;

   

        public HostinUnitListPage(Host host, NavigationService navigationService)
        {
            InitializeComponent();
            this.m_owner = host;
            List<HostingUnitItemControl> hostingUnitsItemsControl = new List<HostingUnitItemControl>();
            ImpBL bl = ImpBL.Instance;
            foreach (var hostingUnit in bl.GetUnitsByHost(host.HostId))
            {
                HostingUnitItemControl huic = new HostingUnitItemControl(hostingUnit, navigationService);
                huic.HostinUnitNameTextBlock.Text = hostingUnit.HostingUnitName;// displayin the units name
                huic.HostingUnitKeyLable.Content = "#" + hostingUnit.HostingUnitKey;//displaying the units key
                hostingUnitsItemsControl.Add(huic);
            }
            HostingUnitListView.ItemsSource = hostingUnitsItemsControl;
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)//this is for adding a new hosting unit
        {
             HostingUnitPage obj =new HostingUnitPage(m_owner, false);
            this.NavigationService.Navigate(obj);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.GoBack();
        }
    }
}