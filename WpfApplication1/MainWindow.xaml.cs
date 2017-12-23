using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using SharpMap;
using SharpMap.CoordinateSystems;
using NetTopologySuite;

namespace WpfApplication1
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            var gss = new NtsGeometryServices();
            var css = new CoordinateSystemServices(
                new CoordinateSystemFactory(Encoding.ASCII),
                new CoordinateTransformationFactory(),
                SharpMap.Converters.WellKnownText.SpatialReference.GetAllReferenceSystems());

            GeoAPI.GeometryServiceProvider.Instance = gss;
            Session.Instance
                .SetGeometryServices(gss)
                .SetCoordinateSystemServices(css)
                .SetCoordinateSystemRepository(css);

            string filepath = @"C:\Users\HuasWin7VM\Desktop\huas\GISData2\lianjiangRiver\lianjiangRiver.shp";

          
            var sf = new SharpMap.Data.Providers.ShapeFile(filepath);
            sf.Open();

            SharpMap.Data.FeatureDataRow dr = null;
            Collection<uint> objIDs = sf.GetObjectIDsInView(sf.GetExtents());
            foreach (uint id in objIDs)
            {
                dr = sf.GetFeature(id);
                
            }

            sf.Close();
            sf.Dispose();

        }
    }
}
