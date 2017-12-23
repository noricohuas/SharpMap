﻿
namespace UnitTests.Data.Providers
{

    [NUnit.Framework.TestFixture]
    public class ShapeFileProviderTests : ProviderTest
    {
        private long _msLineal;
        private long _msVector;
        private const int NumberOfRenderCycles = 1;

        [NUnit.Framework.TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            System.Console.WriteLine("Speed comparison:");
            System.Console.WriteLine("VectorLayer\tLinealLayer\tRatio");
            System.Console.WriteLine(string.Format("{0}\t{1}\t{2:N}", _msVector, _msLineal,
                                                   ((double)_msLineal / _msVector * 100)));
        }

        [NUnit.Framework.Test]
        public void UsingTest()
        {
            using (var s = System.IO.File.Open(GetTestDataFilePath("SPATIAL_F_SKARVMUFF.shp"), System.IO.FileMode.Open))
            {
                using (var reader = new System.IO.BinaryReader(s))
                {
                    System.Console.WriteLine(reader.ReadInt32());
                }
                NUnit.Framework.Assert.Throws<System.ObjectDisposedException>(() => System.Console.WriteLine(s.Position));
            } 
        }

        private static void CopyShapeFile(string path, out string tmp)
        {
            tmp = System.IO.Path.ChangeExtension(System.IO.Path.GetTempFileName(),".shp");
            if (!System.IO.File.Exists(path)) throw new NUnit.Framework.IgnoreException("File not found");
            foreach (var file in System.IO.Directory.GetFiles(System.IO.Path.GetDirectoryName(path), System.IO.Path.GetFileNameWithoutExtension(path) + ".*"))
            {
                var copyFile = System.IO.Path.ChangeExtension(tmp, System.IO.Path.GetExtension(file));
                if (System.IO.File.Exists(copyFile)) System.IO.File.Delete(copyFile);
                System.IO.File.Copy(file, copyFile);
            }
        }

        [NUnit.Framework.Test]
        public void TestDeleteAfterClose()
        {
            string test;
            CopyShapeFile(GetTestDataFilePath("SPATIAL_F_SKARVMUFF.shp"), out test);

            var shp = new SharpMap.Data.Providers.ShapeFile(test);
            shp.Open();
            shp.Close();
            var succeeded = true;
            foreach (var file in System.IO.Directory.GetFiles(System.IO.Path.GetDirectoryName(test), System.IO.Path.GetFileNameWithoutExtension(test) + ".*"))
            {
                try
                {
                    System.IO.File.Delete(file);
                }
                catch (System.Exception)
                {
                    System.Console.WriteLine("Failed to delete '{0}'", file);
                    succeeded = false;
                }
            }
            NUnit.Framework.Assert.IsTrue(succeeded);
        }
        //private string GetTestFile()
        //{
        //    return System.IO.Path.Combine(GetPathToTestDataDir(), "roads_ugl.shp");
        //}

        //private string GetPathToTestDataDir()
        //{
        //    return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.GetType().Assembly.CodeBase.Replace("file:///", "")), @"TestData\");
        //}

        [NUnit.Framework.Test]
        public void TestReadPointZShapeFile()
        {
            var file = GetTestDataFilePath("SPATIAL_F_SKARVMUFF.shp");
            var sh = new SharpMap.Data.Providers.ShapeFile(file, true);
            sh.Open();
            var fc = sh.GetFeatureCount();
            NUnit.Framework.Assert.AreEqual(4342, fc);
            NUnit.Framework.Assert.AreEqual(0, sh.GetObjectIDsInView(sh.GetExtents())[0]);
            var featsInView = sh.GetGeometriesInView(new GeoAPI.Geometries.Envelope(sh.GetExtents()));
            NUnit.Framework.Assert.AreEqual(4342, featsInView.Count);
            sh.Close();
        }

