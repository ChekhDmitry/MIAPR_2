using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace MIAPR_2
{
    internal delegate void ShowDots(Dictionary<Point, List<Point>> classes);

    internal class DotProvider
    {
        private static List<Point> GenerateDots(int dotCount, int maxWidth, int maxHeight)
        {
            List<Point> pointList = new List<Point>();
            var random = new Random();
            for (int i = 0; i < dotCount; i++)
            {
                var point = new System.Drawing.Point();
                point.X = random.Next() % maxWidth;
                point.Y = random.Next() % maxHeight;
                pointList.Add(point);
            }
            return pointList;
        }

        private static Dictionary<Point, List<Point>> GenerateClassCenters(List<Point> pointList)
        {
            Dictionary<Point, List<Point>> classes = new Dictionary<Point, List<Point>>();
            var random = new Random();
            int centerNum = random.Next() % pointList.Count;
            Point center = pointList[centerNum];
            classes[center] = pointList;
            double maxDistance = 0;
            Point secondCenter = center;
            foreach (Point point in pointList)
            {
                double pointDistance = GetDistance(center, point);
                if (pointDistance > maxDistance)
                {
                    secondCenter = point;
                    maxDistance = pointDistance;
                }
            }
            classes.Add(secondCenter, new List<Point>() { secondCenter });
            pointList.Remove(secondCenter);
            return classes;
        }

        private static double GetDistance(Point point1, Point point2)
        {
            int x = point1.X - point2.X;
            int y = point1.Y - point2.Y;
            return Math.Sqrt(x * x + y * y);
        }

        private static double GetAverageDistance(List<Point> points)
        {
            double result = 0;
            int count = 0;
            for (int i = 0; i < points.Count - 1; i++)
            {
                for (int j = i + 1; j < points.Count; j++)
                {
                    result += GetDistance(points[i], points[j]);
                    count++;
                }
            }
            return result / count;
        }

        private static bool AddClassCenter(Dictionary<Point, List<Point>> classes)
        {
            Object syncObject = new object();
            Point newCenter = classes.Keys.ToList()[0];
            Point newCenterPosition = classes.Keys.ToList()[0];
            double maxDistance = 0;
            var tasks = new List<Task>();
            List<Point> centers = classes.Keys.ToList();
            foreach (Point center in classes.Keys)
            {
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    foreach (Point point in classes[center])
                    {
                        if (point != center)
                        {
                            double distance = GetDistance(point, center);
                            lock (syncObject)
                            {
                                if (distance > maxDistance)
                                {
                                    maxDistance = distance;
                                    newCenter = point;
                                    newCenterPosition = center;
                                }
                            }
                        }
                    }
                }));
            }
            Task.WaitAll(tasks.ToArray());
            double averageDistance = GetAverageDistance(classes.Keys.ToList());
            if (averageDistance / 2 > maxDistance)
            {
                return false;
            } 
            else
            {
                classes[newCenterPosition].Remove(newCenter);
                classes.Add(newCenter, new List<Point>() { newCenter});
                return true;
            }
        }

        private static void QualifyDots(Dictionary<Point, List<Point>> classes)
        {
            var newCenters = new List<Point>();
            var addedDots = new List<Point>();
            var oldCenters = new List<Point>();
            Object syncObject = new object();
            List<Point> centers = classes.Keys.ToList();
            foreach (Point center in classes.Keys)
            {
                var points = classes[center];
                var tasks = new List<Task>();
                foreach (Point point in points)
                {
                    tasks.Add(Task.Factory.StartNew(() =>
                    {
                        Point newCenter = center;
                        double minDistance = GetDistance(center, point);
                        foreach(Point diffCenter in centers)
                        {
                            if (diffCenter != center)
                            {
                                double distance = GetDistance(point, diffCenter);
                                if (distance < minDistance)
                                {
                                    newCenter = diffCenter;
                                    minDistance = distance;
                                }
                            }
                        }
                        if (newCenter != center)
                        {
                            lock(syncObject)
                            {
                                newCenters.Add(newCenter);
                                oldCenters.Add(center);
                                addedDots.Add(point);
                            }
                        }
                    }));
                }
                Task.WaitAll(tasks.ToArray());
            }
            for (int i = 0; i<newCenters.Count; i++)
            {
                classes[oldCenters[i]].Remove(addedDots[i]);
                classes[newCenters[i]].Add(addedDots[i]);
            }
        }

        public static void GetClasses(int dotCount, int maxWidth, int maxHeight, ShowDots showDots)
        {
            List<Point> pointList = GenerateDots(dotCount, maxWidth, maxHeight);
            Dictionary<Point, List<Point>> classes = GenerateClassCenters(pointList);

            do
            {
                QualifyDots(classes);
                showDots(classes);
            }
            while (AddClassCenter(classes));
        }
    }
}
