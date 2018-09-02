using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using OpenCvSharp;
using SpeedCam.Data.Entities;
using SpeedCam.Data.Db;

namespace SpeedCam.VideoAnalyzer.Analyze
{
    public class CarIdentificationService
    {
        private int NextBlobId;
        private int NextTrackerId;
        private readonly SqlDatabase Database;
        private readonly Config Config;

        public CarIdentificationService(Config config)
        {
            Database = new SqlDatabase(config.DbConnectionString);
            NextBlobId = 1;
            NextTrackerId = 1;
            Config = config;
        }

        public void AnalyzeVideo(string file, bool isDebug)
        {
            var dateString = "1900-01-01 12:00:00";
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (fileName.Contains("_"))
            {
                dateString = fileName.Split("_")[0].Insert(4, "-").Insert(7, "-").Insert(10, " ").Insert(13, ":").Insert(16, ":");
            }
            var currentFrame = 1;
            var startDate = DateTime.Parse(dateString);
            var stopwatch = Stopwatch.StartNew();
            var frameCount = 0;
            var frame = new Mat();
            var bgSub = BackgroundSubtractorMOG.Create(200, 100);

            var carRecords = new List<CarTracker>();
            var entries = new List<Entry>();

            using (var videoCapture = VideoCapture.FromFile(file))
            {
                frameCount = videoCapture.FrameCount;

                while (videoCapture.IsOpened())
                {
                    frame = videoCapture.RetrieveMat();
                    if (frame.Cols == 0)
                        break;

                    var carBlobs = DetectCars(frame, bgSub, carRecords.HasPossibleOverlaps(), isDebug);
                    var blobsWithTrackers = MatchBlobsToTrackers(carBlobs, carRecords);

                    carRecords.ForEach(c => c.IsUpdated = false);

                    foreach (var blob in blobsWithTrackers)
                    {
                        var closestTracker = carRecords.Where(c => c.Id == blob.Value).FirstOrDefault();
                        if (closestTracker == null)
                        {
                            closestTracker = new CarTracker
                            {
                                Id = NextTrackerId++,
                                Car = new Entry
                                {
                                    Direction = blob.Key.X < Config.LeftStart + 75 ? "R" : "L",
                                    DateAdded = startDate.AddSeconds(currentFrame / 30)
                                },
                                Rect = blob.Key,
                                IsUpdated = true
                            };
                            carRecords.Add(closestTracker);
                        }
                        else
                        {
                            closestTracker.IsUpdated = true;
                            closestTracker.LastUpdated = currentFrame;
                            closestTracker.Rect = blob.Key;

                            if (isDebug)
                            {
                                Cv2.Rectangle(frame, blob.Key, Scalar.Purple, 8);
                            }
                        }

                        closestTracker.ValidateTracker(frame.Height);
                    }
                    carRecords.ForEach(c => c.UpdateEvents(currentFrame, frame, Config));

                    //remove any trackers that have completed but are invalid OR they haven't been updated in 15 frames
                    var badRecords = carRecords.Where(c => (!c.IsUpdated && c.Car.Speed > 0 && c.IsInvalid) || (!c.IsUpdated && currentFrame - c.LastUpdated > 15)).ToList();
                    foreach (var badRecord in badRecords)
                    {
                        carRecords.Remove(badRecord);
                    }

                    var completedRecords = carRecords.Where(c => !c.IsUpdated && c.Car.Speed > 0).ToList();
                    foreach (var completed in completedRecords)
                    {
                        entries.Add(completed.Car);
                        carRecords.Remove(completed);
                    }

                    var fps = Math.Round(currentFrame / stopwatch.Elapsed.TotalSeconds, 2);
                    var percentDone = (currentFrame / (double)frameCount) * 100;
                    var timeRemaining = TimeSpan.FromSeconds((frameCount - currentFrame) / fps);

                    if (isDebug)
                    {
                        DrawStartEndLines(frame);
                        DrawTime(startDate, currentFrame, frame);
                        Cv2.ImShow("Frame", frame);
                        Cv2.WaitKey(1);                        
                    }

                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write($"\r{currentFrame} of {frameCount} - Time: {startDate.AddSeconds(currentFrame / 30).ToString("HH:mm:ss")} - FPS: {fps.ToString("N2")} - {Math.Round(percentDone, 2).ToString("N2")}% - Remaining: {timeRemaining.ToString(@"hh\:mm\:ss")} - Cars: {entries.Count}                           ");

                    currentFrame++;
                }
            }

            Console.WriteLine("");
            Console.WriteLine($"{entries.Count} cars in {stopwatch.Elapsed.ToString(@"hh\:mm\:ss")}");

            foreach (var entry in entries)
            {
                Console.WriteLine($"{entry.DateAdded.ToString("hh:mm:ss")}: {entry.Direction} - {entry.Speed:N2}");

                Database.EntryInsert(entry);
                
                if(!entry.PhotoUpdated)
                {
                    Database.LogInsert(new Log
                    {
                        DateAdded = DateTime.Now,
                        Message = $"Photo for #{entry.Id} has not been updated!",
                        StackTrace = ""
                    });

                }
                if (entry.Picture != null)
                {
                    using (var fileStream = new FileStream(Path.Combine(Config.PhotoFolder, $"{entry.Id}.jpg"), FileMode.Create))
                    {
                        fileStream.Write(entry.Picture, 0, entry.Picture.Length);
                    }
                }
            }

            return;
        }
        