        [NUnit.Framework.Test]
        public void TestPerformanceVectorLayer()
        {
            NUnit.Framework.Assert.IsTrue(System.IO.File.Exists(GetTestDataFilePath("SPATIAL_F_SKARVMUFF.shp")),
                                          "Specified shapefile is not present!");

            var map = new SharpMap.Map(new System.Drawing.Size(1024, 768));

            var shp = new SharpMap.Data.Providers.ShapeFile(GetTestDataFilePath("SPATIAL_F_SKARVMUFF.shp"), false, false);
            var lyr = new SharpMap.Layers.VectorLayer("Roads", shp);

            map.Layers.Add(lyr);
            map.ZoomToExtents();

            RepeatedRendering(map, shp.GetFeatureCount(), NumberOfRenderCycles, out _msVector);

            var res = map.GetMap();
            var path = System.IO.Path.ChangeExtension(GetTestDataFilePath("SPATIAL_F_SKARVMUFF.shp"), ".vector.png");
            res.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            System.Console.WriteLine("\nResult saved at file://" + path.Replace('\\', '/'));
        }

        [NUnit.Framework.Test]
        public void TestPerformanceLinealLayer()
        {
            NUnit.Framework.Assert.IsTrue(System.IO.File.Exists(GetTestDataFilePath("roads_ugl.shp")),
                                          "Specified shapefile is not present!");

            var map = new SharpMap.Map(new System.Drawing.Size(1024, 768));

            var shp = new SharpMap.Data.Providers.ShapeFile(GetTestDataFilePath("roads_ugl.shp"), false, false);
            var lyr = new SharpMap.Layers.Symbolizer.LinealVectorLayer("Roads", shp)
                          {
                              Symbolizer =
                                  new SharpMap.Rendering.Symbolizer.BasicLineSymbolizer
                                      {Line = new System.Drawing.Pen(System.Drawing.Color.Black)}
                          };
            map.Layers.Add(lyr);
            map.ZoomToExtents();

            RepeatedRendering(map, shp.GetFeatureCount(), NumberOfRenderCycles, out _msLineal);
            System.Console.WriteLine("\nWith testing if record is deleted ");
            
            shp.CheckIfRecordIsDeleted = true;
            long tmp;
            RepeatedRendering(map, shp.GetFeatureCount(), 1, out tmp);

            var res = map.GetMap();
            var path = System.IO.Path.ChangeExtension(GetTestDataFilePath("SPATIAL_F_SKARVMUFF.shp"), "lineal.png");
            res.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            System.Console.WriteLine("\nResult saved at file://" + path.Replace('\\', '/'));
        }

        private static void RepeatedRendering(SharpMap.Map map, int numberOfFeatures, int numberOfTimes, out long avgRenderTime)
        {
            System.Console.WriteLine("Rendering Map with " + numberOfFeatures + " features");
            var totalRenderTime = 0L;
            var sw = new System.Diagnostics.Stopwatch();
            for (var i = 1; i <= numberOfTimes; i++)
            {
                System.Console.Write(string.Format("Rendering {0}x time(s)", i));
                sw.Start();
                map.GetMap();
                sw.Stop();
                System.Console.WriteLine(" in " +
                                         sw.ElapsedMilliseconds.ToString(
                                             System.Globalization.NumberFormatInfo.CurrentInfo) + "ms.");
                totalRenderTime += sw.ElapsedMilliseconds;
                sw.Reset();
            }

            avgRenderTime = totalRenderTime/numberOfTimes;
            System.Console.WriteLine("\n Average rendering time:" + avgRenderTime.ToString(
                                         System.Globalization.NumberFormatInfo.CurrentInfo) + "ms.");
        }

        [NUnit.Framework.Test]
        public void TestGetFeature()
        {
            NUnit.Framework.Assert.IsTrue(System.IO.File.Exists(GetTestDataFilePath("SPATIAL_F_SKARVMUFF.shp")),
                                          "Specified shapefile is not present!");

            var shp = new SharpMap.Data.Providers.ShapeFile(GetTestDataFilePath("SPATIAL_F_SKARVMUFF.shp"), false, false);
            shp.Open();
            var feat = shp.GetFeature(0);
            NUnit.Framework.Assert.IsNotNull(feat);
            shp.Close();
        }

