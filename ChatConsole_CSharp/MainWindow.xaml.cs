using System;
using System.Windows;

namespace ChatConsole_CSharp
{
    /// <summary>
    /// Interação lógica para MainWindow.xam
    /// </summary>
    public partial class MainWindow : Window
    {
        GeodeExtension extension;
        public MainWindow()
        {
            InitializeComponent();
            try
            {
                extension = new GeodeExtension();
            }
            catch
            {
                Environment.Exit(0);
            }
        }
    }
}