        private List<Rect> DetectCars(Mat frame, BackgroundSubtractorMOG bgSubtractor, bool groupRectangles, bool isDebug = true)
        {
            Mat fgMask = new Mat();

            bgSubtractor.Apply(frame, fgMask);

            var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(10, 10));
            var closing = new Mat();
            var opening = new Mat();
            var dilation = new Mat();
            Cv2.MorphologyEx(fgMask, closing, MorphTypes.Close, kernel);
            Cv2.MorphologyEx(closing, opening, MorphTypes.Open, kernel);
            Cv2.Dilate(opening, fgMask, kernel);

            var cars = new List<Rect>();
            var hierarchy = new Mat();
            Cv2.FindContours(fgMask, out Mat[] contours, hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxTC89L1);
            foreach (var contour in contours)
            {
                var boundingRect = Cv2.BoundingRect(contour);
                //TODO:  Add to config
                if (boundingRect.Width < 130 
                 || boundingRect.Height < 40 
                 || boundingRect.Height > 210)
                    continue;

                cars.Add(boundingRect);
            }

            if (groupRectangles)
            {
                var duplicateCars = new List<Rect>(cars);
                duplicateCars.AddRange(cars);

                Cv2.GroupRectangles(duplicateCars, 1, 1);
                cars = duplicateCars;
            }

            if (isDebug)
            {
                foreach (var boundingRect in cars)
                {
                    Cv2.Rectangle(frame, boundingRect, Scalar.Blue, 5);
                }

                //Cv2.ImShow("Mask", fgMask);
                //Cv2.WaitKey(1);                
            }

            return cars;
        }

        private Dictionary<Rect, int> MatchBlobsToTrackers(List<Rect> blobs, List<CarTracker> trackers)
        {
            Dictionary<int, List<Neighbor>> blobNeighbors = new Dictionary<int, List<Neighbor>>();
            Dictionary<int, List<Neighbor>> trackerNeighbors = new Dictionary<int, List<Neighbor>>();

            //add unique IDs to the blobs
            List<TrackedRect> blobsWithIds = new List<TrackedRect>();
            foreach (var blob in blobs)
            {
                blobsWithIds.Add(new TrackedRect
                {
                    Id = NextBlobId,
                    Rect = blob
                });
                NextBlobId++;
            }

            //each blob gets a list of their distance from each tracker
            foreach (var blob in blobsWithIds)
            {
                blobNeighbors.Add(blob.Id, new List<Neighbor>());

                foreach(var tracker in trackers)
                {
                    blobNeighbors[blob.Id].Add(new Neighbor
                    {
                        Id = tracker.Id,
                        Distance = tracker.GetDistance(blob.Rect)
                    });
                }

                blobNeighbors[blob.Id] = blobNeighbors[blob.Id].OrderBy(t => t.Distance).ToList();
            }

            //each tracker gets a list of their distance from each blob
            foreach (var tracker in trackers)
            {
                trackerNeighbors.Add(tracker.Id, new List<Neighbor>());

                foreach (var blob in blobsWithIds)
                {
                    if (tracker.IsPossibleMatch(blob.Rect))
                    {
                        trackerNeighbors[tracker.Id].Add(new Neighbor
                        {
                            Id = blob.Id,
                            Distance = tracker.GetDistance(blob.Rect)
                        });
                    }
                }

                trackerNeighbors[tracker.Id] = trackerNeighbors[tracker.Id].OrderBy(t => t.Distance).ToList();
            }

            //match the trackers and blobs by what they agree is closest
            var results = new Dictionary<Rect, int>();
            foreach (var blob in blobsWithIds)
            {
                foreach(var tracker in blobNeighbors[blob.Id])
                {
                    var closestTracker = trackerNeighbors[tracker.Id].FirstOrDefault();
                    if(closestTracker != null && closestTracker.Id == blob.Id)
                    {
                        results.Add(blob.Rect, tracker.Id);
                        trackerNeighbors[tracker.Id].Remove(closestTracker);
                        break;
                    }
                }

                if(!results.ContainsKey(blob.Rect))
                {
                    results.Add(blob.Rect, 0);
                }
            }

            return results;
        }

        private void DrawStartEndLines(Mat frame)
        {
            Cv2.Rectangle(frame, new Rect(Config.LeftStart, 110, 1, 100), Scalar.Green, 3);
            Cv2.Rectangle(frame, new Rect(Config.RightStart, 90, 1, 100), Scalar.Green, 3);
        }

        private void DrawTime(DateTime startDate, int currentFrame, Mat frame)
        {
            var currentTime = startDate.AddSeconds(currentFrame / 30);
            Cv2.PutText(frame, currentTime.ToString("M/d/yy h:mm:ss tt"), new Point(750, 220), HersheyFonts.HersheyPlain, 2, Scalar.White, 1, LineTypes.AntiAlias);
        }
    }
}