        [NUnit.Framework.Test]
        public void TestExecuteIntersectionQuery()
        {
            NUnit.Framework.Assert.IsTrue(System.IO.File.Exists(GetTestDataFilePath("SPATIAL_F_SKARVMUFF.shp")),
                                          "Specified shapefile is not present!");

            var shp = new SharpMap.Data.Providers.ShapeFile(GetTestDataFilePath("SPATIAL_F_SKARVMUFF.shp"), false, false);
            shp.Open();

            var fds = new SharpMap.Data.FeatureDataSet();
            var bbox = shp.GetExtents();
            //narrow it down
            bbox.ExpandBy(-0.425*bbox.Width, -0.425*bbox.Height);

            //Just to avoid that initial query does not impose performance penalty
            shp.DoTrueIntersectionQuery = false;
            shp.ExecuteIntersectionQuery(bbox, fds);
            fds.Tables.RemoveAt(0);

            //Perform query once more
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            shp.ExecuteIntersectionQuery(bbox, fds);
            sw.Stop();
            System.Console.WriteLine("Queried using just envelopes:\n" + fds.Tables[0].Rows.Count + " in " +
                                     sw.ElapsedMilliseconds + "ms.");
            fds.Tables.RemoveAt(0);
            
            shp.DoTrueIntersectionQuery = true;
            sw.Reset();
            sw.Start();
            shp.ExecuteIntersectionQuery(bbox, fds);
            sw.Stop();
            System.Console.WriteLine("Queried using prepared geometries:\n" + fds.Tables[0].Rows.Count + " in " +
                                     sw.ElapsedMilliseconds + "ms.");
        }

        [NUnit.Framework.Test]
        public void TestExecuteIntersectionQueryWithFilterDelegate()
        {
            NUnit.Framework.Assert.IsTrue(System.IO.File.Exists(GetTestDataFilePath("SPATIAL_F_SKARVMUFF.shp")),
                                          "Specified shapefile is not present!");

            var shp = new SharpMap.Data.Providers.ShapeFile(GetTestDataFilePath("SPATIAL_F_SKARVMUFF.shp"), false, false);
            shp.Open();

            var fds = new SharpMap.Data.FeatureDataSet();
            var bbox = shp.GetExtents();
            //narrow it down
            bbox.ExpandBy(-0.425*bbox.Width, -0.425*bbox.Height);

            //Just to avoid that initial query does not impose performance penalty
            shp.DoTrueIntersectionQuery = false;
            shp.FilterDelegate = JustTracks;

            shp.ExecuteIntersectionQuery(bbox, fds);
            fds.Tables.RemoveAt(0);

            //Perform query once more
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            shp.ExecuteIntersectionQuery(bbox, fds);
            sw.Stop();
            System.Console.WriteLine("Queried using just envelopes:\n" + fds.Tables[0].Rows.Count + " in " +
                                     sw.ElapsedMilliseconds + "ms.");
            fds.Tables.RemoveAt(0);
            
            shp.DoTrueIntersectionQuery = true;
            sw.Reset();
            sw.Start();
            shp.ExecuteIntersectionQuery(bbox, fds);
            sw.Stop();
            System.Console.WriteLine("Queried using prepared geometries:\n" + fds.Tables[0].Rows.Count + " in " +
                                     sw.ElapsedMilliseconds + "ms.");
        }

        public static bool JustTracks(SharpMap.Data.FeatureDataRow fdr)
        {
            //System.Console.WriteLine(fdr [0] + ";"+ fdr[4]);
            var s = fdr[4] as string;
            if (s != null)
                return s == "track";
            return true;
        }
    }
}